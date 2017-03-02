using UnityEngine;
using UnityEngine.Serialization;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(SgtSkysphere))]
public class SgtSkysphere_Editor : SgtEditor<SgtSkysphere>
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

		Separator();
		
		DrawDefault("FollowCameras", ref updateMaterial);
		
		Separator();
		
		BeginError(Any(t => t != null && (t.Meshes.Count == 0 || t.Meshes.FindIndex(m => m == null) != -1)));
			DrawDefault("Meshes", ref updateModels);
		EndError();

		if (updateMaterial == true) DirtyEach(t => t.UpdateMaterial());
		if (updateModels   == true) DirtyEach(t => t.UpdateModels  ());
	}
}
#endif

[ExecuteInEditMode]
[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Skysphere")]
public class SgtSkysphere : MonoBehaviour
{
	// All active and enabled skyspheres in the scene
	public static List<SgtSkysphere> AllSkyspheres = new List<SgtSkysphere>();

	[Tooltip("The meshes used to render this")]
	public List<Mesh> Meshes;

	[Tooltip("The color tint")]
	public Color Color = Color.white;

	[Tooltip("The color brightness")]
	public float Brightness = 1.0f;

	[Tooltip("The render queue group")]
	public SgtRenderQueue RenderQueue = SgtRenderQueue.Transparent;

	[Tooltip("The render queue offset")]
	public int RenderQueueOffset;

	[Tooltip("The texture applied to the skysphere meshes")]
	public Texture MainTex;

	[Tooltip("Should this be placed on top of the current camera when rendering?")]
	[FormerlySerializedAs("FollowObservers")]
	public bool FollowCameras;

	// The material applied to the models
	[System.NonSerialized]
	public Material Material;
	
	// The GameObjects used to render this
	public List<SgtSkysphereModel> Models;

	[SerializeField]
	[HideInInspector]
	private bool startCalled;
	
	[System.NonSerialized]
	private bool updateMaterialCalled;

	[System.NonSerialized]
	private bool updateModelsCalled;
	
	[ContextMenu("Update Material")]
	public void UpdateMaterial()
	{
		if (Material == null) Material = SgtHelper.CreateTempMaterial("Skysphere (Generated)", SgtHelper.ShaderNamePrefix + "Skysphere");
		
		Material.renderQueue = (int)RenderQueue + RenderQueueOffset;

		Material.SetTexture("_MainTex", MainTex);
		Material.SetColor("_Color", SgtHelper.Brighten(Color, Brightness));
	}

	[ContextMenu("Update Models")]
	public void UpdateModels()
	{
		updateModelsCalled = true;

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
				SgtSkysphereModel.Pool(Models[i]);

				Models.RemoveAt(i);
			}
		}
	}

	public static SgtSkysphere CreateSkysphere(int layer = 0, Transform parent = null)
	{
		return CreateSkysphere(layer, parent, Vector3.zero, Quaternion.identity, Vector3.one);
	}

	public static SgtSkysphere CreateSkysphere(int layer, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
	{
		var gameObject = SgtHelper.CreateGameObject("Skysphere", layer, parent, localPosition, localRotation, localScale);
		var skysphere  = gameObject.AddComponent<SgtSkysphere>();

		return skysphere;
	}

#if UNITY_EDITOR
	[MenuItem(SgtHelper.GameObjectMenuPrefix + "Skysphere", false, 10)]
	public static void CreateSkysphereMenuItem()
	{
		var parent    = SgtHelper.GetSelectedParent();
		var skysphere = CreateSkysphere(parent != null ? parent.gameObject.layer : 0, parent);

		SgtHelper.SelectAndPing(skysphere);
	}
#endif

	protected virtual void OnEnable()
	{
		AllSkyspheres.Add(this);

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

	protected virtual void OnDisable()
	{
		AllSkyspheres.Remove(this);

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
				SgtSkysphereModel.MarkForDestruction(Models[i]);
			}
		}

		SgtHelper.Destroy(Material);
	}

	private void CameraPreCull(Camera camera)
	{
		if (FollowCameras == true)
		{
			if (Models != null)
			{
				for (var i = Models.Count - 1; i >= 0; i--)
				{
					var model = Models[i];

					if (model != null)
					{
						model.Save();

						model.transform.position = camera.transform.position;
					}
				}
			}
		}
	}

	private void CameraPreRender(Camera camera)
	{
		CameraPreCull(camera);
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

	private SgtSkysphereModel GetOrAddModel(int index)
	{
		var model = default(SgtSkysphereModel);

		if (Models == null)
		{
			Models = new List<SgtSkysphereModel>();
		}

		if (index < Models.Count)
		{
			model = Models[index];

			if (model == null)
			{
				model = SgtSkysphereModel.Create(this);

				Models[index] = model;
			}
		}
		else
		{
			model = SgtSkysphereModel.Create(this);

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