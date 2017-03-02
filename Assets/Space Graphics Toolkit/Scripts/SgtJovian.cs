using UnityEngine;
using UnityEngine.Serialization;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(SgtJovian))]
public class SgtJovian_Editor : SgtEditor<SgtJovian>
{
	protected override void OnInspector()
	{
		var updateMaterial = false;
		var updateModels   = false;
		
		DrawDefault("Color", ref updateMaterial);
		BeginError(Any(t => t.Brightness < 0.0f));
			DrawDefault("Brightness", ref updateMaterial);
		EndError();
		DrawDefault("RenderQueue", ref updateMaterial);
		DrawDefault("RenderQueueOffset", ref updateMaterial);

		Separator();

		BeginError(Any(t => t.MainTex == null));
			DrawDefault("MainTex", ref updateMaterial);
		EndError();
		BeginError(Any(t => t.DepthTex == null));
			DrawDefault("DepthTex", ref updateMaterial);
		EndError();
		
		Separator();

		BeginError(Any(t => t.Sky < 0.0f));
			DrawDefault("Sky"); // Updated when rendering
		EndError();

		DrawDefault("Lit", ref updateMaterial);

		if (Any(t => t.Lit == true))
		{
			BeginIndent();
				BeginError(Any(t => t.LightingTex == null));
					DrawDefault("LightingTex", ref updateMaterial);
				EndError();
				DrawDefault("Scattering", ref updateMaterial);

				if (Any(t => t.Scattering == true))
				{
					BeginIndent();
						BeginError(Any(t => t.ScatteringTex == null));
							DrawDefault("ScatteringTex", ref updateMaterial);
						EndError();
						DrawDefault("ScatteringStrength", ref updateMaterial);
					EndIndent();
				}

				BeginError(Any(t => t.Lights != null && (t.Lights.Count == 0 || t.Lights.Exists(l => l == null))));
					DrawDefault("Lights", ref updateMaterial);
				EndError();
				BeginError(Any(t => t.Shadows != null && t.Shadows.Exists(s => s == null)));
					DrawDefault("Shadows", ref updateMaterial);
				EndError();
			EndIndent();
		}
		
		Separator();
		
		BeginError(Any(t => t.MeshRadius <= 0.0f));
			DrawDefault("MeshRadius", ref updateModels);
		EndError();
		BeginError(Any(t => t.Meshes != null && t.Meshes.Count == 0));
			DrawDefault("Meshes", ref updateModels);
		EndError();
		
		if (Any(t => t.DepthTex == null && t.GetComponent<SgtJovianDepth>() == null))
		{
			Separator();

			if (Button("Add Depth") == true)
			{
				Each(t => SgtHelper.GetOrAddComponent<SgtJovianDepth>(t.gameObject));
			}
		}

		if (Any(t => t.Lit == true && t.LightingTex == null && t.GetComponent<SgtJovianLighting>() == null))
		{
			Separator();

			if (Button("Add Lighting") == true)
			{
				Each(t => SgtHelper.GetOrAddComponent<SgtJovianLighting>(t.gameObject));
			}
		}

		if (Any(t => t.Lit == true && t.Scattering == true && t.ScatteringTex == null && t.GetComponent<SgtJovianScattering>() == null))
		{
			Separator();

			if (Button("Add Scattering") == true)
			{
				Each(t => SgtHelper.GetOrAddComponent<SgtJovianScattering>(t.gameObject));
			}
		}

		serializedObject.ApplyModifiedProperties();

		if (updateMaterial == true) DirtyEach(t => t.UpdateMaterial());
		if (updateModels   == true) DirtyEach(t => t.UpdateModels  ());
	}
}
#endif

