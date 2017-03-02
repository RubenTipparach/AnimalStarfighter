using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(SgtAtmosphere))]
public class SgtAtmosphere_Editor : SgtEditor<SgtAtmosphere>
{
	protected override void OnInspector()
	{
		var updateMaterials      = false;
		var updateInnerRenderers = false;
		var updateOuters         = false;

		DrawDefault("Color", ref updateMaterials);
		BeginError(Any(t => t.Brightness < 0.0f));
			DrawDefault("Brightness", ref updateMaterials);
		EndError();
		DrawDefault("RenderQueue", ref updateMaterials);
		DrawDefault("RenderQueueOffset", ref updateMaterials);

		Separator();
		
		BeginError(Any(t => t.Height <= 0.0f));
			DrawDefault("Height", ref updateMaterials, ref updateOuters);
		EndError();
		BeginError(Any(t => t.Fog >= 1.0f));
			DrawDefault("Fog", ref updateMaterials);
		EndError();
		BeginError(Any(t => t.Sky < 0.0f));
			DrawDefault("Sky"); // Updated when rendering
		EndError();

		Separator();

		DrawDefault("Lit", ref updateMaterials);

		if (Any(t => t.Lit == true))
		{
			BeginIndent();
				BeginError(Any(t => t.LightingTex == null));
					DrawDefault("LightingTex", ref updateMaterials);
				EndError();
				DrawDefault("Scattering", ref updateMaterials);
				if (Any(t => t.Scattering == true))
				{
					BeginIndent();
						DrawDefault("GroundScattering", ref updateMaterials);
						BeginError(Any(t => t.ScatteringTex == null));
							DrawDefault("ScatteringTex", ref updateMaterials);
						EndError();
						DrawDefault("ScatteringStrength", ref updateMaterials);
					EndIndent();
				}
				BeginError(Any(t => t.Lights != null && (t.Lights.Count == 0 || t.Lights.Exists(l => l == null))));
					DrawDefault("Lights", ref updateMaterials);
				EndError();
				BeginError(Any(t => t.Shadows != null && t.Shadows.Exists(s => s == null)));
					DrawDefault("Shadows", ref updateMaterials);
				EndError();
			EndIndent();
		}

		Separator();

		BeginError(Any(t => t.InnerDepthTex == null));
			DrawDefault("InnerDepthTex", ref updateMaterials);
		EndError();
		BeginError(Any(t => t.InnerMeshRadius <= 0.0f));
			DrawDefault("InnerMeshRadius", ref updateMaterials);
		EndError();
		BeginError(Any(InvalidInnerRenderers));
			DrawDefault("InnerRenderers", ref updateInnerRenderers, false);
		EndError();
		
		Separator();
		
		BeginError(Any(t => t.OuterDepthTex == null));
			DrawDefault("OuterDepthTex", ref updateMaterials);
		EndError();
		BeginError(Any(t => t.OuterMeshRadius <= 0.0f));
			DrawDefault("OuterMeshRadius", ref updateOuters);
		EndError();
		BeginError(Any(t => t.OuterMeshes != null &&(t.OuterMeshes.Count == 0 || t.OuterMeshes.Exists(m => m == null) == true)));
			DrawDefault("OuterMeshes", ref updateOuters);
		EndError();
		
		if (Any(t => (t.InnerDepthTex == null || t.OuterDepthTex == null) && t.GetComponent<SgtAtmosphereDepth>() == null))
		{
			Separator();

			if (Button("Add Depth") == true)
			{
				Each(t => SgtHelper.GetOrAddComponent<SgtAtmosphereDepth>(t.gameObject));
			}
		}

		if (Any(t => t.Lit == true && t.LightingTex == null && t.GetComponent<SgtAtmosphereLighting>() == null))
		{
			Separator();

			if (Button("Add Lighting") == true)
			{
				Each(t => SgtHelper.GetOrAddComponent<SgtAtmosphereLighting>(t.gameObject));
			}
		}

		if (Any(t => t.Lit == true && t.Scattering == true && t.ScatteringTex == null && t.GetComponent<SgtAtmosphereScattering>() == null))
		{
			Separator();

			if (Button("Add Scattering") == true)
			{
				Each(t => SgtHelper.GetOrAddComponent<SgtAtmosphereScattering>(t.gameObject));
			}
		}

		if (updateMaterials == true) DirtyEach(t => t.UpdateMaterials());
		if (updateOuters    == true) DirtyEach(t => t.UpdateOuters   ());

		if (updateInnerRenderers == true)
		{
			Each(t => t.RemoveInnerMaterial());

			serializedObject.ApplyModifiedProperties();
			
			DirtyEach(t => t.ApplyInnerMaterial());
		}
	}

