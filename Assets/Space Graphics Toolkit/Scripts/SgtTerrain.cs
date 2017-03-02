using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(SgtTerrain))]
public class SgtTerrain_Editor : SgtEditor<SgtTerrain>
{
	protected override void OnInspector()
	{
		base.OnInspector();

		var updateMeshes    = false;
		var updateColliders = false;
		var updateMaterials = false;
		var updateSplits    = false;
		
		DrawDefault("Material", ref updateMaterials);
		DrawDefault("Atmosphere", ref updateMaterials);
		BeginError(Any(t => t.Resolution <= 1 || (t.Resolution % 2) == 0));
			DrawDefault("Resolution", ref updateMeshes);
		EndError();
		BeginError(Any(t => t.Radius <= 0.0f));
			DrawDefault("Radius", ref updateMeshes);
		EndError();
		BeginError(Any(t => t.Height <= 0.0f));
			DrawDefault("Height", ref updateMeshes);
		EndError();
		
		Separator();
		
		DrawDefault("DefaultDisplacement", ref updateMeshes);
		DrawDefault("DefaultColor", ref updateMeshes);

		Separator();

		BeginError(Any(t => t.Budget <= 0.0f || t.Budget >= 1.0f));
			DrawDefault("Budget");
		EndError();
		BeginError(Any(t => t.DelayMin < 0.0f || t.DelayMin >= 10.0f || t.DelayMin > t.DelayMax));
			DrawDefault("DelayMin");
		EndError();
		BeginError(Any(t => t.DelayMax <= 0.0f || t.DelayMax >= 10.0f || t.DelayMin > t.DelayMax));
			DrawDefault("DelayMax");
		EndError();
		BeginError(Any(t => t.MaxSplitsInEditMode < 0 || t.MaxSplitsInEditMode > t.SplitDistances.Count));
			DrawDefault("MaxSplitsInEditMode", ref updateSplits);
		EndError();
		DrawDefault("MaxColliderDepth", ref updateColliders);
		DrawDefault("SplitDistances", ref updateSplits);

		if (All(DistancesInOrder) == false)
		{
			EditorGUILayout.HelpBox("Split distances should start large and get smaller", MessageType.Warning);
		}

		if (Button("Add Split Distance") == true)
		{
			Each(AddDistance); updateSplits = true;
		}
		
		RequireObserver();
		
		if (updateMeshes    == true) DirtyEach(t => t.UpdateMeshes   ());
		if (updateMaterials == true) DirtyEach(t => t.UpdateMaterials());
		if (updateColliders == true) DirtyEach(t => t.UpdateColliders());
		if (updateSplits    == true) DirtyEach(t => t.UpdateSplits   ());
	}

	private bool DistancesInOrder(SgtTerrain terrain)
	{
		var distances    = terrain.SplitDistances;
		var bestDistance = float.PositiveInfinity;

		if (distances != null)
		{
			for (var i = 0; i < distances.Count; i++)
			{
				var distance = distances[i];

				if (distance >= bestDistance)
				{
					return false;
				}

				bestDistance = distance;
			}
		}

		return true;
	}

	private void AddDistance(SgtTerrain terrain)
	{
		var distances = terrain.SplitDistances;
		var distance  = 5.0f;

		if (distances != null)
		{
			var count = distances.Count;

			if (count > 0)
			{
				distance = distances[count - 1] * 0.5f;
			}
		}
		else
		{
			distances = terrain.SplitDistances = new List<float>();
		}
		
		distances.Add(distance);
	}
}
#endif

