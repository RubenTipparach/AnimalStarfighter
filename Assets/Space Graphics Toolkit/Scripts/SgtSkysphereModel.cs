using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(SgtSkysphereModel))]
public class SgtSkysphereModel_Editor : SgtEditor<SgtSkysphereModel>
{
	protected override void OnInspector()
	{
		BeginDisabled();
			DrawDefault("Skysphere");
		EndDisabled();
	}
}
#endif

[ExecuteInEditMode]
[AddComponentMenu("")]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class SgtSkysphereModel : MonoBehaviour
{
	[Tooltip("The skysphere this belongs to")]
	public SgtSkysphere Skysphere;

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

	private bool    saveSet;
	private Vector3 saveLocalPosition;

	public void Save()
	{
		if (saveSet == false)
		{
			saveSet           = true;
			saveLocalPosition = transform.localPosition;
		}
	}

	public void Restore()
	{
		if (saveSet == true)
		{
			saveSet = false;

			transform.localPosition = saveLocalPosition;
		}
	}

	public static SgtSkysphereModel Create(SgtSkysphere skysphere)
	{
		var model = SgtComponentPool<SgtSkysphereModel>.Pop(skysphere.transform, "Model", skysphere.gameObject.layer);

		model.Skysphere = skysphere;

		return model;
	}

	public static void Pool(SgtSkysphereModel model)
	{
		if (model != null)
		{
			model.Skysphere = null;

			SgtComponentPool<SgtSkysphereModel>.Add(model);
		}
	}

	public static void MarkForDestruction(SgtSkysphereModel model)
	{
		if (model != null)
		{
			model.Skysphere = null;

			model.gameObject.SetActive(true);
		}
	}

	protected virtual void Update()
	{
		if (Skysphere == null)
		{
			Pool(this);
		}
	}
}