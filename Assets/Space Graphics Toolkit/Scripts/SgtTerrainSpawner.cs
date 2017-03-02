using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(SgtTerrainSpawner))]
public class SgtTerrainSpawner_Editor : SgtEditor<SgtTerrainSpawner>
{
	protected override void OnInspector()
	{
		var updateTerrain = false;
		
		DrawDefault("Depth", ref updateTerrain);
		DrawDefault("SpawnCountDistribution", ref updateTerrain);

		Separator();

		DrawDefault("Prefabs", ref updateTerrain);
		
		if (updateTerrain == true) DirtyEach(t => t.DirtyTerrain());
	}
}
#endif

[ExecuteInEditMode]
[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Terrain Spawner")]
public class SgtTerrainSpawner : SgtTerrainModifier
{
	[Tooltip("The patch depth required for these objects to spawn")]
	public int Depth;

	[Tooltip("The prefabs we want to spawn on the terrain patch")]
	public List<SgtTerrainObject> Prefabs;

	[Tooltip("This decides how many prefabs get spawned based on a random 0..1 sample on the x axis")]
	public AnimationCurve SpawnCountDistribution;
	
	// Used during find
	private static SgtTerrainObject targetPrefab;

	private static Keyframe[] defaultSpawnCountDistribution = new Keyframe[] { new Keyframe(0.0f, 0.0f), new Keyframe(1.0f, 3.0f) };

	protected override void OnEnable()
	{
		base.OnEnable();
		
		terrain.OnPopulatePatch   += PopulatePatch;
		terrain.OnDepopulatePatch += DepopulatePatch;
	}

	protected override void OnDisable()
	{
		base.OnDisable();
		
		terrain.OnPopulatePatch   -= PopulatePatch;
		terrain.OnDepopulatePatch -= DepopulatePatch;
	}

	protected virtual void Start()
	{
		if (SpawnCountDistribution == null)
		{
			SpawnCountDistribution = new AnimationCurve();
			SpawnCountDistribution.keys = defaultSpawnCountDistribution;
		}
	}

	private void PopulatePatch(SgtPatch patch)
	{
		if (patch.Depth == Depth && Prefabs != null && Prefabs.Count > 0 && SpawnCountDistribution != null)
		{
			var count = Mathf.FloorToInt(SpawnCountDistribution.Evaluate(Random.value));

			for (var i = 0; i < count; i++)
			{
				var prefab = Prefabs[Random.Range(0, Prefabs.Count)];

				if (prefab != null)
				{
					var instance = Spawn(prefab);
					
					instance.Prefab = prefab;
					instance.Patch  = patch;

					instance.Spawn(terrain, patch.RandomPoint);
				}
			}
		}
	}

	private void DepopulatePatch(SgtPatch patch)
	{
	}
	
	private SgtTerrainObject Spawn(SgtTerrainObject prefab)
	{
		if (prefab.Pool == true)
		{
			targetPrefab = prefab;

			var debris = SgtComponentPool<SgtTerrainObject>.Pop(ObjectMatch);

			if (debris != null)
			{
				debris.transform.SetParent(null, false);

				return debris;
			}
		}
		
		return Instantiate(prefab);
	}

	private bool ObjectMatch(SgtTerrainObject instance)
	{
		return instance != null && instance.Prefab == targetPrefab;
	}
}
