using UnityEngine;

[ExecuteInEditMode]
public abstract class SgtDepth : MonoBehaviour
{
	[Tooltip("The layers that will be sampled when calculating the optical depth")]
	public LayerMask Layers = Physics.DefaultRaycastLayers;

	[Tooltip("The transition style between 0..1 depth")]
	public SgtEase.Type Ease = SgtEase.Type.Linear;

	// Prevent recursive depth calculation from Camera rendering
	private static bool busy;
	
	// Calculates the 0..1 depth between the eye and target
	public float Calculate(Vector3 eye, Vector3 target)
	{
		if (busy == true)
		{
			Debug.LogError("Calculate is being called recursively");
			
			return 0.0f;
		}

		var coverage = default(float);

		busy = true;
		{
			coverage = DoCalculate(eye, target);
		}
		busy = false;

		return 1.0f - SgtEase.Evaluate(Ease, 1.0f - coverage);
	}

	protected abstract float DoCalculate(Vector3 eye, Vector3 target);
}