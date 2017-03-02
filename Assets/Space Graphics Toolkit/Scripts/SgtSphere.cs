using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(SgtSphere))]
public class SgtSphere_Editor : SgtEditor<SgtSphere>
{
	protected override void OnInspector()
	{
		BeginError(Any(t => t.Radius <= 0.0f));
			DrawDefault("Radius");
		EndError();
	}
}
#endif

public class SgtSphere : SgtShape
{
	[Tooltip("The radius of this sphere in local coordinates")]
	public float Radius;

	public override float GetDensity(Vector3 worldPosition)
	{
		var localPosition = transform.InverseTransformPoint(worldPosition);

		return localPosition.magnitude < Radius ? 1.0f : 0.0f;
	}

#if UNITY_EDITOR
	protected virtual void OnDrawGizmosSelected()
	{
		Gizmos.matrix = transform.localToWorldMatrix;

		Gizmos.DrawWireSphere(Vector3.zero, Radius);
	}
#endif
}
