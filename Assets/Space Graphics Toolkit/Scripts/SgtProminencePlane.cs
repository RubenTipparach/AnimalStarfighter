using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(SgtProminencePlane))]
public class SgtProminencePlane_Editor : SgtEditor<SgtProminencePlane>
{
	protected override void OnInspector()
	{
		BeginDisabled();
			DrawDefault("Prominence");
		EndDisabled();
	}
}
#endif

[ExecuteInEditMode]
[AddComponentMenu("")]
[DisallowMultipleComponent]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class SgtProminencePlane : MonoBehaviour
{
	public class CameraState : SgtCameraState
	{
		public Vector3 LocalPosition;
	}

	[Tooltip("The prominence this belongs to")]
	public SgtProminence Prominence;

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

	public void SetRotation(Quaternion rotation)
	{
		SgtHelper.SetLocalRotation(transform, rotation);
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

	public static SgtProminencePlane Create(SgtProminence prominence)
	{
		var plane = SgtComponentPool<SgtProminencePlane>.Pop(prominence.transform, "Plane", prominence.gameObject.layer);

		plane.Prominence = prominence;

		return plane;
	}

	public static void Pool(SgtProminencePlane plane)
	{
		if (plane != null)
		{
			plane.Prominence = null;

			SgtComponentPool<SgtProminencePlane>.Add(plane);
		}
	}

	public static void MarkForDestruction(SgtProminencePlane plane)
	{
		if (plane != null)
		{
			plane.Prominence = null;

			plane.gameObject.SetActive(true);
		}
	}

	protected virtual void Update()
	{
		if (Prominence == null)
		{
			Pool(this);
		}
	}
}