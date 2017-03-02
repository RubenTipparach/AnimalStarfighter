using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(SgtCloudsphereModel))]
public class SgtCloudsphereModel_Editor : SgtEditor<SgtCloudsphereModel>
{
	protected override void OnInspector()
	{
		BeginDisabled();
			DrawDefault("Cloudsphere");
		EndDisabled();
	}
}
#endif

[ExecuteInEditMode]
[AddComponentMenu("")]
[DisallowMultipleComponent]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class SgtCloudsphereModel : MonoBehaviour
{
	public class CameraState : SgtCameraState
	{
		public Vector3 LocalPosition;
	}

	[Tooltip("The cloudsphere this belongs to")]
	public SgtCloudsphere Cloudsphere;

	[System.NonSerialized]
	private MeshFilter meshFilter;
	
	[System.NonSerialized]
	private MeshRenderer meshRenderer;

	[System.NonSerialized]
	private Mesh mesh;

	[System.NonSerialized]
	private Material material;

	[System.NonSerialized]
	private List<CameraState> cameraStates;
	
	public void SetMesh(Mesh newMesh)
	{
		if (newMesh != mesh)
		{
			if (meshFilter == null) meshFilter = gameObject.GetComponent<MeshFilter>();
			
			mesh = meshFilter.sharedMesh = newMesh;
		}
	}

	public void SetMaterial(Material newMaterial)
	{
		if (newMaterial != material)
		{
			if (meshRenderer == null) meshRenderer = gameObject.GetComponent<MeshRenderer>();
			
			material = meshRenderer.sharedMaterial = newMaterial;
		}
	}

	public void SetScale(float scale)
	{
		SgtHelper.SetLocalScale(transform, scale);
	}

	public static SgtCloudsphereModel Create(SgtCloudsphere cloudsphere)
	{
		var model = SgtComponentPool<SgtCloudsphereModel>.Pop(cloudsphere.transform, "Model", cloudsphere.gameObject.layer);

		model.Cloudsphere = cloudsphere;

		return model;
	}

	public static void Pool(SgtCloudsphereModel model)
	{
		if (model != null)
		{
			model.Cloudsphere = null;

			SgtComponentPool<SgtCloudsphereModel>.Add(model);
		}
	}

	public static void MarkForDestruction(SgtCloudsphereModel model)
	{
		if (model != null)
		{
			model.Cloudsphere = null;

			model.gameObject.SetActive(true);
		}
	}
	
	public void BeginSave()
	{
		Save(SgtCameraState.BeginSave(ref cameraStates));
	}
	
	private void Save(CameraState cameraState)
	{
		if (cameraState != null)
		{
			cameraState.LocalPosition = transform.localPosition;
		}
	}

	public void EndSave(Camera camera)
	{
		Save(SgtCameraState.EndSave(cameraStates, camera));
	}

	public void Restore(Camera camera = null)
	{
		var cameraState = SgtCameraState.Restore(cameraStates, camera, true);

		if (cameraState != null)
		{
			transform.localPosition = cameraState.LocalPosition;
		}
	}

	protected virtual void Update()
	{
		if (Cloudsphere == null)
		{
			Pool(this);
		}
	}
}