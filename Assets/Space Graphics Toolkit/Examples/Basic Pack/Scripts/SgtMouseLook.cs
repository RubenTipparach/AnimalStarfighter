using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(SgtMouseLook))]
public class SgtMouseLook_Editor : SgtEditor<SgtMouseLook>
{
	protected override void OnInspector()
	{
		DrawDefault("Camera");
		DrawDefault("Require");
		DrawDefault("Sensitivity");
		DrawDefault("Pitch");
		DrawDefault("Yaw");
		BeginError(Any(t => t.Dampening < 0.0f));
			DrawDefault("Dampening");
		EndError();
	}
}
#endif

// This component handles mouselook when attached to the camera
[ExecuteInEditMode]
[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Mouse Look")]
public class SgtMouseLook : MonoBehaviour
{
	[Tooltip("The camera whose FOV will be used to scale the sensitivity (default = MainCamera)")]
	public Camera Camera;

	[Tooltip("The key that needs to be held down to look")]
	public KeyCode Require = KeyCode.Mouse0;

	[Tooltip("How quickly this rotates relative to the mouse movement")]
	public float Sensitivity = 2.0f;

	[Tooltip("The target X rotation")]
	[UnityEngine.Serialization.FormerlySerializedAs("TargetPitch")]
	public float Pitch;

	[Tooltip("The target Y rotation")]
	[UnityEngine.Serialization.FormerlySerializedAs("TargetYaw")]
	public float Yaw;

	[Tooltip("The speed at which this approaches the target rotation")]
	public float Dampening = 10.0f;

	private float currentPitch;

	private float currentYaw;

	protected virtual void Awake()
	{
		currentPitch = Pitch;
		currentYaw   = Yaw;
	}

	protected virtual void Update()
	{
		Pitch = Mathf.Clamp(Pitch, -89.9f, 89.9f);

		var sensitivity = Sensitivity;

		var camera = Camera;

		if (camera == null)
		{
			camera = Camera.main;
		}

		if (camera != null && camera.orthographic == false)
		{
			sensitivity *= camera.fieldOfView / 60.0f;
		}

		if (Require == KeyCode.None || Input.GetKey(Require) == true)
		{
			Pitch -= Input.GetAxisRaw("Mouse Y") * sensitivity;
			Yaw   += Input.GetAxisRaw("Mouse X") * sensitivity;
		}

		currentPitch = SgtHelper.Dampen(currentPitch, Pitch, Dampening, Time.deltaTime, 0.1f);
		currentYaw   = SgtHelper.Dampen(currentYaw  , Yaw  , Dampening, Time.deltaTime, 0.1f);

		var rotation = Quaternion.Euler(currentPitch, currentYaw, 0.0f);

		SgtHelper.SetLocalRotation(transform, rotation);
	}
}
