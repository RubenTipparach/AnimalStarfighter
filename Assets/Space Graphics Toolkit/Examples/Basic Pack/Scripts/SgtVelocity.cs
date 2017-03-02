using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(SgtVelocity))]
public class SgtVelocity_Editor : SgtEditor<SgtVelocity>
{
	protected override void OnInspector()
	{
		DrawDefault("Velocity");
	}
}
#endif

// This component allows you to view a rigidbody's current velocity, as well as set its initial velocity in the editor
[RequireComponent(typeof(Rigidbody))]
[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Velocity")]
public class SgtVelocity : MonoBehaviour
{
	[Tooltip("The initial and current rigidbody velocity")]
	public Vector3 Velocity;

	[HideInInspector]
	[SerializeField]
	private Vector3 expectedVelocity;

	private Rigidbody body;

	protected virtual void OnEnable()
	{
		UpdateVelocity(true);
	}

	protected virtual void Update()
	{
		UpdateVelocity();
	}

	protected virtual void FixedUpdate()
	{
		UpdateVelocity();
	}

	private void UpdateVelocity(bool forceSet = false)
	{
		if (body == null) body = GetComponent<Rigidbody>();

		if (Velocity != expectedVelocity || forceSet == true)
		{
			body.velocity = expectedVelocity = Velocity;
		}
		else
		{
			Velocity = expectedVelocity = body.velocity;
		}
	}
}
