using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(SgtTerrainColor))]
public class SgtTerrainColor_Editor : SgtEditor<SgtTerrainColor>
{
	protected override void OnInspector()
	{
		var dirtyTerrain = false;

		DrawDefault("Color", ref dirtyTerrain);

		Separator();

		BeginError(Any(t => t.Height < 0.0f));
			DrawDefault("Height", ref dirtyTerrain);
		EndError();
		BeginError(Any(t => t.HeightAllowance < 0.0f));
			DrawDefault("HeightAllowance", ref dirtyTerrain);
		EndError();
		
		Separator();

		BeginError(Any(t => t.Normal < 0.0f));
			DrawDefault("Normal", ref dirtyTerrain);
		EndError();
		BeginError(Any(t => t.NormalAllowance < 0.0f));
			DrawDefault("NormalAllowance", ref dirtyTerrain);
		EndError();
		
		if (dirtyTerrain == true) DirtyEach(t => t.DirtyTerrain());
	}
}
#endif

[ExecuteInEditMode]
[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Terrain Color")]
public class SgtTerrainColor : SgtTerrainModifier
{
	[Tooltip("The color that will be applied to the terrain")]
	public Color Color = Color.white;

	[Tooltip("The terrain height where this color is strongest (in local coordinates)")]
	public float Height = 1.0f;
	
	[Tooltip("The amount the height can be off while still receiving color (0 = disabled)")]
	public float HeightAllowance = 0.2f;

	[Tooltip("The slope angle where this color is strongest (in cosine coordinates)")]
	[Range(0.0f, 1.0f)]
	public float Normal;

	[Tooltip("The amount the normal can be off while still receiving color (0 = disabled)")]
	public float NormalAllowance = 0.5f;
	
	protected override void OnEnable()
	{
		base.OnEnable();

		terrain.OnCalculateVertexData += CalculateVertexData;
	}

	protected override void OnDisable()
	{
		base.OnDisable();

		terrain.OnCalculateVertexData -= CalculateVertexData;
	}

	private void CalculateVertexData()
	{
		var weight   = 1.0f;
		var distance = SgtTerrain.currentPosition.magnitude;

		if (HeightAllowance != 0.0f)
		{
			weight *= Mathf.SmoothStep(1.0f, 0.0f, Mathf.Abs(distance - Height) / HeightAllowance);
		}

		if (NormalAllowance != 0.0f)
		{
			var slope = 1.0f - Vector3.Dot(SgtTerrain.currentNormal, SgtTerrain.currentPosition / distance);

			weight *= Mathf.SmoothStep(1.0f, 0.0f, Mathf.Abs(slope - Normal) / NormalAllowance);
		}

		SgtTerrain.currentColor = Color.Lerp(SgtTerrain.currentColor, Color, weight);
	}
}
