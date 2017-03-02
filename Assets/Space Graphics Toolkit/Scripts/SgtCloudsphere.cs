using UnityEngine;
using UnityEngine.Serialization;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(SgtCloudsphere))]
public class SgtCloudsphere_Editor : SgtEditor<SgtCloudsphere>
{
	protected override void OnInspector()
	{
		var updateMaterial = false;
		var updateModels   = false;
		
		DrawDefault("Color", ref updateMaterial);
		BeginError(Any(t => t.Brightness <= 0.0f));
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
		BeginError(Any(t => t.Radius < 0.0f));
			DrawDefault("Radius", ref updateModels);
		EndError();
		DrawDefault("CameraOffset"); // Updated automatically

		Separator();

		DrawDefault("Fade", ref updateMaterial);

		if (Any(t => t.Fade == true))
		{
			BeginIndent();
				BeginError(Any(t => t.FadeTex == null));
					DrawDefault("FadeTex", ref updateMaterial);
				EndError();
				BeginError(Any(t => t.FadeDistance <= 0.0f));
					DrawDefault("FadeDistance", ref updateMaterial);
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

		Separator();

		DrawDefault("Lit", ref updateModels);

		if (Any(t => t.Lit == true))
		{
			BeginIndent();
				BeginError(Any(t => t.LightingTex == null));
					DrawDefault("LightingTex", ref updateMaterial);
				EndError();
				BeginError(Any(t => t.Lights != null && (t.Lights.Count == 0 || t.Lights.Exists(l => l == null))));
					DrawDefault("Lights", ref updateMaterial);
				EndError();
				BeginError(Any(t => t.Shadows != null && t.Shadows.Exists(s => s == null)));
					DrawDefault("Shadows", ref updateMaterial);
				EndError();
			EndIndent();
		}

		if (Any(t => t.DepthTex == null && t.GetComponent<SgtCloudsphereDepth>() == null))
		{
			Separator();

			if (Button("Add Depth") == true)
			{
				Each(t => SgtHelper.GetOrAddComponent<SgtCloudsphereDepth>(t.gameObject));
			}
		}

		if (Any(t => t.Lit == true && t.LightingTex == null && t.GetComponent<SgtCloudsphereLighting>() == null))
		{
			Separator();

			if (Button("Add Lighting") == true)
			{
				Each(t => SgtHelper.GetOrAddComponent<SgtCloudsphereLighting>(t.gameObject));
			}
		}

		if (Any(t => t.Fade == true && t.FadeTex == null && t.GetComponent<SgtCloudsphereFade>() == null))
		{
			Separator();

			if (Button("Add Fade") == true)
			{
				Each(t => SgtHelper.GetOrAddComponent<SgtCloudsphereFade>(t.gameObject));
			}
		}
		
		if (updateMaterial == true) DirtyEach(t => t.UpdateMaterial());
		if (updateModels   == true) DirtyEach(t => t.UpdateModels  ());
	}
}
#endif

[ExecuteInEditMode]
[AddComponentMenu("Space Graphics Toolkit/SGT Cloudsphere")]
public class SgtCloudsphere : MonoBehaviour
{
	// All active and enabled cloudspheres in the scene
	public static List<SgtCloudsphere> AllCloudspheres = new List<SgtCloudsphere>();
	
	[Tooltip("The color tint")]
	public Color Color = Color.white;

	[Tooltip("The Color.rgb values are multiplied by this")]
	public float Brightness = 1.0f;

	[Tooltip("The radius of the cloudsphere meshes specified below")]
	public float MeshRadius = 1.0f;

	[Tooltip("The meshes used to build the cloudsphere (should be a sphere)")]
	public List<Mesh> Meshes;

	[Tooltip("The render queue group for this cloudsphere")]
	public SgtRenderQueue RenderQueue = SgtRenderQueue.Transparent;

	[Tooltip("The render queue offset for this cloudsphere")]
	public int RenderQueueOffset;

	[Tooltip("The desired radius of the cloudsphere in local coordinates")]
	public float Radius = 1.5f;

	[Tooltip("Should the clouds fade out when the camera gets near?")]
	public bool Fade;

	[Tooltip("The lookup table used to calculate the fade")]
	public Texture FadeTex;
	