[ExecuteInEditMode]
[DisallowMultipleComponent]
[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Terrain")]
public partial class SgtTerrain : MonoBehaviour
{
	// All active and enabled terrains in the scene
	public static List<SgtTerrain> AllTerrains = new List<SgtTerrain>();
	
	[Tooltip("The base material applied to patches")]
	public Material Material;

	[Tooltip("The atmosphere applied to the patches")]
	public SgtAtmosphere Atmosphere;

	[Tooltip("The amount of rows & columns on each patch edge")]
	[Range(1, 16)]
	public int Resolution = 5;

	[Tooltip("The maximum time this terrain can spend in Update in seconds")]
	public float Budget = 0.01f;

	[Tooltip("The minimum delay between patch updating in seconds")]
	public float DelayMin = 0.5f;

	[Tooltip("The maximum delay between patch updating in seconds (unless the budget is exceeded)")]
	public float DelayMax = 1.0f;
	
	[Tooltip("The amount of times the main patches can be split in edit mode (0 = no splits)")]
	#pragma warning disable 414
	public int MaxSplitsInEditMode;

	[Tooltip("The maximum depth of the patches that get colliders (0 = no colliders)")]
	public int MaxColliderDepth;
	
	[Tooltip("The inner radius of the terrain (may go under this value based on displacement settings)")]
	public float Radius = 1.0f;

	[Tooltip("The maximum height of the terrain (may go above this value based on displacement settings)")]
	public float Height = 0.1f;

	[Tooltip("The default displacement that gets passed to height modifiers (0 = Radius, 1 = Radius + Height)")]
	public float DefaultDisplacement = 0.5f;

	[Tooltip("The default vertex color that gets passed to height modifiers")]
	public Color DefaultColor = Color.white;

	[Tooltip("The local distance between the patch and observer (camera) for the patch to split")]
	public List<float> SplitDistances;
	
	[SerializeField]
	private SgtPatch negativeX;

	[SerializeField]
	private SgtPatch negativeY;

	[SerializeField]
	private SgtPatch negativeZ;

	[SerializeField]
	private SgtPatch positiveX;

	[SerializeField]
	private SgtPatch positiveY;

	[SerializeField]
	private SgtPatch positiveZ;
	
	[SerializeField]
	[HideInInspector]
	private bool startCalled;
	
	[System.NonSerialized]
	private bool updateMaterialsCalled;

	[System.NonSerialized]
	private bool updateMeshesCalled;
	
	[System.NonSerialized]
	private bool updateMaterialsDirty;

	[System.NonSerialized]
	private bool updateMeshesDirty;
	
	[System.NonSerialized]
	private int patchIndex;

	[System.NonSerialized]
	private float updateAge;

	[System.NonSerialized]
	private List<SgtPatch> patches = new List<SgtPatch>();
	
	[System.NonSerialized]
	private int patchSequence;
	
	[System.NonSerialized]
	private static List<Vector3> localObservers = new List<Vector3>();
	
	private static int globalPatchSequence;
	
	private static readonly List<float> defaultSplitDistances = new List<float>(new float[] { 10.0f, 5.0f, 2.5f, 1.25f, 0.75f });
	
	public void UpdateMaterialsDirty()
	{
		updateMaterialsDirty = true;
	}

	[ContextMenu("Update Materials")]
	public void UpdateMaterials()
	{
		updateMaterialsCalled = true;
		updateMaterialsDirty  = false;
		
		if (Atmosphere != null)
		{
			Atmosphere.UpdateMaterials();
		}

		ValidateMainPatches();
		
		negativeX.UpdateMaterials();
		negativeY.UpdateMaterials();
		negativeZ.UpdateMaterials();
		positiveX.UpdateMaterials();
		positiveY.UpdateMaterials();
		positiveZ.UpdateMaterials();
	}
	
	public void UpdateMeshesDirty()
	{
		updateMeshesDirty = true;
	}

	[ContextMenu("Update Meshes")]
	public void UpdateMeshes()
	{
		updateMeshesCalled = true;
		updateMeshesDirty  = false;

		ValidateMainPatches();

		negativeX.UpdateMeshes();
		negativeY.UpdateMeshes();
		negativeZ.UpdateMeshes();
		positiveX.UpdateMeshes();
		positiveY.UpdateMeshes();
		positiveZ.UpdateMeshes();
	}

	[ContextMenu("Update Colliders")]
	public void UpdateColliders()
	{
		ValidateMainPatches();

		negativeX.UpdateColliders();
		negativeY.UpdateColliders();
		negativeZ.UpdateColliders();
		positiveX.UpdateColliders();
		positiveY.UpdateColliders();
		positiveZ.UpdateColliders();
	}

	[ContextMenu("Update Splits")]
	public void UpdateSplits()
	{
		UpdateLocalObservers();

		ValidateMainPatches();

		negativeX.UpdateSplits(localObservers);
		negativeY.UpdateSplits(localObservers);
		negativeZ.UpdateSplits(localObservers);
		positiveX.UpdateSplits(localObservers);
		positiveY.UpdateSplits(localObservers);
		positiveZ.UpdateSplits(localObservers);
	}

	// This will return the local height of the terrain under the localPosition point
	public float GetSurfaceHeightLocal(Vector3 localPosition)
	{
		var displacement = DefaultDisplacement;

		if (OnCalculateDisplacement != null) OnCalculateDisplacement(localPosition, ref displacement);
		
		return Radius + Height * displacement;
	}

	public float GetSurfaceHeightWorld(Vector3 worldPosition)
	{
		var localPosition   = transform.InverseTransformPoint(worldPosition);
		var surfacePosition = GetSurfacePositionLocal(localPosition);

		return Vector3.Distance(transform.position, transform.TransformPoint(surfacePosition));
	}

	// This will return the local surface position under the given local position
	public Vector3 GetSurfacePositionLocal(Vector3 localPosition)
	{
		var height = GetSurfaceHeightLocal(localPosition);

		return localPosition.normalized * height;
	}

	// This will return the world surface position under the given world position
	public Vector3 GetSurfacePositionWorld(Vector3 worldPosition, float offset = 0.0f)
	{
		var localPosition   = transform.InverseTransformPoint(worldPosition);
		var surfacePosition = GetSurfacePositionLocal(localPosition);
		
		return transform.TransformPoint(surfacePosition) + transform.TransformDirection(surfacePosition).normalized * offset;
	}

	// This will return the local surface normal under the given local position
	public Vector3 GetSurfaceNormalLocal(Vector3 localPosition, Vector3 localRight, Vector3 localForward)
	{
		var right       = GetSurfacePositionLocal(localPosition + localRight);
		var left        = GetSurfacePositionLocal(localPosition - localRight);
		var forward     = GetSurfacePositionLocal(localPosition + localForward);
		var back        = GetSurfacePositionLocal(localPosition - localForward);
		var rightLeft   = right   - left;
		var forwardBack = forward - back;

		return Vector3.Cross(forwardBack.normalized, rightLeft.normalized).normalized;
	}

	// This will return the world surface normal under the given world position, using 4 samples, whose distances are based on the right & forward vectors
	public Vector3 GetSurfaceNormalWorld(Vector3 worldPosition, Vector3 worldRight, Vector3 worldForward)
	{
		var localPosition = transform.InverseTransformPoint(worldPosition);
		var localRight    = transform.InverseTransformDirection(worldRight);
		var localForward  = transform.InverseTransformDirection(worldForward);
		var localNormal   = GetSurfaceNormalLocal(localPosition, localRight, localForward);

		return transform.TransformDirection(localNormal);
	}

	public Vector3 GetSurfaceNormalWorld(Vector3 worldPosition)
	{
		return (worldPosition - transform.position).normalized;
	}

	public SgtPatch CreatePatch(string name, SgtPatch parent, Vector3 pointBL, Vector3 pointBR, Vector3 pointTL, Vector3 pointTR, Vector3 coordBL, Vector3 coordBR, Vector3 coordTL, Vector3 coordTR, int depth)
	{
		var parentTransform = parent != null ? parent.transform : transform;
		var patch           = SgtPatch.Create(name, gameObject.layer, parentTransform);

		patch.Terrain = this;
		patch.Parent  = parent;
		patch.Depth   = depth;
		patch.PointBL = pointBL;
		patch.PointBR = pointBR;
		patch.PointTL = pointTL;
		patch.PointTR = pointTR;
		patch.CoordBL = coordBL;
		patch.CoordBR = coordBR;
		patch.CoordTL = coordTL;
		patch.CoordTR = coordTR;
		
		patch.UpdateMesh();
		patch.UpdateCollider();
		patch.UpdateMaterials();
		
		return patch;
	}

	public static SgtTerrain CreateTerrain(int layer = 0, Transform parent = null)
	{
		return CreateTerrain(layer, parent, Vector3.zero, Quaternion.identity, Vector3.one);
	}

	public static SgtTerrain CreateTerrain(int layer, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
	{
		var gameObject = SgtHelper.CreateGameObject("Terrain", layer, parent, localPosition, localRotation, localScale);
		var terrain    = gameObject.AddComponent<SgtTerrain>();

		return terrain;
	}

#if UNITY_EDITOR
	[MenuItem(SgtHelper.GameObjectMenuPrefix + "Terrain", false, 10)]
	public static void CreateTerrainMenuItem()
	{
		var parent  = SgtHelper.GetSelectedParent();
		var terrain = CreateTerrain(parent != null ? parent.gameObject.layer : 0, parent);

		SgtHelper.SelectAndPing(terrain);
	}
#endif

	protected virtual void OnEnable()
	{
		AllTerrains.Add(this);

		if (negativeX != null) negativeX.gameObject.SetActive(true);
		if (negativeY != null) negativeY.gameObject.SetActive(true);
		if (negativeZ != null) negativeZ.gameObject.SetActive(true);
		if (positiveX != null) positiveX.gameObject.SetActive(true);
		if (positiveY != null) positiveY.gameObject.SetActive(true);
		if (positiveZ != null) positiveZ.gameObject.SetActive(true);

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
			
			if (SplitDistances == null)
			{
				SplitDistances = defaultSplitDistances;
			}

			CheckUpdateCalls();
		}
	}
	
	protected virtual void Update()
	{
		if (updateMaterialsDirty == true)
		{
			UpdateMaterials();
		}

		if (updateMeshesDirty == true)
		{
			UpdateMeshes();
		}

		ValidateMainPatches();
	}

	protected virtual void LateUpdate()
	{
		if (Application.isPlaying == true)
		{
			updateAge += Time.deltaTime;
		}

		UpdatePatches();
	}

	protected virtual void OnDisable()
	{
		AllTerrains.Remove(this);

		if (negativeX != null) negativeX.gameObject.SetActive(false);
		if (negativeY != null) negativeY.gameObject.SetActive(false);
		if (negativeZ != null) negativeZ.gameObject.SetActive(false);
		if (positiveX != null) positiveX.gameObject.SetActive(false);
		if (positiveY != null) positiveY.gameObject.SetActive(false);
		if (positiveZ != null) positiveZ.gameObject.SetActive(false);
	}
	
	protected virtual void OnDestroy()
	{
		SgtPatch.MarkForDestruction(negativeX);
		SgtPatch.MarkForDestruction(negativeY);
		SgtPatch.MarkForDestruction(negativeZ);
		SgtPatch.MarkForDestruction(positiveX);
		SgtPatch.MarkForDestruction(positiveY);
		SgtPatch.MarkForDestruction(positiveZ);
	}

	private System.Diagnostics.Stopwatch budgetWatch = new System.Diagnostics.Stopwatch();

	private void UpdatePatches()
	{
#if UNITY_EDITOR
		if (Application.isPlaying == false)
		{
			return;
		}
#endif
		if (patchIndex < patches.Count)
		{
			UpdateLocalObservers();

			budgetWatch.Reset();
			budgetWatch.Start();
			
			while (budgetWatch.Elapsed.TotalSeconds < Budget && SgtComponentPool<SgtPatch>.Count < 4)
			{
				SgtComponentPool<SgtPatch>.Cache();
			}

			while (budgetWatch.Elapsed.TotalSeconds < Budget && patchIndex < patches.Count)
			{
				var patch = patches[patchIndex++];

				// Make sure this patch is still in sequence (if it gets pooled and spawned it won't be)
				if (patch != null && patch.Sequence == patchSequence)
				{
					patch.UpdateSplit(localObservers);
					
					patch.Cooldown = Random.Range(DelayMin, DelayMax);
				}
			}

			budgetWatch.Stop();
		}
		else
		{
			// Grab new leaves
			patches.Clear();

			// Been at least half the min delay since the last update?
			if (updateAge > DelayMin * 0.5f)
			{
				var elapsed = updateAge;

				updateAge     = 0.0f;
				patchIndex    = 0;
				patchSequence = globalPatchSequence = globalPatchSequence % int.MaxValue + 1; // Increment this, but prevent it from going below 1

				ValidateMainPatches();

				negativeX.GetPatches(patches, patchSequence, elapsed);
				negativeY.GetPatches(patches, patchSequence, elapsed);
				negativeZ.GetPatches(patches, patchSequence, elapsed);
				positiveX.GetPatches(patches, patchSequence, elapsed);
				positiveY.GetPatches(patches, patchSequence, elapsed);
				positiveZ.GetPatches(patches, patchSequence, elapsed);
			}
		}
	}

	private SgtPatch CreatePatch(string name, Quaternion rotation)
	{
		var pointBL = rotation * new Vector3(-1.0f, -1.0f, 1.0f);
		var pointBR = rotation * new Vector3( 1.0f, -1.0f, 1.0f);
		var pointTL = rotation * new Vector3(-1.0f,  1.0f, 1.0f);
		var pointTR = rotation * new Vector3( 1.0f,  1.0f, 1.0f);
		var coordBL = new Vector2(1.0f, 0.0f);
		var coordBR = new Vector2(0.0f, 0.0f);
		var coordTL = new Vector2(1.0f, 1.0f);
		var coordTR = new Vector2(0.0f, 1.0f);

		return CreatePatch(name, null, pointBL, pointBR, pointTL, pointTR, coordBL, coordBR, coordTL, coordTR, 0);
	}

	private void ValidateMainPatches()
	{
		if (negativeX == null) negativeX = CreatePatch("Negative X", Quaternion.Euler(  0.0f,  90.0f, 0.0f));
		if (negativeY == null) negativeY = CreatePatch("Negative Y", Quaternion.Euler( 90.0f,   0.0f, 0.0f));
		if (negativeZ == null) negativeZ = CreatePatch("Negative Z", Quaternion.Euler(  0.0f, 180.0f, 0.0f));
		if (positiveX == null) positiveX = CreatePatch("Positive X", Quaternion.Euler(  0.0f, 270.0f, 0.0f));
		if (positiveY == null) positiveY = CreatePatch("Positive Y", Quaternion.Euler(270.0f,   0.0f, 0.0f));
		if (positiveZ == null) positiveZ = CreatePatch("Positive Z", Quaternion.Euler(  0.0f,   0.0f, 0.0f));
	}

	private void UpdateLocalObservers()
	{
		localObservers.Clear();

		for (var i = SgtObserver.AllObservers.Count - 1; i >= 0; i--)
		{
			var observer = SgtObserver.AllObservers[i];
			var point    = transform.InverseTransformPoint(observer.transform.position);

			localObservers.Add(point);
		}
	}

	private void CheckUpdateCalls()
	{
		if (updateMeshesCalled == false)
		{
			UpdateMeshes();
		}

		//if (updateMaterialsCalled == false)
		//{
			UpdateMaterials();
		//}
		UpdateColliders();
	}
}