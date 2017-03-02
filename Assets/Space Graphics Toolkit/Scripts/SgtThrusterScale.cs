using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(SgtThrusterScale))]
public class SgtThrusterScale_Editor : SgtEditor<SgtThrusterScale>
{
	protected override void OnInspector()
	{
		BeginError(Any(t => t.Thruster == null));
			DrawDefault("Thruster");
		EndError();
		DrawDefault("Dampening");
		DrawDefault("BaseScale");
		DrawDefault("ThrottleScale");

		Separator();

		DrawDefault("Flicker");
		DrawDefault("FlickerOffset");
		DrawDefault("FlickerSpeed");
	}
}
#endif

// This component allows you to create simple thrusters that can apply forces to Rigidbodies based on their position. You can also use sprites to change the graphics
[ExecuteInEditMode]
[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Thruster Scale")]
public class SgtThrusterScale : MonoBehaviour
{
	[Tooltip("The thruster the scale will be based on")]
	public SgtThruster Thruster;
	
	[Tooltip("The speed at which the scale reaches its target value")]
	public float Dampening = 10.0f;

	[Tooltip("The scale value that's applied by default")]
	public Vector3 BaseScale;

	[Tooltip("The scale value that's added when the throttle is 1")]
	public Vector3 ThrottleScale = Vector3.one;

	[Tooltip("The amount the ThrottleScale flickers over time")]
	[Range(0.0f, 1.0f)]
	public float Flicker = 0.1f;

	[Tooltip("The offset of the flicker animation")]
	public float FlickerOffset;

	[Tooltip("The speed of the flicker animation")]
	public float FlickerSpeed = 5.0f;
	
	[SerializeField]
	private float throttle;

	[System.NonSerialized]
	private float[] points;
	
	protected virtual void Start()
	{
		if (Thruster == null)
		{
			Thruster = GetComponentInParent<SgtThruster>();
		}
	}

	protected virtual void Update()
	{
		if (Thruster != null)
		{
			if (Application.isPlaying == true)
			{
				FlickerOffset += FlickerSpeed * Time.deltaTime;
			}

			if (points == null)
			{
				points = new float[128];

				for (var i = points.Length - 1; i >= 0; i--)
				{
					points[i] = Random.value;
				}
			}

			var noise  = Mathf.Repeat(FlickerOffset, points.Length);
			var index  = (int)noise;
			var frac   = noise % 1.0f;
			var pointA = points[index];
			var pointB = points[(index + 1) % points.Length];
			var pointC = points[(index + 2) % points.Length];
			var pointD = points[(index + 3) % points.Length];
			var f      = 1.0f - SgtHelper.CubicInterpolate(pointA, pointB, pointC, pointD, frac) * Flicker;

			throttle = SgtHelper.Dampen(throttle, Thruster.Throttle, Dampening, Time.deltaTime);

			transform.localScale = BaseScale + ThrottleScale * throttle * f;
		}
	}
}