using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(SgtThrusterRoll))]
public class SgtThrusterRoll_Editor : SgtEditor<SgtThrusterRoll>
{
	protected override void OnInspector()
	{
		DrawDefault("Rotation");
	}
}
#endif

// This component allows you to create simple thrusters that can apply forces to Rigidbodies based on their position. You can also use sprites to change the graphics
[ExecuteInEditMode]
[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Thruster Roll")]
public class SgtThrusterRoll : MonoBehaviour
{
	public class CameraState : SgtCameraState
	{
		public Quaternion LocalRotation;
	}

	[Tooltip("The rotation offset in degrees")]
	public Vector3 Rotation = new Vector3(0.0f, 90.0f, 90.0f);
	
	[System.NonSerialized]
	private List<CameraState> cameraStates;

	protected virtual void OnEnable()
	{
		Camera.onPreCull    += CameraPreCull;
		Camera.onPreRender  += CameraPreRender;
		Camera.onPostRender += CameraPostRender;
	}

	protected virtual void OnDisable()
	{
		Camera.onPreCull    -= CameraPreCull;
		Camera.onPreRender  -= CameraPreRender;
		Camera.onPostRender -= CameraPostRender;
	}
	
	private void CameraPreCull(Camera camera)
	{
		BeginSave();
		{
			var direction = transform.forward;
			var adjacent  = transform.position - camera.transform.position;
			var cross     = Vector3.Cross(direction, adjacent);
			
			if (cross != Vector3.zero)
			{
				transform.rotation = Quaternion.LookRotation(cross, direction) * Quaternion.Euler(Rotation);
			}
		}
		EndSave(camera);
	}

	private void CameraPreRender(Camera camera)
	{
		Restore(camera);
	}

	private void CameraPostRender(Camera camera)
	{
		Restore();
	}

	private void BeginSave()
	{
		Save(SgtCameraState.BeginSave(ref cameraStates));
	}
	
	private void Save(CameraState cameraState)
	{
		if (cameraState != null)
		{
			cameraState.LocalRotation = transform.localRotation;
		}
	}

	private void EndSave(Camera camera)
	{
		Save(SgtCameraState.EndSave(cameraStates, camera));
	}

	private void Restore(Camera camera = null)
	{
		var cameraState = SgtCameraState.Restore(cameraStates, camera, true);

		if (cameraState != null)
		{
			transform.localRotation = cameraState.LocalRotation;
		}
	}
}