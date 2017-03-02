using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(SgtRingModel))]
public class SgtRingModel_Editor : SgtEditor<SgtRingModel>
{
	protected override void OnInspector()
	{
		BeginDisabled();
			DrawDefault("Ring");
		EndDisabled();
	}
}
#endif

[ExecuteInEditMode]
[AddComponentMenu("")]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class SgtRingModel : MonoBehaviour
{
	[Tooltip("The ring this belongs to")]
	public SgtRing Ring;
	
	[System.NonSerialized]
	private MeshFilter meshFilter;
	
	[System.NonSerialized]
	private MeshRenderer meshRenderer;

	public void SetMesh(Mesh mesh)
	{
		if (meshFilter == null) meshFilter = gameObject.GetComponent<MeshFilter>();

		if (meshFilter.sharedMesh != mesh)
		{
			meshFilter.sharedMesh = mesh;
		}
	}

	public void SetMaterial(Material material)
	{
		if (meshRenderer == null) meshRenderer = gameObject.GetComponent<MeshRenderer>();

		if (meshRenderer.sharedMaterial != material)
		{
			meshRenderer.sharedMaterial = material;
		}
	}

	public void SetRotation(Quaternion rotation)
	{
		SgtHelper.SetLocalRotation(transform, rotation);
	}

	public static SgtRingModel Create(SgtRing ring)
	{
		var segment = SgtComponentPool<SgtRingModel>.Pop(ring.transform, "Ring Model", ring.gameObject.layer);

		segment.Ring = ring;

		return segment;
	}

	public static void Pool(SgtRingModel segment)
	{
		if (segment != null)
		{
			segment.Ring = null;

			SgtComponentPool<SgtRingModel>.Add(segment);
		}
	}

	public static void MarkForDestruction(SgtRingModel segment)
	{
		if (segment != null)
		{
			segment.Ring = null;

			segment.gameObject.SetActive(true);
		}
	}

	protected virtual void Update()
	{
		if (Ring == null)
		{
			Pool(this);
		}
	}
}