[ExecuteInEditMode]
[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Jovian")]
public class SgtJovian : MonoBehaviour
{
	// All currently active and enabled jovians
	public static List<SgtJovian> AllJovians = new List<SgtJovian>();
	
	[Tooltip("The color tint")]
	public Color Color = Color.white;

	[Tooltip("The Color.rgb values are multiplied by this")]
	public float Brightness = 1.0f;

	[Tooltip("The render queue group for this jovian")]
	public SgtRenderQueue RenderQueue = SgtRenderQueue.Transparent;

	[Tooltip("The render queue offset for this jovian")]
	public int RenderQueueOffset;

	[Tooltip("The main cube map texture used to render the jovian")]
	public Cubemap MainTex;

	[Tooltip("The lookup table used for depth color and opacity (bottom = no depth/space, top = maximum depth/center)")]
	public Texture2D DepthTex;
	
	[Tooltip("The opacity multiplier of the sky when the camera is inside the sky mesh")]
	public float Sky = 1.0f;

	[Tooltip("Does this jovian receive light?")]
	public bool Lit;
	
	[Tooltip("The lookup table used to calculate the lighting color and brightness")]
	public Texture LightingTex;

	[Tooltip("The lights shining on this jovian")]
	public List<Light> Lights;

	[Tooltip("The shadows casting on this jovian")]
	public List<SgtShadow> Shadows;

	[Tooltip("Should lights scatter through the atmosphere?")]
	public bool Scattering;

	[Tooltip("The color applied to the jovian based on the lighting (bottom = dark side, top = light side)")]
	public Texture ScatteringTex;

	[Tooltip("The scattering brightness multiplier")]
	public float ScatteringStrength = 3.0f;

	[Tooltip("The radius of the jovian meshes specified below")]
	public float MeshRadius = 1.0f;

	[Tooltip("The meshes used to build the jovian (should be a sphere)")]
	public List<Mesh> Meshes;
	
	// "The models used to render the full jovian
	[FormerlySerializedAs("models")]
	public List<SgtJovianModel> Models;
	
	// The material applied to all models
	[System.NonSerialized]
	public Material Material;
	
	public virtual void UpdateDepthTex()
	{
		if (Material != null)
		{
			Material.SetTexture("_DepthTex", DepthTex);
		}
	}
	
	public virtual void UpdateLightingTex()
	{
		if (Material != null)
		{
			Material.SetTexture("_LightingTex", LightingTex);
		}
	}
	
	public virtual void UpdateScatteringTex()
	{
		if (Material != null)
		{
			Material.SetTexture("_ScatteringTex", ScatteringTex);
		}
	}

	[ContextMenu("Update Material")]
	public void UpdateMaterial()
	{
		if (Material == null)
		{
			Material = SgtHelper.CreateTempMaterial("Jovian Material (Generated)", SgtHelper.ShaderNamePrefix + "Jovian");

			if (Models != null)
			{
				for (var i = Models.Count - 1; i >= 0; i--)
				{
					var model = Models[i];

					if (model != null)
					{
						model.SetMaterial(Material);
					}
				}
			}
		}

		Material.renderQueue = (int)RenderQueue + RenderQueueOffset;

		Material.SetTexture("_MainTex", MainTex);
		Material.SetTexture("_DepthTex", DepthTex);
		Material.SetColor("_Color", SgtHelper.Brighten(Color, Brightness));
		
		if (Lit == true)
		{
			Material.SetTexture("_LightingTex", LightingTex);
		}

		SgtHelper.SetTempMaterial(Material);
		
		if (Scattering == true)
		{
			Material.SetTexture("_ScatteringTex", ScatteringTex);

			SgtHelper.EnableKeyword("SGT_B"); // Scattering
		}
		else
		{
			SgtHelper.DisableKeyword("SGT_B"); // Scattering
		}

		UpdateMaterialNonSerialized();
	}
	
	[ContextMenu("Update Models")]
	public void UpdateModels()
	{
		var meshCount = Meshes != null ? Meshes.Count : 0;
		
		for (var i = 0; i < meshCount; i++)
		{
			var mesh  = Meshes[i];
			var model = GetOrAddModel(i);

			model.SetMesh(mesh);
			model.SetMaterial(Material);
		}

		// Remove any excess
		if (Models != null)
		{
			for (var i = Models.Count - 1; i >= meshCount; i--)
			{
				SgtJovianModel.Pool(Models[i]);

				Models.RemoveAt(i);
			}
		}
	}

	public static SgtJovian CreateJovian(int layer = 0, Transform parent = null)
	{
		return CreateJovian(layer, parent, Vector3.zero, Quaternion.identity, Vector3.one);
	}

	public static SgtJovian CreateJovian(int layer, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
	{
		var gameObject = SgtHelper.CreateGameObject("Jovian", layer, parent, localPosition, localRotation, localScale);
		var jovian     = gameObject.AddComponent<SgtJovian>();

		return jovian;
	}

#if UNITY_EDITOR
	[MenuItem(SgtHelper.GameObjectMenuPrefix + "Jovian", false, 10)]
	public static void CreateJovianMenuItem()
	{
		var parent = SgtHelper.GetSelectedParent();
		var jovian = CreateJovian(parent != null ? parent.gameObject.layer : 0, parent);

		SgtHelper.SelectAndPing(jovian);
	}
#endif

	protected virtual void OnEnable()
	{
		AllJovians.Add(this);

		Camera.onPreRender += CameraPreRender;

		if (Models != null)
		{
			for (var i = Models.Count - 1; i >= 0; i--)
			{
				var model = Models[i];

				if (model != null)
				{
					model.gameObject.SetActive(true);
				}
			}
		}
	}
	
	protected virtual void Start()
	{
		if (Material == null)
		{
			UpdateMaterial();
		}
#if UNITY_EDITOR
		// Add a mesh?
		if (Meshes == null)
		{
			var mesh = SgtHelper.LoadFirstAsset<Mesh>("Geosphere40 t:mesh");

			if (mesh != null)
			{
				Meshes = new List<Mesh>();

				Meshes.Add(mesh);
			}

			UpdateModels();
		}
#endif
	}

	protected virtual void LateUpdate()
	{
		// The lights and shadows may have moved, so write them
		if (Material != null)
		{
			SgtHelper.SetTempMaterial(Material);

			SgtHelper.WriteLights(Lit, Lights, 2, transform.position, transform, null, SgtHelper.Brighten(Color, Brightness), ScatteringStrength);
			SgtHelper.WriteShadows(Shadows, 2);
		}
	}

	protected virtual void OnDisable()
	{
		AllJovians.Remove(this);

		Camera.onPreRender -= CameraPreRender;

		if (Models != null)
		{
			for (var i = Models.Count - 1; i >= 0; i--)
			{
				var model = Models[i];

				if (model != null)
				{
					model.gameObject.SetActive(false);
				}
			}
		}
	}

	protected virtual void OnDestroy()
	{
		if (Models != null)
		{
			for (var i = Models.Count - 1; i >= 0; i--)
			{
				SgtJovianModel.MarkForDestruction(Models[i]);
			}
		}

		SgtHelper.Destroy(Material);
	}

#if UNITY_EDITOR
	protected virtual void OnDrawGizmosSelected()
	{
		if (SgtHelper.Enabled(this) == true)
		{
			var r0 = transform.lossyScale;

			SgtHelper.DrawSphere(transform.position, transform.right * r0.x, transform.up * r0.y, transform.forward * r0.z);
		}
	}
#endif

	private void UpdateMaterialNonSerialized()
	{
		var localToWorld = transform.localToWorldMatrix * SgtHelper.Scaling(MeshRadius * 2.0f); // Double mesh radius so the max thickness caps at 1.0
		
		Material.SetMatrix("_WorldToLocal", localToWorld.inverse);

		Material.SetMatrix("_LocalToWorld", localToWorld);
		
		SgtHelper.SetTempMaterial(Material);

		SgtHelper.WriteShadowsNonSerialized(Shadows, 2);
	}
	
	private void CameraPreRender(Camera camera)
	{
		if (Material != null)
		{
			var cameraPosition      = camera.transform.position;
			var localCameraPosition = transform.InverseTransformPoint(cameraPosition);
			var localDistance       = localCameraPosition.magnitude;
			var scaleDistance       = SgtHelper.Divide(localDistance, MeshRadius);

			if (scaleDistance > 1.0f)
			{
				SgtHelper.EnableKeyword("SGT_A", Material); // Outside
			}
			else
			{
				SgtHelper.DisableKeyword("SGT_A", Material); // Outside

				if (DepthTex != null)
				{
#if UNITY_EDITOR
					SgtHelper.MakeTextureReadable(DepthTex);
#endif
					Material.SetFloat("_Sky", Sky * DepthTex.GetPixelBilinear(1.0f - scaleDistance, 0.0f).a);
				}
			}
			
			UpdateMaterialNonSerialized();
		}
	}

	private SgtJovianModel GetOrAddModel(int index)
	{
		var model = default(SgtJovianModel);

		if (Models == null)
		{
			Models = new List<SgtJovianModel>();
		}

		if (index < Models.Count)
		{
			model = Models[index];

			if (model == null)
			{
				model = SgtJovianModel.Create(this);

				Models[index] = model;
			}
		}
		else
		{
			model = SgtJovianModel.Create(this);

			Models.Add(model);
		}

		return model;
	}
}