	[Tooltip("The distance the fading begins from in world space")]
	public float FadeDistance = 1.0f;

	[Tooltip("The amount the clouds get moved toward the current camera")]
	[FormerlySerializedAs("ObserverOffset")]
	public float CameraOffset;

	[Tooltip("The cubemap used to render the clouds")]
	public Cubemap MainTex;
	
	[Tooltip("The lookup table used for depth color and opacity (bottom = no depth/space, top = maximum depth/center)")]
	public Texture2D DepthTex;

	[Tooltip("Does this cloudsphere receive light?")]
	public bool Lit;

	[Tooltip("The lookup table used to calculate the lighting color and brightness")]
	public Texture LightingTex;

	[Tooltip("The lights shining on this cloudsphere")]
	public List<Light> Lights;

	[Tooltip("The shadows casting on this cloudsphere")]
	public List<SgtShadow> Shadows;
	
	// The material applied to all models
	[System.NonSerialized]
	public Material Material;
	
	// The models used to render this cloudsphere
	[SerializeField]
	public List<SgtCloudsphereModel> Models;
	
	[SerializeField]
	private bool startCalled;
	
	[System.NonSerialized]
	private bool updateMaterialCalled;

	[System.NonSerialized]
	private bool updateModelsCalled;
	
	public void UpdateDeptchTex()
	{
		if (Material != null)
		{
			Material.SetTexture("_DepthTex", DepthTex);
		}
	}
	
	public void UpdateFadeTex()
	{
		if (Material != null)
		{
			Material.SetTexture("_FadeTex", FadeTex);
		}
	}
	
	public void UpdateLightingTex()
	{
		if (Material != null)
		{
			Material.SetTexture("_LightingTex", LightingTex);
		}
	}
	
	[ContextMenu("Update Material")]
	public void UpdateMaterial()
	{
		updateMaterialCalled = true;

		if (Material == null)
		{
			Material = SgtHelper.CreateTempMaterial("Cloudsphere (Generated)", SgtHelper.ShaderNamePrefix + "Cloudsphere");

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
		
		var renderQueue = (int)RenderQueue + RenderQueueOffset;
		var color       = SgtHelper.Brighten(Color, Brightness);
		
		Material.renderQueue = renderQueue;
		
		Material.SetColor("_Color", color);
		Material.SetTexture("_MainTex", MainTex);
		Material.SetTexture("_DepthTex", DepthTex);

		if (Fade == true)
		{
			SgtHelper.EnableKeyword("SGT_A", Material); // Fade
			
			Material.SetTexture("_FadeTex", FadeTex);
			Material.SetFloat("_FadeDistanceRecip", SgtHelper.Reciprocal(FadeDistance));
		}
		else
		{
			SgtHelper.DisableKeyword("SGT_A", Material); // Fade
		}

		if (Lit == true)
		{
			Material.SetTexture("_LightingTex", LightingTex);
		}
	}

	[ContextMenu("Update Models")]
	public void UpdateModels()
	{
		updateModelsCalled = true;

		var meshCount = Meshes != null ? Meshes.Count : 0;
		var scale     = SgtHelper.Divide(Radius, MeshRadius);

		for (var i = 0; i < meshCount; i++)
		{
			var mesh  = Meshes[i];
			var model = GetOrAddModel(i);

			model.SetMesh(mesh);
			model.SetMaterial(Material);
			model.SetScale(scale);
		}

		// Remove any excess
		if (Models != null)
		{
			for (var i = Models.Count - 1; i >= meshCount; i--)
			{
				SgtCloudsphereModel.Pool(Models[i]);

				Models.RemoveAt(i);
			}
		}
	}

	public static SgtCloudsphere CreateCloudsphere(int layer = 0, Transform parent = null)
	{
		return CreateCloudsphere(layer, parent, Vector3.zero, Quaternion.identity, Vector3.one);
	}

	public static SgtCloudsphere CreateCloudsphere(int layer, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
	{
		var gameObject  = SgtHelper.CreateGameObject("Cloudsphere", layer, parent, localPosition, localRotation, localScale);
		var cloudsphere = gameObject.AddComponent<SgtCloudsphere>();

		return cloudsphere;
	}

#if UNITY_EDITOR
	[MenuItem(SgtHelper.GameObjectMenuPrefix + "Cloudsphere", false, 10)]
	public static void CreateCloudsphereMenuItem()
	{
		var parent      = SgtHelper.GetSelectedParent();
		var cloudsphere = CreateCloudsphere(parent != null ? parent.gameObject.layer : 0, parent);

		SgtHelper.SelectAndPing(cloudsphere);
	}
#endif
	
	protected virtual void OnEnable()
	{
		AllCloudspheres.Add(this);

		Camera.onPreCull    += CameraPreCull;
		Camera.onPreRender  += CameraPreRender;
		Camera.onPostRender += CameraPostRender;
		
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

		if (startCalled == true)
		{
			CheckUpdateCalls();
		}
	}

	protected virtual void Start()
	{
		if (startCalled == false)
		{
			startCalled = true;
			
			// Add a mesh?
#if UNITY_EDITOR
			if (Meshes == null)
			{
				var mesh = SgtHelper.LoadFirstAsset<Mesh>("Geosphere40 t:mesh");

				if (mesh != null)
				{
					Meshes = new List<Mesh>();

					Meshes.Add(mesh);
				}
			}
#endif

			CheckUpdateCalls();
		}
	}

	protected virtual void LateUpdate()
	{
		// The lights and shadows may have moved, so write them
		if (Material != null)
		{
			SgtHelper.SetTempMaterial(Material);

			SgtHelper.WriteLights(Lit, Lights, 2, transform.position, null, null, SgtHelper.Brighten(Color, Brightness), 1.0f);
			SgtHelper.WriteShadows(Shadows, 2);
		}
	}

	protected virtual void OnDisable()
	{
		AllCloudspheres.Remove(this);

		Camera.onPreCull    -= CameraPreCull;
		Camera.onPreRender  -= CameraPreRender;
		Camera.onPostRender -= CameraPostRender;
		
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
				SgtCloudsphereModel.MarkForDestruction(Models[i]);
			}
		}

		SgtHelper.Destroy(Material);
	}