	private static bool InvalidInnerRenderers(SgtAtmosphere atmosphere)
	{
		for (var i = SgtTerrain.AllTerrains.Count - 1; i >= 0; i--)
		{
			if (SgtTerrain.AllTerrains[i].Atmosphere == atmosphere)
			{
				return false;
			}
		}

		return atmosphere.InnerRenderers == null || atmosphere.InnerRenderers.Count == 0 || atmosphere.InnerRenderers.Exists(r => r == null) == true;
	}
}
#endif

[ExecuteInEditMode]
[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Atmosphere")]
public class SgtAtmosphere : MonoBehaviour
{
	// All active and enabled coronas in the scene
	public static List<SgtAtmosphere> AllAtmospheres = new List<SgtAtmosphere>();

	[Tooltip("The color tint")]
	public Color Color = Color.white;

	[Tooltip("The Color.rgb values are multiplied by this")]
	public float Brightness = 1.0f;

	[Tooltip("The render queue group of the surface and sky materials")]
	public SgtRenderQueue RenderQueue = SgtRenderQueue.Transparent;

	[Tooltip("The render queue offset of the surface and sky materials")]
	public int RenderQueueOffset;
	
	[Tooltip("The height of the sky above the surface in local space")]
	public float Height = 0.1f;

	[Tooltip("If you want an extra-thin or extra-thick density, you can adjust that here (0 = default)")]
	public float Fog;

	[Tooltip("The opacity multiplier of the sky when the camera is inside the sky mesh")]
	public float Sky = 1.0f;
	
	[Tooltip("Does this atmosphere receive light?")]
	public bool Lit;
	
	[Tooltip("The lookup texture used to calculate the lighting color")]
	public Texture LightingTex;
	
	[Tooltip("Should lights scatter through the atmosphere?")]
	public bool Scattering;

	[Tooltip("Should lights scatter through the atmosphere onto the ground?")]
	public bool GroundScattering;

	[Tooltip("The lookup texture used to calculate the scattering brightness")]
	public Texture ScatteringTex;

	[Tooltip("The scattering brightness multiplier")]
	public float ScatteringStrength = 3.0f;
	
	[Tooltip("The lights shining on this atmosphere (max = 1 light, 2 = scattering)")]
	public List<Light> Lights;

	[Tooltip("The shadows casting on this atmosphere (max = 2)")]
	public List<SgtShadow> Shadows;

	[Tooltip("The lookup table used to calculate the optical depth of the surface material")]
	public Texture InnerDepthTex;
	
	[Tooltip("The radius of the inner renderers (surface) in local coordinates")]
	public float InnerMeshRadius = 1.0f;

	[Tooltip("The renderers that are used to render the surface mesh")]
	public List<MeshRenderer> InnerRenderers;
	
	[Tooltip("The lookup table used to calculate the optical depth of the sky material")]
	public Texture2D OuterDepthTex;
	
	[Tooltip("The radius of the OuterMeshes in local space")]
	public float OuterMeshRadius = 1.0f;

	[Tooltip("The meshes used to render the sky (should form a sphere)")]
	public List<Mesh> OuterMeshes;
	
	// The GameObjects used to render the sky
	public List<SgtAtmosphereOuter> Outers;
	
	// The material applied to the surface
	[System.NonSerialized]
	public Material InnerMaterial;

	// The material applied to the sky
	[System.NonSerialized]
	public Material OuterMaterial;
	
	[SerializeField]
	protected bool awakeCalled;

	[SerializeField]
	protected bool startCalled;
	
	[System.NonSerialized]
	protected bool updateMaterialsCalled;
	
	[System.NonSerialized]
	protected bool updateOutersCalled;
	
	public float OuterRadius
	{
		get
		{
			return InnerMeshRadius + Height;
		}
	}
	
	public void UpdateDepthTex()
	{
		if (InnerMaterial != null)
		{
			InnerMaterial.SetTexture("_DepthTex", InnerDepthTex);
		}

		if (OuterMaterial != null)
		{
			OuterMaterial.SetTexture("_DepthTex", OuterDepthTex);
		}
	}
	
	public void UpdateLightingTex()
	{
		if (InnerMaterial != null)
		{
			InnerMaterial.SetTexture("_LightingTex", LightingTex);
		}

		if (OuterMaterial != null)
		{
			OuterMaterial.SetTexture("_LightingTex", LightingTex);
		}
	}
	
	public void UpdateScatteringTex()
	{
		if (InnerMaterial != null)
		{
			InnerMaterial.SetTexture("_ScatteringTex", ScatteringTex);
		}

		if (OuterMaterial != null)
		{
			OuterMaterial.SetTexture("_ScatteringTex", ScatteringTex);
		}
	}

	[ContextMenu("Update Materials")]
	public void UpdateMaterials()
	{
		updateMaterialsCalled = true;

		if (InnerMaterial == null)
		{
			InnerMaterial = SgtHelper.CreateTempMaterial("Atmosphere Inner (Generated)", SgtHelper.ShaderNamePrefix + "AtmosphereInner");

			if (InnerRenderers != null)
			{
				for (var i = InnerRenderers.Count - 1; i >= 0; i--)
				{
					var innerRenderer = InnerRenderers[i];

					if (innerRenderer != null)
					{
						SgtHelper.AddMaterial(innerRenderer, InnerMaterial);
					}
				}
			}
		}

		if (OuterMaterial == null)
		{
			OuterMaterial = SgtHelper.CreateTempMaterial("Atmosphere Outer (Generated)", SgtHelper.ShaderNamePrefix + "AtmosphereOuter");

			if (Outers != null)
			{
				for (var i = Outers.Count - 1; i >= 0; i--)
				{
					var outer = Outers[i];

					if (outer != null)
					{
						outer.SetMaterial(OuterMaterial);
					}
				}
			}
		}
		
		var color       = SgtHelper.Brighten(Color, Brightness);
		var renderQueue = (int)RenderQueue + RenderQueueOffset;

		InnerMaterial.renderQueue = renderQueue;
		OuterMaterial.renderQueue = renderQueue;
		
		InnerMaterial.SetColor("_Color", color);
		OuterMaterial.SetColor("_Color", color);

		InnerMaterial.SetTexture("_DepthTex", InnerDepthTex);
		OuterMaterial.SetTexture("_DepthTex", OuterDepthTex);

		if (Lit == true)
		{
			InnerMaterial.SetTexture("_LightingTex", LightingTex);
			OuterMaterial.SetTexture("_LightingTex", LightingTex);

			if (Scattering == true)
			{
				OuterMaterial.SetTexture("_ScatteringTex", ScatteringTex);

				SgtHelper.EnableKeyword("SGT_B", OuterMaterial); // Scattering

				if (GroundScattering == true)
				{
					InnerMaterial.SetTexture("_ScatteringTex", ScatteringTex);

					SgtHelper.EnableKeyword("SGT_B", InnerMaterial); // Scattering
				}
				else
				{
					SgtHelper.DisableKeyword("SGT_B", InnerMaterial); // Scattering
				}
			}
			else
			{
				SgtHelper.DisableKeyword("SGT_B", InnerMaterial); // Scattering
				SgtHelper.DisableKeyword("SGT_B", OuterMaterial); // Scattering
			}
		}
		
		SgtHelper.SetTempMaterial(InnerMaterial, OuterMaterial);
		
		UpdateMaterialNonSerialized();
	}

	private SgtTerrain terrain;

	[ContextMenu("Apply Inner Material")]
	public void ApplyInnerMaterial()
	{
		if (InnerRenderers != null)
		{
			for (var i = InnerRenderers.Count - 1; i >= 0; i--)
			{
				SgtHelper.AddMaterial(InnerRenderers[i], InnerMaterial);
			}
		}

		if (UpdateTerrain() == true)
		{
			terrain.Atmosphere = this;

			terrain.UpdateMaterials();
		}
	}

	[ContextMenu("Remove Inner Material")]
	public void RemoveInnerMaterial()
	{
		if (InnerRenderers != null)
		{
			for (var i = InnerRenderers.Count - 1; i >= 0; i--)
			{
				SgtHelper.RemoveMaterial(InnerRenderers[i], InnerMaterial);
			}
		}

		if (UpdateTerrain() == true)
		{
			terrain.Atmosphere = null;

			terrain.UpdateMaterials();
		}
	}

	public static SgtAtmosphere CreateAtmosphere(int layer = 0, Transform parent = null)
	{
		return CreateAtmosphere(layer, parent, Vector3.zero, Quaternion.identity, Vector3.one);
	}

	public static SgtAtmosphere CreateAtmosphere(int layer, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
	{
		var gameObject = SgtHelper.CreateGameObject("Atmosphere", layer, parent, localPosition, localRotation, localScale);
		var atmosphere = gameObject.AddComponent<SgtAtmosphere>();

		return atmosphere;
	}

#if UNITY_EDITOR
	[MenuItem(SgtHelper.GameObjectMenuPrefix + "Atmosphere", false, 10)]
	public static void CreateAtmosphereMenuItem()
	{
		var parent     = SgtHelper.GetSelectedParent();
		var atmosphere = CreateAtmosphere(parent != null ? parent.gameObject.layer : 0, parent);

		SgtHelper.SelectAndPing(atmosphere);
	}
#endif
	
	public void AddInnerRenderer(MeshRenderer renderer)
	{
		if (renderer != null)
		{
			if (InnerRenderers == null)
			{
				InnerRenderers = new List<MeshRenderer>();
			}

			if (InnerRenderers.Contains(renderer) == false)
			{
				SgtHelper.AddMaterial(renderer, InnerMaterial);

				InnerRenderers.Add(renderer);
			}
		}
	}

	public void RemoveRenderer(MeshRenderer renderer)
	{
		if (renderer != null && InnerRenderers != null)
		{
			SgtHelper.RemoveMaterial(renderer, InnerMaterial);
			
			InnerRenderers.Remove(renderer);
		}
	}
	
	[ContextMenu("Update Outers")]
	public void UpdateOuters()
	{
		updateOutersCalled = true;

		var meshCount  = OuterMeshes != null ? OuterMeshes.Count : 0;
		var outerScale = SgtHelper.Divide(OuterRadius, OuterMeshRadius);

		for (var i = 0; i < meshCount; i++)
		{
			var outerMesh = OuterMeshes[i];
			var outer     = GetOrAddOuter(i);

			outer.SetMesh(outerMesh);
			outer.SetMaterial(OuterMaterial);
			outer.SetScale(outerScale);
		}

		// Remove any excess
		if (Outers != null)
		{
			for (var i = Outers.Count - 1; i >= meshCount; i--)
			{
				var outer = Outers[i];

				if (outer != null)
				{
					SgtAtmosphereOuter.Pool(outer);
				}

				Outers.RemoveAt(i);
			}
		}
	}
	
	protected virtual void OnEnable()
	{
		AllAtmospheres.Add(this);

		Camera.onPreRender += CameraPreRender;

		if (InnerRenderers != null)
		{
			for (var i = InnerRenderers.Count - 1; i >= 0; i--)
			{
				SgtHelper.ReplaceMaterial(InnerRenderers[i], InnerMaterial);
			}
		}

		if (Outers != null)
		{
			for (var i = Outers.Count - 1; i >= 0; i--)
			{
				var outer = Outers[i];

				if (outer != null)
				{
					outer.gameObject.SetActive(true);
				}
			}
		}

		if (startCalled == true)
		{
			CheckUpdateCalls();
		}

		ApplyInnerMaterial();
	}

	protected virtual void Start()
	{
		if (startCalled == false)
		{
			startCalled = true;
			
			StartOnce();
		}
	}

	protected virtual void LateUpdate()
	{
		// The lights and shadows may have moved, so write them
		if (InnerMaterial != null && OuterMaterial != null)
		{
			SgtHelper.SetTempMaterial(InnerMaterial, OuterMaterial);

			SgtHelper.WriteLights(Lit, Lights, 2, transform.position, transform, null, SgtHelper.Brighten(Color, Brightness), ScatteringStrength);
			SgtHelper.WriteShadows(Shadows, 2);
		}
	}
	
	protected virtual void OnDisable()
	{
		AllAtmospheres.Remove(this);

		Camera.onPreRender -= CameraPreRender;

		RemoveInnerMaterial();

		if (Outers != null)
		{
			for (var i = Outers.Count - 1; i >= 0; i--)
			{
				var outer = Outers[i];

				if (outer != null)
				{
					outer.gameObject.SetActive(false);
				}
			}
		}
	}

	protected virtual void OnDestroy()
	{
		if (Outers != null)
		{
			for (var i = Outers.Count - 1; i >= 0; i--)
			{
				SgtAtmosphereOuter.MarkForDestruction(Outers[i]);
			}
		}
		
		SgtHelper.Destroy(OuterMaterial);
		SgtHelper.Destroy(InnerMaterial);
	}

#if UNITY_EDITOR
	protected virtual void OnDrawGizmosSelected()
	{
		if (SgtHelper.Enabled(this) == true)
		{
			var r1 = InnerMeshRadius;
			var r2 = OuterRadius;

			SgtHelper.DrawSphere(transform.position, transform.right * transform.lossyScale.x * r1, transform.up * transform.lossyScale.y * r1, transform.forward * transform.lossyScale.z * r1);
			SgtHelper.DrawSphere(transform.position, transform.right * transform.lossyScale.x * r2, transform.up * transform.lossyScale.y * r2, transform.forward * transform.lossyScale.z * r2);
		}
	}
#endif
	
	private bool UpdateTerrain()
	{
		if (terrain != null && terrain.Atmosphere != this)
		{
			terrain = null;
		}

		if (terrain == null)
		{
			terrain = GetComponentInParent<SgtTerrain>();
		}

		return terrain != null;
	}

	private void CameraPreRender(Camera camera)
	{
		if (InnerMaterial != null && OuterMaterial != null)
		{
			var cameraPosition       = camera.transform.position;
			var localCameraPosition  = transform.InverseTransformPoint(cameraPosition);
			var localDistance        = localCameraPosition.magnitude;
			var clampedSky           = Mathf.InverseLerp(OuterRadius, InnerMeshRadius, localDistance);

			var innerAtmosphereDepth = default(float);
			var outerAtmosphereDepth = default(float);
			var radiusRatio          = SgtHelper.Divide(InnerMeshRadius, OuterRadius);
			var scaleDistance        = SgtHelper.Divide(localDistance, OuterRadius);
			var densityScale         = 1.0f - Fog;

			SgtHelper.CalculateAtmosphereThicknessAtHorizon(radiusRatio, 1.0f, scaleDistance, out innerAtmosphereDepth, out outerAtmosphereDepth);
			
			SgtHelper.SetTempMaterial(InnerMaterial, OuterMaterial);

			if (scaleDistance > 1.0f)
			{
				SgtHelper.EnableKeyword("SGT_A"); // Outside
			}
			else
			{
				SgtHelper.DisableKeyword("SGT_A"); // Outside
			}

			InnerMaterial.SetFloat("_HorizonLengthRecip", SgtHelper.Reciprocal(innerAtmosphereDepth * densityScale));
			OuterMaterial.SetFloat("_HorizonLengthRecip", SgtHelper.Reciprocal(outerAtmosphereDepth * densityScale));

			if (OuterDepthTex != null)
			{
#if UNITY_EDITOR
				SgtHelper.MakeTextureReadable(OuterDepthTex);
#endif
				OuterMaterial.SetFloat("_Sky", Sky * OuterDepthTex.GetPixelBilinear(clampedSky / densityScale, 0.0f).a);
			}

			UpdateMaterialNonSerialized();
		}
	}

	private void UpdateMaterialNonSerialized()
	{
		var scale        = SgtHelper.Divide(OuterMeshRadius, OuterRadius);
		var worldToLocal = SgtHelper.Scaling(scale) * transform.worldToLocalMatrix;

		InnerMaterial.SetMatrix("_WorldToLocal", worldToLocal);
		OuterMaterial.SetMatrix("_WorldToLocal", worldToLocal);
	}
	
	private void StartOnce()
	{
		// Is this corona being added to a terrain?
		var terrain = GetComponent<SgtTerrain>();

		if (terrain != null)
		{
			terrain.Atmosphere = this;

			terrain.UpdateMaterialsDirty();
		}
		// Is this corona being added to a sphere?
		else
		{
			if (InnerRenderers == null)
			{
				var meshRenderer = GetComponentInParent<MeshRenderer>();

				if (meshRenderer != null)
				{
					var meshFilter = meshRenderer.GetComponent<MeshFilter>();

					if (meshFilter != null)
					{
						var mesh = meshFilter.sharedMesh;

						if (mesh != null)
						{
							var min = mesh.bounds.min;
							var max = mesh.bounds.max;
							var avg = Mathf.Abs(min.x) + Mathf.Abs(min.y) + Mathf.Abs(min.z) + Mathf.Abs(max.x) + Mathf.Abs(max.y) + Mathf.Abs(max.z);

							InnerMeshRadius = avg / 6.0f;
							InnerRenderers  = new List<MeshRenderer>();

							InnerRenderers.Add(meshRenderer);
						}
					}
				}
			}
		}
		
#if UNITY_EDITOR
		// Add an outer mesh?
		if (OuterMeshes == null)
		{
			var mesh = SgtHelper.LoadFirstAsset<Mesh>("Geosphere40 t:mesh");

			if (mesh != null)
			{
				OuterMeshes = new List<Mesh>();

				OuterMeshes.Add(mesh);
			}
		}
#endif
		CheckUpdateCalls();
	}

	private void CheckUpdateCalls()
	{
		if (updateMaterialsCalled == false)
		{
			UpdateMaterials();
		}

		if (updateOutersCalled == false)
		{
			UpdateOuters();
		}
	}

	private SgtAtmosphereOuter GetOrAddOuter(int index)
	{
		var outer = default(SgtAtmosphereOuter);

		if (Outers == null)
		{
			Outers = new List<SgtAtmosphereOuter>();
		}

		if (index < Outers.Count)
		{
			outer = Outers[index];

			if (outer == null)
			{
				outer = SgtAtmosphereOuter.Create(this);

				Outers[index] = outer;
			}
		}
		else
		{
			outer = SgtAtmosphereOuter.Create(this);

			Outers.Add(outer);
		}

		return outer;
	}
}