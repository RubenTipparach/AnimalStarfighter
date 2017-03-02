using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(SgtDebrisRandomVelocity))]
public class SgtDebrisRandomVelocity_Editor : SgtEditor<SgtDebrisRandomVelocity>
{
	protected override void OnInspector()
	{
		BeginError(Any(t => t.MaxLinearSpeed < 0.0f));
			DrawDefault("MaxLinearSpeed");
		EndError();
		BeginError(Any(t => t.MaxAngularSpeed < 0.0f));
			DrawDefault("MaxAngularSpeed");
		EndError();
	}
}
#endif

// This component will randomly set the debris Rigidbody velocity when spawned
[DisallowMultipleComponent]
[RequireComponent(typeof(SgtDebris))]
[RequireComponent(typeof(Rigidbody))]
[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Debris Random Velocity")]
public class SgtDebrisRandomVelocity : MonoBehaviour
{
	[Tooltip("The maximum random linear speed this debris can spawn with")]
	public float MaxLinearSpeed = 1.0f;

	[Tooltip("The maximum random angular speed this debris can spawn with")]
	public float MaxAngularSpeed = 1.0f;

	[System.NonSerialized]
	private SgtDebris debris;

	[System.NonSerialized]
	private Rigidbody body;

	protected virtual void OnEnable()
	{
		if (debris == null) debris = GetComponent<SgtDebris>();
		if (body   == null) body   = GetComponent<Rigidbody>();

		debris.OnSpawn += Spawn;
	}

	protected virtual void OnDisable()
	{
		debris.OnSpawn -= Spawn;
	}

	private void Spawn()
	{
		body.velocity = Random.insideUnitSphere * MaxLinearSpeed;

		body.angularVelocity = new Vector3(Random.value, Random.value, Random.value) * MaxAngularSpeed;
	}
}