	private void CameraPreCull(Camera camera)
	{
		if (Material != null)
		{
			UpdateMaterialNonSerialized();
		}

		if (CameraOffset != 0.0f)
		{
			if (Models != null)
			{
				for (var i = Models.Count - 1; i >= 0; i--)
				{
					var model = Models[i];

					if (model != null)
					{
						var modelTransform = model.transform;
						var cameraDir      = (modelTransform.position - camera.transform.position).normalized;
						
						model.BeginSave();
						{
							modelTransform.position += cameraDir * CameraOffset;
						}
						model.EndSave(camera);
					}
				}
			}
		}
	}

	private void CameraPreRender(Camera camera)
	{
		if (Models != null)
		{
			for (var i = Models.Count - 1; i >= 0; i--)
			{
				var model = Models[i];

				if (model != null)
				{
					model.Restore(camera);
				}
			}
		}
	}

	private void CameraPostRender(Camera camera)
	{
		if (Models != null)
		{
			for (var i = Models.Count - 1; i >= 0; i--)
			{
				var model = Models[i];

				if (model != null)
				{
					model.Restore();
				}
			}
		}
	}
	
	private void UpdateMaterialNonSerialized()
	{
		SgtHelper.SetTempMaterial(Material);

		SgtHelper.WriteShadowsNonSerialized(Shadows, 2);
	}
	private SgtCloudsphereModel GetOrAddModel(int index)
	{
		var model = default(SgtCloudsphereModel);

		if (Models == null)
		{
			Models = new List<SgtCloudsphereModel>();
		}

		if (index < Models.Count)
		{
			model = Models[index];

			if (model == null)
			{
				model = SgtCloudsphereModel.Create(this);

				Models[index] = model;
			}
		}
		else
		{
			model = SgtCloudsphereModel.Create(this);

			Models.Add(model);
		}

		return model;
	}

	private void CheckUpdateCalls()
	{
		if (updateMaterialCalled == false)
		{
			UpdateMaterial();
		}

		if (updateModelsCalled == false)
		{
			UpdateModels();
		}
	}
}