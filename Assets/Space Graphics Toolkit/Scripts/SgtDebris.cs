using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(SgtDebris))]
public class SgtDebris_Editor : SgtEditor<SgtDebris>
{
	protected override void OnInspector()
	{
		DrawDefault("Pool");
		
		Separator();

		BeginDisabled();
			DrawDefault("Spawner");
			DrawDefault("Prefab");
			DrawDefault("Scale");
		EndDisabled();
	}
}
#endif

[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Debris")]
public class SgtDebris : MonoBehaviour
{
	// Called when this debris is spawned (if pooling is enabled)
	public System.Action OnSpawn;

	// Called when this debris is despawned (if pooling is enabled)
	public System.Action OnDespawn;

	[Tooltip("Can this particle be pooled?")]
	public bool Pool;
	
	[Tooltip("The debris this particle was spawned by")]
	public SgtDebrisSpawner Spawner;
	
	[Tooltip("The prefab this was instantiated from")]
	public SgtDebris Prefab;

	[Tooltip("This gets automatically copied when spawning debris")]
	public Vector3 Scale;

	// The initial scale-in
	public float Show;
}