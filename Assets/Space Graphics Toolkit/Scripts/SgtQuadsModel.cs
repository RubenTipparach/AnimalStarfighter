using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(SgtQuadsModel))]
public class SgtQuadsModel_Editor : SgtEditor<SgtQuadsModel>
{
	protected override void OnInspector()
	{
		BeginDisabled();
			DrawDefault("Quads");
		EndDisabled();
	}
}
#endif

[ExecuteInEditMode]
[AddComponentMenu("")]
[DisallowMultipleComponent]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class SgtQuadsModel : MonoBehaviour
{
	public class CameraState : SgtCameraState
	{
		public Vector3 LocalPosition;
	}

	[Tooltip("The quads instance this belongs to")]
	public SgtQuads Quads;
	
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
	
	public Mesh Mesh
	{
		get
		{
			return mesh;
		}
	}

	public void PoolMeshNow()
	{
		if (mesh != null)
		{
			if (meshFilter == null) meshFilter = gameObject.GetComponent<MeshFilter>();

			mesh = meshFilter.sharedMesh = SgtObjectPool<Mesh>.Add(mesh, m => m.Clear());
		}
	}

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

	public static SgtQuadsModel Create(SgtQuads quads)
	{
		var model = SgtComponentPool<SgtQuadsModel>.Pop(quads.transform, "Quads Model", quads.gameObject.layer);

		model.Quads = quads;
		
		return model;
	}

	public static void Pool(SgtQuadsModel model)
	{
		if (model != null)
		{
			model.Quads = null;

			model.PoolMeshNow();

			SgtComponentPool<SgtQuadsModel>.Add(model);
		}
	}

	public static void MarkForDestruction(SgtQuadsModel model)
	{
		if (model != null)
		{
			model.Quads = null;

			model.PoolMeshNow();

			model.gameObject.SetActive(true);
		}
	}
	
	public void BeginSave()
	{
		Restore(null, false);

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

	public void Restore(Camera camera = null, bool remove = true)
	{
		var cameraState = SgtCameraState.Restore(cameraStates, camera, remove);

		if (cameraState != null)
		{
			transform.localPosition = cameraState.LocalPosition;
		}
	}
	
	protected virtual void OnDestroy()
	{
		PoolMeshNow();
	}

	protected virtual void Update()
	{
		if (Quads == null)
		{
			Pool(this);
		}
	}
}