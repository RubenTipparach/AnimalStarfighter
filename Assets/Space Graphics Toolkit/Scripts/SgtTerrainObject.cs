using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(SgtTerrainObject))]
public class SgtTerrainObject_Editor : SgtEditor<SgtTerrainObject>
{
	protected override void OnInspector()
	{
		DrawDefault("Pool");
		DrawDefault("ScaleMin");
		DrawDefault("ScaleMax");
		DrawDefault("AlignToNormal");
		
		Separator();

		BeginDisabled();
			DrawDefault("Patch");
			DrawDefault("Prefab");
		EndDisabled();
	}
}
#endif

[ExecuteInEditMode]
[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Terrain Object")]
public class SgtTerrainObject : MonoBehaviour
{
	// Called when this object is spawned (if pooling is enabled)
	public System.Action OnSpawn;

	// Called when this object is despawned (if pooling is enabled)
	public System.Action OnDespawn;

	[Tooltip("Can this particle be pooled?")]
	public bool Pool;

	[Tooltip("The minimum scale this prefab is multiplied by when spawned")]
	public float ScaleMin = 1.0f;

	[Tooltip("The maximum scale this prefab is multiplied by when spawned")]
	public float ScaleMax = 1.1f;

	[Tooltip("How far from the center the height samples are taken to align to the surface normal in world coordinates (0 = no alignment)")]
	public float AlignToNormal;

	[Tooltip("The patch this was spawned on")]
	public SgtPatch Patch;

	[Tooltip("The prefab this was instantiated from")]
	public SgtTerrainObject Prefab;

	public void Spawn(SgtTerrain terrain, Vector3 localPosition)
	{
		if (OnSpawn != null) OnSpawn();
		
		// Snap to surface
		localPosition = terrain.GetSurfacePositionLocal(localPosition);

		// Rotate up
		var up = Quaternion.Euler(0.0f, Random.Range(0.0f, 360.0f), 0.0f) * Vector3.up;

		// Spawn on surface
		transform.position   = terrain.transform.TransformPoint(localPosition);
		transform.rotation   = Quaternion.FromToRotation(up, terrain.transform.TransformDirection(localPosition));
		transform.localScale = Prefab.transform.localScale * Random.Range(ScaleMin, ScaleMax);
		
		if (AlignToNormal != 0.0f)
		{
			var worldRight   = transform.right   * AlignToNormal;
			var worldForward = transform.forward * AlignToNormal;
			var worldNormal  = terrain.GetSurfaceNormalWorld(transform.position, worldRight, worldForward);

			transform.rotation = Quaternion.FromToRotation(up, worldNormal);
		}

		Patch.OnDepopulate += Despawn;
	}

	public void Despawn()
	{
		Patch.OnDepopulate -= Despawn;

		if (OnDespawn != null) OnDespawn();

		SgtComponentPool<SgtTerrainObject>.Add(this);
	}
}
