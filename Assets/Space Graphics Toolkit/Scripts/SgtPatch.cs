using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(SgtPatch))]
public class SgtPatch_Editor : SgtEditor<SgtPatch>
{
	protected override void OnInspector()
	{
		var updateMaterials = false;
		
		DrawDefault("Material", ref updateMaterials);
		
		Separator();

		BeginDisabled();
			DrawDefault("Terrain");
			DrawDefault("Parent");
			DrawDefault("Depth");
			DrawDefault("IsSplit");
			DrawDefault("PointBL");
			DrawDefault("PointBR");
			DrawDefault("PointTL");
			DrawDefault("PointTR");
			DrawDefault("CoordBL");
			DrawDefault("CoordBR");
			DrawDefault("CoordTL");
			DrawDefault("CoordTR");
			DrawDefault("ChildBL");
			DrawDefault("ChildBR");
			DrawDefault("ChildTL");
			DrawDefault("ChildTR");
			DrawDefault("MeshCenter");
			DrawDefault("Cooldown");
		EndDisabled();

		if (updateMaterials == true) DirtyEach(t => t.UpdateMaterials());
	}
}
#endif

[ExecuteInEditMode]
[AddComponentMenu("")]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class SgtPatch : MonoBehaviour
{
	[Tooltip("The material that gets applied to this patch, and its children")]
	public Material Material;

	[Tooltip("The terrain this patch belongs to")]
	public SgtTerrain Terrain;
	
	[Tooltip("The parent of this patch")]
	public SgtPatch Parent;
	
	[Tooltip("The subdivision level of this patch")]
	public int Depth;

	[Tooltip("Has this patch been subdivided?")]
	public bool IsSplit;

	[Tooltip("The undeformed plane position of the bottom left of this patch in local space")]
	public Vector3 PointBL;

	[Tooltip("The undeformed plane position of the bottom right of this patch in local space")]
	public Vector3 PointBR;

	[Tooltip("The undeformed plane position of the top left of this patch in local space")]
	public Vector3 PointTL;

	[Tooltip("The undeformed plane position of the top right of this patch in local space")]
	public Vector3 PointTR;

	[Tooltip("The coord of the bottom left of this patch")]
	public Vector2 CoordBL;

	[Tooltip("The coord of the bottom right of this patch")]
	public Vector2 CoordBR;

	[Tooltip("The coord of the top left of this patch")]
	public Vector2 CoordTL;

	[Tooltip("The coord of the top right of this patch")]
	public Vector2 CoordTR;
	
	[Tooltip("The bottom left subdivided child")]
	public SgtPatch ChildBL;

	[Tooltip("The bottom right subdivided child")]
	public SgtPatch ChildBR;

	[Tooltip("The top left subdivided child")]
	public SgtPatch ChildTL;

	[Tooltip("The top right subdivided child")]
	public SgtPatch ChildTR;

	[Tooltip("The center of this patch mesh in local space")]
	public Vector3 MeshCenter;

	[Tooltip("The amount of seconds until this patch can be updated")]
	public float Cooldown;

	// Called when this patch should be cleared of objects
	public System.Action OnDepopulate;

	[System.NonSerialized]
	public bool Populated;

	[System.NonSerialized]
	public int Sequence;

	[System.NonSerialized]
	public int Tick;

	[System.NonSerialized]
	public Mesh Mesh;

	[System.NonSerialized]
	private MeshFilter meshFilter;

	[System.NonSerialized]
	private MeshRenderer meshRenderer;

	[System.NonSerialized]
	private MeshCollider meshCollider;
	
	private static Material[] sharedMaterials1 = new Material[1];

	private static Material[] sharedMaterials2 = new Material[2];
	
	public Vector3 RandomPoint
	{
		get
		{
			var x = Random.value;
			var y = Random.value;
			var b = Vector3.LerpUnclamped(PointBL, PointBR, x);
			var t = Vector3.LerpUnclamped(PointTL, PointTR, x);

			return Vector3.LerpUnclamped(b, t, y);
		}
	}

	public void PoolMeshNow()
	{
		Mesh = SgtObjectPool<Mesh>.Add(Mesh, m => m.Clear());
	}

	[ContextMenu("Update Materials")]
	public void UpdateMaterials()
	{
		var materials  = default(Material[]);
		var atmosphere = Terrain.Atmosphere;

		if (SgtHelper.Enabled(atmosphere) == true && atmosphere.InnerMaterial != null)
		{
			materials = sharedMaterials2;

			materials[1] = atmosphere.InnerMaterial;
		}
		else
		{
			materials = sharedMaterials1;
		}

		materials[0] = FindMaterial();

		UpdateMaterials(materials);
	}

	private Material FindMaterial()
	{
		if (Material != null)
		{
			return Material;
		}

		if (Parent != null)
		{
			return Parent.FindMaterial();
		}

		return Terrain.Material;
	}

	public void UpdateMaterials(Material[] materials)
	{
		if (Material != null)
		{
			materials[0] = Material;
		}

		UpdateMaterial(materials);

		if (IsSplit == true)
		{
			ChildBL.UpdateMaterials(materials);
			ChildBR.UpdateMaterials(materials);
			ChildTL.UpdateMaterials(materials);
			ChildTR.UpdateMaterials(materials);
		}
	}
	
	public void UpdateMaterial(Material[] materials)
	{
		if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
		
		var sharedMaterials = meshRenderer.sharedMaterials;

		if (sharedMaterials.Length != materials.Length)
		{
			meshRenderer.sharedMaterials = materials;
		}
		else
		{
			for (var i = materials.Length - 1; i >= 0; i--)
			{
				if (sharedMaterials[i] != materials[i])
				{
					meshRenderer.sharedMaterials = materials;

					return;
				}
			}
		}
	}

	[ContextMenu("Update Meshes")]
	public void UpdateMeshes()
	{
		UpdateMesh();

		if (IsSplit == true)
		{
			ChildBL.UpdateMeshes();
			ChildBR.UpdateMeshes();
			ChildTL.UpdateMeshes();
			ChildTR.UpdateMeshes();
		}
	}

	public void Populate()
	{
		if (Populated == false)
		{
			Populated = true;

			if (Terrain.OnPopulatePatch != null) Terrain.OnPopulatePatch(this);
		}
	}

	public void Depopulate()
	{
		if (Populated == true)
		{
			Populated = false;

			if (OnDepopulate != null) OnDepopulate();

			if (Terrain.OnDepopulatePatch != null) Terrain.OnDepopulatePatch(this);
		}
	}

	public void UpdateMesh()
	{
		// Create mesh and assign to filter?
		if (Mesh == null)
		{
			Mesh = SgtObjectPool<Mesh>.Pop() ?? new Mesh();
			
			Mesh.name = "Patch";
#if UNITY_EDITOR
			Mesh.hideFlags = HideFlags.DontSave;
#endif
			if (meshFilter == null) meshFilter = GetComponent<MeshFilter>();

			meshFilter.sharedMesh = Mesh;
		}
		
		Terrain.GenerateMesh(this);
		
		Depopulate();
		Populate();
	}
	
	[ContextMenu("Update Colliders")]
	public void UpdateColliders()
	{
		UpdateCollider();

		if (IsSplit == true)
		{
			ChildBL.UpdateColliders();
			ChildBR.UpdateColliders();
			ChildTL.UpdateColliders();
			ChildTR.UpdateColliders();
		}
	}
	
	public void UpdateCollider()
	{
		if (meshCollider == null) meshCollider = GetComponent<MeshCollider>();
		
		var maxDepth = Terrain.MaxColliderDepth;

		if (Depth < maxDepth && (IsSplit == false || Depth == maxDepth - 1))
		{
			if (meshCollider == null) meshCollider = gameObject.AddComponent<MeshCollider>();

			meshCollider.sharedMesh = Mesh;
			meshCollider.enabled    = true;
		}
		else
		{
			if (meshCollider != null)
			{
				meshCollider.enabled = false;
			}
		}
	}
	
	public void GetPatches(List<SgtPatch> leaves, int currentSequence, float elapsed)
	{
		Sequence  = currentSequence;
		Cooldown -= elapsed;

		if (Cooldown <= 0.0f)
		{
			leaves.Add(this);
		}

		if (IsSplit == true)
		{
			ChildBL.GetPatches(leaves, currentSequence, elapsed);
			ChildBR.GetPatches(leaves, currentSequence, elapsed);
			ChildTL.GetPatches(leaves, currentSequence, elapsed);
			ChildTR.GetPatches(leaves, currentSequence, elapsed);
		}
	}

	public void UpdateSplits(List<Vector3> localObservers)
	{
		UpdateSplit(localObservers);

		if (IsSplit == true)
		{
			ChildBL.UpdateSplits(localObservers);
			ChildBR.UpdateSplits(localObservers);
			ChildTL.UpdateSplits(localObservers);
			ChildTR.UpdateSplits(localObservers);
		}
	}

	public void UpdateSplit(List<Vector3> localObservers)
	{
#if UNITY_EDITOR
		if (Application.isPlaying == false)
		{
			if (Depth + 1 > Terrain.MaxSplitsInEditMode)
			{
				Merge(); return;
			}
		}
#endif
		// Split distances reduced at runtime?
		if (Depth > Terrain.SplitDistances.Count)
		{
			Merge(); return;
		}

		if (Depth < Terrain.SplitDistances.Count)
		{
			var bestDistance  = float.PositiveInfinity;
			var splitDistance = Terrain.SplitDistances[Depth];

			// Go through all observers to find the closest
			for (var i = localObservers.Count - 1; i >= 0; i--)
			{
				var distance = Vector3.Distance(MeshCenter, localObservers[i]);

				if (distance < bestDistance)
				{
					bestDistance = distance;
				}
			}

			// Too far?
			if (bestDistance > splitDistance * 1.1f)
			{
				Merge();
			}
			// Too near?
			else if (bestDistance < splitDistance * 0.9f)
			{
				Split();
			}
		}
	}

	public static SgtPatch Create(string name, int layer, Transform parent)
	{
		return SgtComponentPool<SgtPatch>.Pop(parent, name, layer);
	}

	public static SgtPatch Pool(SgtPatch patch)
	{
		if (patch != null)
		{
			patch.Merge();
			patch.Depopulate();

			patch.Terrain  = null;
			patch.Parent   = null;
			patch.Material = null;
			patch.Sequence = -1;
			
			patch.PoolMeshNow();

			SgtComponentPool<SgtPatch>.Add(patch);
		}

		return null;
	}

	public static SgtPatch MarkForDestruction(SgtPatch patch)
	{
		if (patch != null)
		{
			patch.Terrain = null;

			patch.gameObject.SetActive(true);
		}

		return null;
	}

#if UNITY_EDITOR
	protected virtual void OnEnable()
	{
		EditorUtility.SetSelectedRenderState(meshRenderer, EditorSelectedRenderState.Hidden);
	}
#endif
	
	protected virtual void OnDestroy()
	{
		PoolMeshNow();
	}
	
	private void Split()
	{
		if (IsSplit == false)
		{
			IsSplit = true;

			if (meshRenderer != null) meshRenderer.enabled = false;
			
			UpdateCollider();

			var PointCC = (PointBL + PointTR) * 0.5f;
			var PointBC = (PointBL + PointBR) * 0.5f;
			var PointTC = (PointTL + PointTR) * 0.5f;
			var PointCL = (PointTL + PointBL) * 0.5f;
			var PointCR = (PointTR + PointBR) * 0.5f;

			var CoordCC = (CoordBL + CoordTR) * 0.5f;
			var CoordBC = (CoordBL + CoordBR) * 0.5f;
			var CoordTC = (CoordTL + CoordTR) * 0.5f;
			var CoordCL = (CoordTL + CoordBL) * 0.5f;
			var CoordCR = (CoordTR + CoordBR) * 0.5f;
			
			ChildBL = Terrain.CreatePatch("Bottom Left" , this, PointBL, PointBC, PointCL, PointCC, CoordBL, CoordBC, CoordCL, CoordCC, Depth + 1);
			ChildBR = Terrain.CreatePatch("Bottom Right", this, PointBC, PointBR, PointCC, PointCR, CoordBC, CoordBR, CoordCC, CoordCR, Depth + 1);
			ChildTL = Terrain.CreatePatch("Top Left"    , this, PointCL, PointCC, PointTL, PointTC, CoordCL, CoordCC, CoordTL, CoordTC, Depth + 1);
			ChildTR = Terrain.CreatePatch("Top Right"   , this, PointCC, PointCR, PointTC, PointTR, CoordCC, CoordCR, CoordTC, CoordTR, Depth + 1);
		}
	}

	private void Merge()
	{
		if (IsSplit == true)
		{
			IsSplit = false;

			if (meshRenderer != null) meshRenderer.enabled = true;

			UpdateCollider();

			ChildBL = Pool(ChildBL);
			ChildBR = Pool(ChildBR);
			ChildTL = Pool(ChildTL);
			ChildTR = Pool(ChildTR);
		}
	}
}