using UnityEngine;

public abstract class SgtShadow : MonoBehaviour
{
	[Tooltip("The light that this shadow is being cast away from")]
	public Light Light;
	
	public abstract Texture GetTexture();
	
	// Show enable/disable checkbox
	protected virtual void Start()
	{
	}
	
	public virtual bool CalculateShadow(ref Matrix4x4 matrix, ref float ratio)
	{
		if (SgtHelper.Enabled(Light) == true && Light.intensity > 0.0f)
		{
			return true;
		}
		
		return false;
	}
}