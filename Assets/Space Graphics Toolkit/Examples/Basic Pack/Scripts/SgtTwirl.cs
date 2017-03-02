using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(SgtTwirl))]
public class SgtTwirl_Editor : SgtEditor<SgtTwirl>
{
	protected override void OnInspector()
	{
		DrawDefault("DegreesPerSecond");
		DrawDefault("Axis");
		BeginError(Any(t => t.Dampening < 0.0f));
			DrawDefault("Dampening");
		EndError();
	}
}
#endif

// This object allows you to rotate an object around a randomly changing axis
[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Twirl")]
public class SgtTwirl : MonoBehaviour
{
	[Tooltip("The spin rate")]
	public float DegreesPerSecond = 1.0f;
	
	[Tooltip("The current axis of rotation")]
	public Vector3 Axis = Vector3.up;
	
	[Tooltip("The speed at which the axis changes to a new axis")]
	public float Dampening = 2.0f;
	
	[SerializeField]
	private Vector3 targetAxis;
	
	protected virtual void Update()
	{
		if (targetAxis == Vector3.zero || Vector3.Distance(targetAxis, Axis) < 0.1f)
		{
			targetAxis.x = Random.Range(-1.0f, 1.0f);
			targetAxis.y = Random.Range(-1.0f, 1.0f);
			targetAxis.z = Random.Range(-1.0f, 1.0f);
			targetAxis = targetAxis.normalized;
		}
		
		Axis = SgtHelper.Dampen3(Axis, targetAxis, Dampening, Time.deltaTime, 0.1f);
		
		transform.Rotate(Axis.normalized, DegreesPerSecond * Time.deltaTime);
	}
}