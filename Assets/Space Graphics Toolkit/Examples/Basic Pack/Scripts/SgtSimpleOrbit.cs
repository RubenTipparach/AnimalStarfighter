using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(SgtSimpleOrbit))]
public class SgtSimpleOrbit_Editor : SgtEditor<SgtSimpleOrbit>
{
	protected override void OnInspector()
	{
		BeginError(Any(t => t.Radius == 0.0f));
			DrawDefault("Radius");
		EndError();
		DrawDefault("Oblateness");
		DrawDefault("Center");
		DrawDefault("Angle");
		DrawDefault("DegreesPerSecond");
	}
}
#endif

// This component handles basic orbiting around the parent GameObject
[ExecuteInEditMode]
[DisallowMultipleComponent]
[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Simple Orbit")]
public class SgtSimpleOrbit : MonoBehaviour
{
	[Tooltip("The radius of the orbit in local coordinates")]
	public float Radius = 1.0f;
	
	[Tooltip("How squashed the orbit is")]
	[Range(0.0f, 1.0f)]
	public float Oblateness;
	
	[Tooltip("The local position offset of the orbit")]
	public Vector3 Center;
	
	[Tooltip("The curent position along the orbit in degrees")]
	public float Angle;
	
	[Tooltip("The orbit speed")]
	public float DegreesPerSecond = 10.0f;
	
	protected virtual void Update()
	{
		if (Application.isPlaying == true)
		{
			Angle += DegreesPerSecond * Time.deltaTime;
		}

		var r1 = Radius;
		var r2 = Radius * (1.0f - Oblateness);
		var lp = Center;
		
		lp.x += Mathf.Sin(Angle * Mathf.Deg2Rad) * r1;
		lp.z += Mathf.Cos(Angle * Mathf.Deg2Rad) * r2;
		
		SgtHelper.SetLocalPosition(transform, lp);
	}
	
#if UNITY_EDITOR
	protected virtual void OnDrawGizmosSelected()
	{
		if (SgtHelper.Enabled(this) == true)
		{
			if (transform.parent != null)
			{
				Gizmos.matrix = transform.parent.localToWorldMatrix;
			}
			
			var r1 = Radius;
			var r2 = Radius * (1.0f - Oblateness);
			
			SgtHelper.DrawCircle(Center, Vector3.right * r1, Vector3.forward * r2);
			
			Gizmos.DrawLine(Vector3.zero, transform.localPosition);
		}
	}
#endif
}