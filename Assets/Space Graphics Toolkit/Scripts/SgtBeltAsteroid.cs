using UnityEngine;

[System.Serializable]
public class SgtBeltAsteroid
{
	// Temp instance used when generating the belt
	public static SgtBeltAsteroid Temp = new SgtBeltAsteroid();

	[Tooltip("The coordinate index in the asteroid texture")]
	public int Variant;
	
	[Tooltip("Color tint of this asteroid")]
	public Color Color = Color.white;
	
	[Tooltip("Radius of this asteroid in local space")]
	public float Radius;
	
	[Tooltip("Height of this asteroid's orbit in local space")]
	public float Height;
	
	[Tooltip("The base roll angle of this asteroid in radians")]
	public float Angle;
	
	[Tooltip("How fast this asteroid rolls in radians per second")]
	public float Spin;
	
	[Tooltip("The base angle of this asteroid's orbit in radians")]
	public float OrbitAngle;
	
	[Tooltip("The speed of this asteroid's orbit in radians")]
	public float OrbitSpeed;
	
	[Tooltip("The distance of this asteroid's orbit in radians")]
	public float OrbitDistance;
	
	public void CopyFrom(SgtBeltAsteroid other)
	{
		Variant       = other.Variant;
		Color         = other.Color;
		Radius        = other.Radius;
		Height        = other.Height;
		Angle         = other.Angle;
		Spin          = other.Spin;
		OrbitAngle    = other.OrbitAngle;
		OrbitSpeed    = other.OrbitSpeed;
		OrbitDistance = other.OrbitDistance;
	}
}