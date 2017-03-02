using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(SgtThruster))]
public class SgtThruster_Editor : SgtEditor<SgtThruster>
{
	protected override void OnInspector()
	{
		DrawDefault("Throttle");
		DrawDefault("Rigidbody");
		
		if (Any(t => t.Rigidbody != null))
		{
			BeginIndent();
				DrawDefault("ForceType");
				DrawDefault("ForceMode");
				DrawDefault("ForceMagnitude");
			EndIndent();
		}
	}
}
#endif

// This component allows you to create simple thrusters that can apply forces to Rigidbodies based on their position. You can also use sprites to change the graphics
[ExecuteInEditMode]
[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Thruster")]
public class SgtThruster : MonoBehaviour
{
	[Tooltip("How active is this thruster? 0 for off, 1 for max power, -1 for max reverse, etc")]
	public float Throttle;
	
	[Tooltip("The rigidbody you want to apply the thruster forces to")]
	public Rigidbody Rigidbody;

	[Tooltip("The type of force we want to apply to the Rigidbody")]
	public SgtForceType ForceType = SgtForceType.AddForceAtPosition;

	[Tooltip("The force mode used when ading force to the Rigidbody")]
	public ForceMode ForceMode = ForceMode.Acceleration;

	[Tooltip("The maximum amount of force applied to the rigidbody (when the throttle is -1 or 1)")]
	public float ForceMagnitude = 1.0f;
	
	// Create a child GameObject with a thruster attached
	public static SgtThruster CreateThruster(int layer = 0, Transform parent = null)
	{
		return CreateThruster(layer, parent, Vector3.zero, Quaternion.identity, Vector3.one);
	}

	public static SgtThruster CreateThruster(int layer, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
	{
		var gameObject = SgtHelper.CreateGameObject("Thruster", layer, parent, localPosition, localRotation, localScale);
		var thruster   = gameObject.AddComponent<SgtThruster>();

		return thruster;
	}

#if UNITY_EDITOR
	[MenuItem(SgtHelper.GameObjectMenuPrefix + "Thruster", false, 10)]
	public static void CreateThrusterMenuItem()
	{
		var parent   = SgtHelper.GetSelectedParent();
		var thruster = CreateThruster(parent != null ? parent.gameObject.layer : 0, parent);

		SgtHelper.SelectAndPing(thruster);
	}
#endif
	
	protected virtual void FixedUpdate()
	{
#if UNITY_EDITOR
		if (Application.isPlaying == false)
		{
			return;
		}
#endif
		// Apply thruster force to rigidbody
		if (Rigidbody != null)
		{
			var force = transform.forward * ForceMagnitude * Throttle * Time.fixedDeltaTime;

			switch (ForceType)
			{
				case SgtForceType.AddForce: Rigidbody.AddForce(force, ForceMode); break;
				case SgtForceType.AddForceAtPosition: Rigidbody.AddForceAtPosition(force, transform.position, ForceMode); break;
			}
		}
	}
	
#if UNITY_EDITOR
	protected virtual void OnDrawGizmosSelected()
	{
		var a = transform.position;
		var b = transform.position + transform.forward * ForceMagnitude;

		Gizmos.color = new Color(1.0f, 1.0f, 1.0f, 0.5f);
		Gizmos.DrawLine(a, b);

		Gizmos.color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
		Gizmos.DrawLine(a, a + (b - a) * Throttle);
	}
#endif
}