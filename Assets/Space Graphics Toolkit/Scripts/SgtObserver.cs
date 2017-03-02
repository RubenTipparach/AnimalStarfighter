using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(SgtObserver))]
public class SgtObserver_Editor : SgtEditor<SgtObserver>
{
	protected override void OnInspector()
	{
		DrawDefault("RollAngle");
	}
}
#endif

[ExecuteInEditMode]
[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Observer")]
public class SgtObserver : MonoBehaviour
{
	// All active and enabled observers in the scene
	public static List<SgtObserver> AllObservers = new List<SgtObserver>();
	
	public static System.Action<SgtObserver> OnObserverPreCull;
	
	public static System.Action<SgtObserver> OnObserverPreRender;
	
	public static System.Action<SgtObserver> OnObserverPostRender;
	
	[Tooltip("The amount of degrees this observer has rolled (used to counteract billboard non-rotation)")]
	public float RollAngle;
	
	// A quaternion of the current roll angle
	public Quaternion RollQuaternion = Quaternion.identity;
	
	// A matrix of the current roll angle
	public Matrix4x4 RollMatrix = Matrix4x4.identity;
	
	// The change in position of this GameObject over the past frame
	[System.NonSerialized]
	public Vector3 DeltaPosition;
	
	// The current velocity of this GameObject per second
	[System.NonSerialized]
	public Vector3 Velocity;
	
	// Previous frame rotation
	[System.NonSerialized]
	public Quaternion OldRotation = Quaternion.identity;
	
	// Previous frame position
	[System.NonSerialized]
	public Vector3 OldPosition;

	// The camera this observer is attached to
	[System.NonSerialized]
	public Camera Camera;
	
	// Find the observer attached to a specific camera, if it exists
	public static SgtObserver Find(Camera camera)
	{
		for (var i = AllObservers.Count - 1; i >= 0; i--)
		{
			var observer = AllObservers[i];

			if (observer.Camera == camera)
			{
				return observer;
			}
		}

		return null;
	}
	
	protected virtual void OnEnable()
	{
		AllObservers.Add(this);

		OldRotation = transform.rotation;
		OldPosition = transform.position;

		if (Camera == null) Camera = GetComponent<Camera>();
	}
	
	protected virtual void OnPreCull()
	{
		if (OnObserverPreCull != null) OnObserverPreCull(this);
	}
	
	protected virtual void OnPreRender()
	{
		if (OnObserverPreRender != null) OnObserverPreRender(this);
	}
	
	protected virtual void OnPostRender()
	{
		if (OnObserverPostRender != null) OnObserverPostRender(this);
	}
	
	protected virtual void LateUpdate()
	{
		var newRotation   = transform.rotation;
		var newPosition   = transform.position;
		var deltaRotation = Quaternion.Inverse(OldRotation) * newRotation;
		var deltaPosition = OldPosition - newPosition;
		
		OldRotation    = newRotation;
		OldPosition    = newPosition;
		RollAngle      = (RollAngle - deltaRotation.eulerAngles.z) % 360.0f;
		RollQuaternion = Quaternion.Euler(0.0f, 0.0f, RollAngle);
		RollMatrix     = SgtHelper.Rotation(RollQuaternion);
		DeltaPosition  = deltaPosition;
		Velocity       = SgtHelper.Reciprocal(Time.deltaTime) * deltaPosition;
	}
	
	protected virtual void OnDisable()
	{
		AllObservers.Remove(this);
	}
}