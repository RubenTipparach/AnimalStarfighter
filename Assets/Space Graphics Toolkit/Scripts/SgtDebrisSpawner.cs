using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(SgtDebrisSpawner))]
public class SgtDebrisSpawner_Editor : SgtEditor<SgtDebrisSpawner>
{
	protected override void OnInspector()
	{
		BeginError(Any(t => t.Follower == null));
			DrawDefault("Follower");
		EndError();
		BeginError(Any(t => t.ShowSpeed <= 0.0f));
			DrawDefault("ShowSpeed");
		EndError();
		BeginError(Any(t => t.ShowDistance <= 0.0f || t.ShowDistance > t.HideDistance));
			DrawDefault("ShowDistance");
		EndError();
		BeginError(Any(t => t.HideDistance < 0.0f || t.ShowDistance > t.HideDistance));
			DrawDefault("HideDistance");
		EndError();
		
		Separator();

		DrawDefault("SpawnOnAwake");
		BeginError(Any(t => t.SpawnLimit < 0));
			DrawDefault("SpawnLimit");
		EndError();
		BeginError(Any(t => t.SpawnRateMin < 0.0f || t.SpawnRateMin > t.SpawnRateMax));
			DrawDefault("SpawnRateMin");
		EndError();
		BeginError(Any(t => t.SpawnRateMax < 0.0f || t.SpawnRateMin > t.SpawnRateMax));
			DrawDefault("SpawnRateMax");
		EndError();
		BeginError(Any(t => t.SpawnScaleMin < 0.0f || t.SpawnScaleMin > t.SpawnScaleMax));
			DrawDefault("SpawnScaleMin");
		EndError();
		BeginError(Any(t => t.SpawnScaleMax < 0.0f || t.SpawnScaleMin > t.SpawnScaleMax));
			DrawDefault("SpawnScaleMax");
		EndError();
		
		Separator();

		BeginError(Any(t => t.Prefabs == null || t.Prefabs.Count == 0 || t.Prefabs.Contains(null) == true));
			DrawDefault("Prefabs");
		EndError();

		Separator();

		BeginError(Any(t => t.Locations != null && t.Locations.Contains(null) == true));
			DrawDefault("Locations");
		EndError();
	}
}
#endif

