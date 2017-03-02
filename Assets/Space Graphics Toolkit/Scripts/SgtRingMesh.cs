using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(SgtRingMesh))]
public class SgtRingMesh_Editor : SgtEditor<SgtRingMesh>
{
	protected override void OnInspector()
	{
		var updateMesh  = false;
		var updateApply = false;
		
		BeginError(Any(t => t.Ring == null));
			DrawDefault("Ring", ref updateApply, ref updateMesh);
		EndError();
		BeginError(Any(t => t.Segments < 1));
			DrawDefault("Segments", ref updateMesh);
		EndError();
		BeginError(Any(t => t.SegmentDetail < 1));
			DrawDefault("SegmentDetail", ref updateMesh);
		EndError();
		BeginError(Any(t => t.RadiusMin == t.RadiusMax));
			DrawDefault("RadiusMin", ref updateMesh);
			DrawDefault("RadiusMax", ref updateMesh);
		EndError();
		BeginError(Any(t => t.RadiusDetail < 1));
			DrawDefault("RadiusDetail", ref updateMesh);
		EndError();
		DrawDefault("BoundsShift", ref updateMesh);
		
		if (updateMesh  == true) DirtyEach(t => t.UpdateMesh ());
		if (updateApply == true) DirtyEach(t => t.UpdateApply());
	}
}
#endif

[ExecuteInEditMode]
[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Ring Mesh")]
public class SgtRingMesh : MonoBehaviour
{
	[Tooltip("The ring this mesh will be applied to")]
	public SgtRing Ring;

	[Tooltip("The amount of segments this mesh is designed to be split into")]
	public int Segments = 8;

	[Tooltip("The amount of times the ring mesh is sliced around each segment")]
	public int SegmentDetail = 50;
	
	[Tooltip("The radius of the inner edge of the ring in local coordinates")]
	public float RadiusMin = 1.0f;

	[Tooltip("The radius of the outer edge of the ring in local coordinates")]
	public float RadiusMax = 2.0f;
	
	[Tooltip("The amount of times the ring mesh is sliced between the radius min and max")]
	public int RadiusDetail = 2;

	[Tooltip("The amount the mesh bounds should get pushed out by in local coordinates. This should be used with 8+ Segments")]
	public float BoundsShift;

	[System.NonSerialized]
	private Mesh generatedMesh;
	
	[SerializeField]
	[HideInInspector]
	private bool startCalled;
	
	public Mesh GeneratedMesh
	{
		get
		{
			return generatedMesh;
		}
	}
	
#if UNITY_EDITOR
	[ContextMenu("Export Mesh")]
	public void ExportOuterTexture()
	{
		if (generatedMesh != null)
		{
			var root = Application.dataPath;
			var path = EditorUtility.SaveFilePanel("Export Mesh", root, "RingMesh", "png");

			ExportMesh(generatedMesh, root, path);
		}
	}

	private void ExportMesh(Mesh mesh, string root, string path)
	{
		if (string.IsNullOrEmpty(path) == false)
		{
			if (path.StartsWith(root) == true)
			{
				
				Debug.Log("Exported Mesh to " + path);
			}
		}
	}
#endif

	[ContextMenu("Update Mesh")]
	public void UpdateMesh()
	{
		if (Segments > 0 && SegmentDetail > 0 && RadiusDetail > 0)
		{
			if (generatedMesh == null)
			{
				generatedMesh = SgtHelper.CreateTempMesh("Ring Mesh (Generated)");

				UpdateApply();
			}
			
			var slices     = SegmentDetail + 1;
			var rings      = RadiusDetail + 1;
			var total      = slices * rings * 2;
			var positions  = new Vector3[total];
			var coords1    = new Vector2[total];
			var coords2    = new Vector2[total];
			var colors     = new Color[total];
			var indices    = new int[SegmentDetail * RadiusDetail * 6];
			var yawStep    = (Mathf.PI * 2.0f) / Segments / SegmentDetail;
			var sliceStep  = 1.0f / SegmentDetail;
			var ringStep   = 1.0f / RadiusDetail;

			for (var slice = 0; slice < slices; slice++)
			{
				var a = yawStep * slice;
				var x = Mathf.Sin(a);
				var z = Mathf.Cos(a);
				
				for (var ring = 0; ring < rings; ring++)
				{
					var v       = rings * slice + ring;
					var slice01 = sliceStep * slice;
					var ring01  = ringStep * ring;
					var radius  = Mathf.Lerp(RadiusMin, RadiusMax, ring01);

					positions[v] = new Vector3(x * radius, 0.0f, z * radius);
					colors[v] = new Color(1.0f, 1.0f, 1.0f, 0.0f);
					coords1[v] = new Vector2(ring01, slice01);
					coords2[v] = new Vector2(radius, slice01 * radius);
				}
			}
			
			for (var slice = 0; slice < SegmentDetail; slice++)
			{
				for (var ring = 0; ring < RadiusDetail; ring++)
				{
					var i  = (slice * RadiusDetail + ring) * 6;
					var v0 = slice * rings + ring;
					var v1 = v0 + rings;

					indices[i + 0] = v0 + 0;
					indices[i + 1] = v0 + 1;
					indices[i + 2] = v1 + 0;
					indices[i + 3] = v1 + 1;
					indices[i + 4] = v1 + 0;
					indices[i + 5] = v0 + 1;
				}
			}
			
			generatedMesh.Clear();
			generatedMesh.vertices  = positions;
			generatedMesh.colors    = colors;
			generatedMesh.uv        = coords1;
			generatedMesh.uv2       = coords2;
			generatedMesh.triangles = indices;
			generatedMesh.RecalculateNormals();
			generatedMesh.RecalculateBounds();

			var bounds = generatedMesh.bounds;

			generatedMesh.bounds = SgtHelper.NewBoundsCenter(bounds, bounds.center + bounds.center.normalized * BoundsShift);
		}
	}
	
	[ContextMenu("Update Apply")]
	public void UpdateApply()
	{
		if (Ring != null)
		{
			if (generatedMesh != null)
			{
				if (Ring.Mesh != generatedMesh)
				{
					Ring.Mesh = generatedMesh;

					Ring.UpdateMesh();
				}
			}
		}
	}

	protected virtual void OnEnable()
	{
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

			if (Ring == null)
			{
				Ring = GetComponent<SgtRing>();
			}

			CheckUpdateCalls();
		}
	}

	protected virtual void OnDestroy()
	{
		if (generatedMesh != null)
		{
			generatedMesh.Clear();

			SgtObjectPool<Mesh>.Add(generatedMesh);
		}
	}

#if UNITY_EDITOR
	protected virtual void OnDrawGizmosSelected()
	{
		Gizmos.matrix = transform.localToWorldMatrix;

		SgtHelper.DrawCircle(Vector3.zero, Vector3.up, RadiusMin);
		SgtHelper.DrawCircle(Vector3.zero, Vector3.up, RadiusMax);
	}
#endif

	private void CheckUpdateCalls()
	{
		if (generatedMesh == null)
		{
			UpdateMesh();
		}

		UpdateApply();
	}
}