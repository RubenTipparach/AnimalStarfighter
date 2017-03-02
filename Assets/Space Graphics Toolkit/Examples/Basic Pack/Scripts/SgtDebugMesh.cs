using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(SgtDebugMesh))]
public class SgtDebugMesh_Editor : SgtEditor<SgtDebugMesh>
{
	protected override void OnInspector()
	{
		BeginError(Any(t => t.DrawScale <= 0.0f));
			DrawDefault("DrawScale");
		EndError();
		DrawDefault("NormalsColor");
		DrawDefault("TangentsColor");
	}
}
#endif

// This component draws debug mesh info in the scene window
[DisallowMultipleComponent]
[RequireComponent(typeof(MeshFilter))]
[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Debug Mesh")]
public class SgtDebugMesh : MonoBehaviour
{
	[Tooltip("The scale of the normal andtangent lines")]
	public float DrawScale = 1.0f;
	
	[Tooltip("The color of the normals")]
	public Color NormalsColor = Color.red;
	
	[Tooltip("The color of the tangents")]
	public Color TangentsColor = Color.green;
	
	[System.NonSerialized]
	private MeshFilter meshFilter;
	
#if UNITY_EDITOR
	protected virtual void OnDrawGizmos()
	{
		if (meshFilter == null) meshFilter = GetComponent<MeshFilter>();
		
		var mesh = meshFilter.sharedMesh;
		
		if (mesh != null)
		{
			var positions = mesh.vertices;
			
			if (positions.Length > 0)
			{
				var normals   = mesh.normals;
				var tangents  = mesh.tangents;
				
				Gizmos.matrix = transform.localToWorldMatrix;
				
				if (normals.Length > 0 && normals.Length == positions.Length)
				{
					Gizmos.color = NormalsColor;
					
					for (var i = 0; i < positions.Length; i++)
					{
						var position = positions[i];
						
						Gizmos.DrawLine(position, position + normals[i] * DrawScale);
					}
				}
				
				if (tangents.Length > 0 && tangents.Length == positions.Length)
				{
					Gizmos.color = TangentsColor;
					
					for (var i = 0; i < positions.Length; i++)
					{
						var position = positions[i];
						
						Gizmos.DrawLine(position, position + (Vector3)tangents[i] * DrawScale);
					}
				}
			}
		}
	}
#endif
}