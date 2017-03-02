using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(SgtMouseZoom))]
public class SgtMouseZoom_Editor : SgtEditor<SgtMouseZoom>
{
	protected override void OnInspector()
	{
		DrawDefault("Require");
		DrawDefault("Camera");
		BeginError(Any(t => t.Sensitivity <= 0.0f || t.Sensitivity >= 1.0f));
			DrawDefault("Sensitivity");
		EndError();
		DrawDefault("Zoom");
		BeginError(Any(t => t.ZoomMin > t.ZoomMax));
			DrawDefault("ZoomMin");
			DrawDefault("ZoomMax");
		EndError();
		BeginError(Any(t => t.Dampening < 0.0f));
			DrawDefault("Dampening");
		EndError();
	}
}
#endif

// This component handles mouse zoom
[ExecuteInEditMode]
[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Mouse Zoom")]
public class SgtMouseZoom : MonoBehaviour
{
	[Tooltip("The camera that will be zoomed (default = MainCamera)")]
	public Camera Camera;

	[Tooltip("The key that needs to be held down to zoom")]
	public KeyCode Require = KeyCode.None;
	
	[Tooltip("How quickly this rotates relative to the mouse movement")]
	public float Sensitivity = 0.1f;

	[Tooltip("The zoom value")]
	public float Zoom;

	[Tooltip("The minimum zoom value")]
	public float ZoomMin = 1.0f;

	[Tooltip("The maximum zoom value")]
	public float ZoomMax = 90.0f;

	[Tooltip("The speed at which this approaches the target rotation")]
	public float Dampening = 10.0f;

	private float currentZoom;
	
	protected virtual void Update()
	{
		var camera = Camera;

		if (camera == null)
		{
			camera = Camera.main;
		}

		if (camera != null)
		{
			if (currentZoom == 0.0f)
			{
				if (camera.orthographic == true)
				{
					currentZoom = camera.orthographicSize;
				}
				else
				{
					currentZoom = camera.fieldOfView;
				}

				Zoom = currentZoom;
			}

			if (Require == KeyCode.None || Input.GetKey(Require) == true)
			{
				var scroll = Input.mouseScrollDelta.y;

				if (scroll > 0.0f)
				{
					Zoom *= 1.0f - Sensitivity;
				}

				if (scroll < 0.0f)
				{
					Zoom *= 1.0f + Sensitivity;
				}
			}

			Zoom        = Mathf.Clamp(Zoom, ZoomMin, ZoomMax);
			currentZoom = SgtHelper.Dampen(currentZoom, Zoom, Dampening, Time.deltaTime, 0.1f);

			if (camera.orthographic == true)
			{
				camera.orthographicSize = currentZoom;
			}
			else
			{
				camera.fieldOfView = currentZoom;
			}
		}
	}
}
