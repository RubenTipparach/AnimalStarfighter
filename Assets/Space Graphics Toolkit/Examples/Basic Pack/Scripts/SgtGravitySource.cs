using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(SgtGravitySource))]
public class SgtGravitySource_Editor : SgtEditor<SgtGravitySource>
{
	protected override void OnInspector()
	{
		DrawDefault("Mass");
	}
}
#endif

// This component allows gravity receivers to get attracted to it
[ExecuteInEditMode]
[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Gravity Source")]
public class SgtGravitySource : MonoBehaviour
{
	// All active and enabled gravity sources
	public static List<SgtGravitySource> AllGravitySources = new List<SgtGravitySource>();

	[Tooltip("The mass of this gravity source (automatically set if there is a Rigidbody)")]
	public float Mass = 100.0f;

	[System.NonSerialized]
	private Rigidbody body;

	protected virtual void OnEnable()
	{
		AllGravitySources.Add(this);
	}

	protected virtual void OnDisable()
	{
		AllGravitySources.Remove(this);
	}

	protected virtual void Update()
	{
		if (body == null) body = GetComponent<Rigidbody>();

		if (body != null)
		{
			Mass = body.mass;
		}
	}
}
