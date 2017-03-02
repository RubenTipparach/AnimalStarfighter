using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(SgtGravityReceiver))]
public class SgtGravityReceiver_Editor : SgtEditor<SgtGravityReceiver>
{
	protected override void OnInspector()
	{
	}
}
#endif

// This component causes the attached rigidbody to get pulled toward all gravity sources
[ExecuteInEditMode]
[RequireComponent(typeof(Rigidbody))]
[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Gravity Receiver")]
public class SgtGravityReceiver : MonoBehaviour
{
	[System.NonSerialized]
	private Rigidbody body;

	protected virtual void FixedUpdate()
	{
		if (body == null) body = GetComponent<Rigidbody>();

		for (var i = SgtGravitySource.AllGravitySources.Count - 1; i >= 0; i--)
		{
			var gravitySource = SgtGravitySource.AllGravitySources[i];

			if (gravitySource.transform != transform)
			{
				var totalMass  = body.mass * gravitySource.Mass;
				var vector     = gravitySource.transform.position - transform.position;
				var distanceSq = vector.sqrMagnitude;

				if (distanceSq > 0.0f)
				{
					var force = totalMass / distanceSq;

					body.AddForce(vector.normalized * force * Time.fixedDeltaTime, ForceMode.Acceleration);
				}
			}
		}
	}
}
