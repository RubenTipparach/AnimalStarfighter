using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(SgtTerrainDisplacer))]
public class SgtTerrainDisplacer_Editor : SgtEditor<SgtTerrainDisplacer>
{
	protected override void OnInspector()
	{
		BeginError(Any(t => t.Heightmap == null));
			DrawDefault("Heightmap");
		EndError();
		BeginError(Any(t => t.Strength == 0.0f));
			DrawDefault("Strength");
		EndError();
	}
}
#endif

[ExecuteInEditMode]
[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Terrain Displacer")]
public class SgtTerrainDisplacer : SgtTerrainModifier
{
	[Tooltip("The heightmap texture using a cylindrical (equirectangular) projection")]
	public Texture2D Heightmap;

	[Tooltip("The strength of the displacement")]
	[Range(0.0f, 1.0f)]
	public float Strength = 1.0f;

	protected override void OnEnable()
	{
		base.OnEnable();

		terrain.OnCalculateDisplacement += CalculateDisplacement;
	}

	protected override void OnDisable()
	{
		base.OnDisable();

		terrain.OnCalculateDisplacement -= CalculateDisplacement;
	}
	
	private void CalculateDisplacement(Vector3 localPosition, ref float height)
	{
		if (Heightmap != null)
		{
			var uv    = SgtHelper.CartesianToPolarUV(localPosition);
			var color = SampleBilinear(uv);

			height += (color.a - 0.5f) * Strength;
		}
	}

	private Color SampleBilinear(Vector2 uv)
	{
		return Heightmap.GetPixelBilinear(uv.x, uv.y);
	}
}