[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Debris Spawner")]
public class SgtDebrisSpawner : MonoBehaviour
{
	// All active and enabled debris in the scene
	public static List<SgtDebrisSpawner> AllDebrisSpawners = new List<SgtDebrisSpawner>();
	
	[Tooltip("If this transform is inside the radius then debris will begin spawning")]
	public Transform Follower;

	[Tooltip("How quickly the debris shows after it spawns")]
	public float ShowSpeed = 10.0f;

	[Tooltip("The distance from the follower that debris begins spawning")]
	public float ShowDistance = 0.9f;
	
	[Tooltip("The distance from the follower that debris gets hidden")]
	public float HideDistance = 1.0f;
	
	[Tooltip("The maximum amount of debris that can be spawned")]
	public int SpawnLimit = 50;
	
	[Tooltip("The minimum amount of seconds between debris spawns")]
	public float SpawnRateMin = 0.5f;

	[Tooltip("The maximum amount of seconds between debris spawns")]
	public float SpawnRateMax = 1.0f;

	[Tooltip("The minimum scale multiplier applied to spawned debris")]
	public float SpawnScaleMin = 1.0f;

	[Tooltip("The maximum scale multiplier applied to spawned debris")]
	public float SpawnScaleMax = 1.0f;
	
	[Tooltip("Should all the debris be automatically spawned at the start?")]
	public bool SpawnOnAwake;

	[Tooltip("These prefabs are randomly picked from when spawning new debris")]
	public List<SgtDebris> Prefabs;

	[Tooltip("These shapes define where the debris can spawn, if this is empty then they will spawn everywhere")]
	public List<SgtShape> Locations;

	// The currently spawned debris
	public List<SgtDebris> Debris;
	
	// Seconds until a new debris can be spawned
	private float spawnCooldown;

	private Vector3 followerPosition;

	private Vector3 followerVelocity;

	private float minScale = 0.001f;

	// Used during find
	private SgtDebris targetPrefab;

	[ContextMenu("Clear Debris")]
	public void ClearDebris()
	{
		if (Debris != null)
		{
			for (var i = Debris.Count - 1; i >= 0; i--)
			{
				var debris = Debris[i];

				if (debris != null)
				{
					Despawn(debris, i);
				}
				else
				{
					Debris.RemoveAt(i);
				}
			}
		}
	}

	[ContextMenu("Spawn Debris Inside")]
	public void SpawnDebrisInside()
	{
		SpawnDebris(true);
	}

	// Spawns 1 debris regardless of the spawn limit, if inside is false then the debris will be spawned along the HideDistance
	public void SpawnDebris(bool inside)
	{
		if (Prefabs != null && Prefabs.Count > 0 && Follower != null)
		{
			var index  = Random.Range(0, Prefabs.Count - 1);
			var prefab = Prefabs[index];

			if (prefab != null)
			{
				var debris   = Spawn(prefab);
				var vector   = Random.insideUnitSphere * HideDistance + followerVelocity;
				var distance = HideDistance;
				
				if (inside == true)
				{
					distance = Random.Range(0.0f, HideDistance);
				}
				else
				{
					distance = Random.Range(ShowDistance, HideDistance);
				}

				if (vector.sqrMagnitude <= 0.0f)
				{
					vector = Random.onUnitSphere;
				}
				
				debris.Show    = 0.0f;
				debris.Spawner = this;
				debris.Prefab  = prefab;
				debris.Scale   = prefab.transform.localScale * Random.Range(SpawnScaleMin, SpawnScaleMax);

				debris.transform.SetParent(transform, false);

				debris.transform.position   = Follower.transform.position + vector.normalized * distance;
				debris.transform.rotation   = Random.rotationUniform;
				debris.transform.localScale = prefab.transform.localScale * minScale;

				if (debris.OnSpawn != null) debris.OnSpawn();

				if (Debris == null)
				{
					Debris = new List<SgtDebris>();
				}

				Debris.Add(debris);
			}
		}
	}

	[ContextMenu("Spawn All Debris Inside")]
	public void SpawnAllDebrisInside()
	{
		if (SpawnLimit > 0)
		{
			var count = Debris != null ? Debris.Count : 0;

			for (var i = count; i < SpawnLimit; i++)
			{
				SpawnDebrisInside();
			}
		}
	}

	public float GetDensity(Vector3 worldPosition)
	{
		// Spawn everywhere by default
		if (Locations == null || Locations.Count == 0)
		{
			return 1.0f;
		}

		var bestDensity = 0.0f;

		for (var i = Locations.Count - 1; i >= 0; i--)
		{
			var location = Locations[i];

			if (location != null)
			{
				var density = location.GetDensity(worldPosition);

				if (density > bestDensity)
				{
					bestDensity = density;
				}
			}
		}

		return bestDensity;
	}

	public static SgtDebrisSpawner CreateDebrisSpawner(int layer = 0, Transform parent = null)
	{
		return CreateDebrisSpawner(layer, parent, Vector3.zero, Quaternion.identity, Vector3.one);
	}

	public static SgtDebrisSpawner CreateDebrisSpawner(int layer, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
	{
		var gameObject    = SgtHelper.CreateGameObject("Debris Spawner", layer, parent, localPosition, localRotation, localScale);
		var debrisSpawner = gameObject.AddComponent<SgtDebrisSpawner>();

		return debrisSpawner;
	}

#if UNITY_EDITOR
	[UnityEditor.MenuItem(SgtHelper.GameObjectMenuPrefix + "Debris Spawner", false, 10)]
	public static void CreateDebrisSpawnerMenuItem()
	{
		var parent        = SgtHelper.GetSelectedParent();
		var debrisSpawner = CreateDebrisSpawner(parent != null ? parent.gameObject.layer : 0, parent);

		SgtHelper.SelectAndPing(debrisSpawner);
	}
#endif

	protected virtual void Awake()
	{
		ResetFollower();

		if (SpawnOnAwake == true)
		{
			SpawnAllDebrisInside();
		}
	}

	protected virtual void OnEnable()
	{
		AllDebrisSpawners.Add(this);

		ResetFollower();
	}
	
	protected virtual void FixedUpdate()
	{
		var newFollowerPosition = Follower != null ? Follower.position : Vector3.zero;
		
		followerVelocity = (newFollowerPosition - followerPosition) * SgtHelper.Reciprocal(Time.fixedDeltaTime);
		followerPosition = newFollowerPosition;
	}

	protected virtual void Update()
	{
		if (Follower == null)
		{
			ClearDebris(); return;
		}

		var followerPosition = Follower.position;
		var followerDensity  = GetDensity(followerPosition);
		
		if (followerDensity > 0.0f)
		{
			var debrisCount = Debris != null ? Debris.Count : 0;

			if (debrisCount < SpawnLimit)
			{
				spawnCooldown -= Time.deltaTime;

				while (spawnCooldown <= 0.0f)
				{
					spawnCooldown += Random.Range(SpawnRateMin, SpawnRateMax);
					
					SpawnDebris(false);

					debrisCount += 1;

					if (debrisCount >= SpawnLimit)
					{
						break;
					}
				}
			}
		}
		
		if (Debris != null)
		{
			var distanceRange = HideDistance - ShowDistance;
			
			for (var i = Debris.Count - 1; i >= 0; i--)
			{
				var debris = Debris[i];

				if (debris != null)
				{
					var targetScale = default(float);
					var distance    = Vector3.Distance(followerPosition, debris.transform.position);
					
					// Fade its size in
					debris.Show = SgtHelper.Dampen(debris.Show, 1.0f, ShowSpeed, Time.deltaTime, 0.1f);

					if (distance < ShowDistance)
					{
						targetScale = 1.0f;
					}
					else if (distance > HideDistance)
					{
						targetScale = 0.0f;
					}
					else
					{
						targetScale = 1.0f - SgtHelper.Divide(distance - ShowDistance, distanceRange);
					}
					
					debris.transform.localScale = debris.Scale * debris.Show * Mathf.Max(minScale, targetScale);

					if (targetScale <= 0.0f)
					{
						Despawn(debris, i);
					}
				}
				else
				{
					Debris.RemoveAt(i);
				}
			}
		}
	}

#if UNITY_EDITOR
	protected virtual void OnDrawGizmosSelected()
	{
		Gizmos.matrix = SgtHelper.Translation(Follower != null ? Follower.position : transform.position);

		Gizmos.DrawWireSphere(Vector3.zero, ShowDistance);

		Gizmos.color = new Color(1.0f, 1.0f, 1.0f, 0.5f);

		Gizmos.DrawWireSphere(Vector3.zero, HideDistance);
	}
#endif

	private SgtDebris Spawn(SgtDebris prefab)
	{
		if (prefab.Pool == true)
		{
			targetPrefab = prefab;

			var debris = SgtComponentPool<SgtDebris>.Pop(DebrisMatch);

			if (debris != null)
			{
				debris.transform.SetParent(null, false);

				return debris;
			}
		}
		
		return Instantiate(prefab);
	}

	private void Despawn(SgtDebris debris, int index)
	{
		if (debris.OnDespawn != null) debris.OnDespawn();

		if (debris.Pool == true)
		{
			SgtComponentPool<SgtDebris>.Add(debris);
		}
		else
		{
			SgtHelper.Destroy(debris.gameObject);
		}

		Debris.RemoveAt(index);
	}
	
	protected virtual void OnDisable()
	{
		AllDebrisSpawners.Remove(this);
	}

	private void ResetFollower()
	{
		followerVelocity = Vector3.zero;
		followerPosition = Follower != null ? Follower.position : Vector3.zero;
	}

	private bool DebrisMatch(SgtDebris debris)
	{
		return debris != null && debris.Prefab == targetPrefab;
	}
}
