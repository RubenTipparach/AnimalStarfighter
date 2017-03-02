using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(SgtSpacetimeBomb))]
public class SgtSpacetimeBomb_Editor : SgtEditor<SgtSpacetimeBomb>
{
	protected override void OnInspector()
	{
		DrawDefault("Spacetime");
		DrawDefault("Well");
		DrawDefault("Radius");
		BeginError(Any(t => t.ShrinkSpeed < 0.0f));
			DrawDefault("ShrinkSpeed");
		EndError();
	}
}
#endif

// This component handles adding/removing itself from a spacetime's well list
[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Spacetime Bomb")]
public class SgtSpacetimeBomb : MonoBehaviour
{
	[Tooltip("The Spacetime this bomb belongs to")]
	public SgtSpacetime Spacetime;
	
	[Tooltip("The well this bomb uses")]
	public SgtSpacetimeWell Well;
	
	[Tooltip("The maximum radius of this bomb")]
	public float Radius;
	
	[Tooltip("The speed at which this bomb fades away")]
	public float ShrinkSpeed;
	
	protected virtual void Update()
	{
		if (Well != null)
		{
			Well.Radius = Radius;
		}
		
		Radius -= ShrinkSpeed * Time.deltaTime;
		
		if (Radius <= 0.0f)
		{
			SgtHelper.Destroy(gameObject);
		}
	}
	
	protected virtual void Start()
	{
		if (Well != null && Spacetime != null && Spacetime.Wells.Contains(Well) == false)
		{
			Spacetime.Wells.Add(Well);
		}
	}
	
	protected virtual void OnDestroy()
	{
		if (Well != null && Spacetime != null)
		{
			Spacetime.Wells.Remove(Well);
		}
	}
}