using UnityEngine;

public abstract class SgtShape : MonoBehaviour
{
	// Returns a 0..1 value, where 1 is fully inside
	public abstract float GetDensity(Vector3 worldPosition);
}
