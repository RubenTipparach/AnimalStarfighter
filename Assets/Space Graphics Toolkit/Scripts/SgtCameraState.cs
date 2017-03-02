using UnityEngine;
using System.Collections.Generic;

public class SgtCameraState
{
	// The camera associated with this state
	public Camera Camera;

	public static T BeginSave<T>(ref List<T> cameraStates)
		where T : SgtCameraState, new()
	{
		if (cameraStates == null)
		{
			cameraStates = new List<T>();
		}

		if (cameraStates.Count == 0)
		{
			var cameraState = SgtClassPool<T>.Pop() ?? new T();
			
			cameraState.Camera = null;

			cameraStates.Add(cameraState);

			return cameraState;
		}

		return null;
	}

	public static T EndSave<T>(List<T> cameraStates, Camera camera)
		where T : SgtCameraState, new()
	{
		var cameraState = SgtClassPool<T>.Pop() ?? new T();
		
		cameraState.Camera = camera;

		cameraStates.Add(cameraState);

		return cameraState;
	}

	public static T Restore<T>(List<T> cameraStates, Camera camera, bool remove)
		where T : SgtCameraState
	{
		if (cameraStates != null)
		{
			for (var i = cameraStates.Count - 1; i >= 0; i--)
			{
				var cameraState = cameraStates[i];

				if (cameraState.Camera == camera)
				{
					if (remove == true)
					{
						cameraStates.RemoveAt(i);

						SgtClassPool<T>.Add(cameraState);
					}

					return cameraState;
				}
			}
		}

		return null;
	}
}