using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(SgtFlare))]
public class SgtFlare_Editor : SgtEditor<SgtFlare>
{
	protected override void OnInspector()
	{
		var updateMesh     = false;
		var updateModel    = false;
		var updateMaterial = false;
		
		BeginError(Any(t => t.Mesh == null));
			DrawDefault("Mesh", ref updateMesh);
		EndError();
		BeginError(Any(t => t.Material == null));
			DrawDefault("Material", ref updateMaterial);
		EndError();
		DrawDefault("CameraOffset"); // Updated automatically
		DrawDefault("FollowCameras"); // Automatically updated

		if (Any(t => t.FollowCameras == true))
		{
			BeginIndent();
				BeginError(Any(t => t.FollowDistance <= 0.0f));
					DrawDefault("FollowDistance"); // Automatically updated
				EndError();
			EndIndent();
		}

		Separator();
		
		DrawDefault("Depth"); // Automatically updated

		if (Any(t => t.Mesh == null && t.GetComponent<SgtFlareMesh>() == null))
		{
			Separator();

			if (Button("Add Mesh") == true)
			{
				Each(t => SgtHelper.GetOrAddComponent<SgtFlareMesh>(t.gameObject));
			}
		}

		if (Any(t => t.Material == null && t.GetComponent<SgtFlareMaterial>() == null))
		{
			Separator();

			if (Button("Add Material") == true)
			{
				Each(t => SgtHelper.GetOrAddComponent<SgtFlareMaterial>(t.gameObject));
			}
		}

		if (updateMesh     == true) DirtyEach(t => t.UpdateMesh    ());
		if (updateModel    == true) DirtyEach(t => t.UpdateModel   ());
		if (updateMaterial == true) DirtyEach(t => t.UpdateMaterial());
	}
}
#endif

[ExecuteInEditMode]
[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Flare")]
public class SgtFlare : MonoBehaviour
{
	[Tooltip("The mesh used to render this flare")]
	public Mesh Mesh;
	
	[Tooltip("The material used to render this flare")]
	public Material Material;
	
	[Tooltip("The thickness required between the camera and this flare for the flare to be fully hidden in world space")]
	public float HideThickness = 10.0f;

	[Tooltip("If you want this flare to hide behind solid objects then set this")]
	public SgtDepth Depth;

	[Tooltip("Should the flare automatically snap to cameras ")]
	public bool FollowCameras;

	[Tooltip("The distance from the camera this flare will be placed in world space")]
	public float FollowDistance = 100.0f;

	[Tooltip("The distance this flare is moved toward the current camera when rendering in world space")]
	public float CameraOffset;

	// The model used to render this flare
	public SgtFlareModel Model;
	
	// Prevent recusive calling
	private static bool busy;
	
	[ContextMenu("Update Mesh")]
	public void UpdateMesh()
	{
		if (Model != null)
		{
			Model.SetMesh(Mesh);
		}
	}

	[ContextMenu("Update Material")]
	public void UpdateMaterial()
	{
		if (Model != null)
		{
			Model.SetMaterial(Material);
		}
	}

	[ContextMenu("Update Model")]
	public void UpdateModel()
	{
		if (Model == null)
		{
			Model = SgtFlareModel.Create(this);

			Model.SetMesh(Mesh);
			Model.SetMaterial(Material);
		}
	}
	
	public static SgtFlare CreateFlare(int layer = 0, Transform parent = null)
	{
		return CreateFlare(layer, parent, Vector3.zero, Quaternion.identity, Vector3.one);
	}

	public static SgtFlare CreateFlare(int layer, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
	{
		var gameObject = SgtHelper.CreateGameObject("Flare", layer, parent, localPosition, localRotation, localScale);
		var flare      = gameObject.AddComponent<SgtFlare>();

		return flare;
	}

#if UNITY_EDITOR
	[MenuItem(SgtHelper.GameObjectMenuPrefix + "Flare", false, 10)]
	public static void CreateFlareMenuItem()
	{
		var parent = SgtHelper.GetSelectedParent();
		var flare  = CreateFlare(parent != null ? parent.gameObject.layer : 0, parent);

		SgtHelper.SelectAndPing(flare);
	}
#endif
	
	protected virtual void OnEnable()
	{
		Camera.onPreCull    += CameraPreCull;
		Camera.onPreRender  += CameraPreRender;
		Camera.onPostRender += CameraPostRender;

		if (Model != null)
		{
			Model.gameObject.SetActive(true);
		}
	}

	protected virtual void OnDisable()
	{
		Camera.onPreCull    -= CameraPreCull;
		Camera.onPreRender  -= CameraPreRender;
		Camera.onPostRender -= CameraPostRender;

		if (Model != null)
		{
			Model.gameObject.SetActive(false);
		}
	}

	protected virtual void Start()
	{
		if (Model == null)
		{
			UpdateModel();
		}
	}
	
	protected virtual void OnDestroy()
	{
		SgtFlareModel.MarkForDestruction(Model);
	}
	
	private void CameraPreCull(Camera camera)
	{
		if (busy == true)
		{
			return;
		}

		if (Model != null)
		{
			Model.BeginSave();
			{
				if (FollowCameras == true)
				{
					Model.transform.position = camera.transform.position - Model.transform.forward * FollowDistance;
				}

				if (Depth != null)
				{
					busy = true;
					{
						Model.transform.localScale *= 1.0f - Depth.Calculate(camera.transform.position, Model.transform.position);
					}
					busy = false;
				}

				// Face camera with offset
				var cameraDir = (Model.transform.position - camera.transform.position).normalized;

				Model.transform.rotation  = camera.transform.rotation;
				Model.transform.position += cameraDir * CameraOffset;
			}
			Model.EndSave(camera);
		}
	}

	private void CameraPreRender(Camera camera)
	{
		if (busy == true)
		{
			return;
		}

		if (Model != null)
		{
			Model.Restore(camera);
		}
	}

	private void CameraPostRender(Camera camera)
	{
		if (busy == true)
		{
			return;
		}

		if (Model != null)
		{
			Model.Restore();
		}
	}
}