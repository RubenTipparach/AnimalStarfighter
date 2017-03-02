using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(SgtTerrainCylindrical))]
public class SgtTerrainCylindrical_Editor : SgtEditor<SgtTerrainCylindrical>
{
	protected override void OnInspector()
	{
	}
}

#endif

[ExecuteInEditMode]
[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Terrain Cylindrical")]
public class SgtTerrainCylindrical : SgtTerrainModifier
{
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
		SgtTerrain.currentCoord1.x = Mathf.Atan2(SgtTerrain.currentPosition.x, SgtTerrain.currentPosition.z);
		SgtTerrain.currentCoord1.y = Mathf.Asin(SgtTerrain.currentPosition.y / SgtTerrain.currentPosition.magnitude);

		SgtTerrain.currentCoord1.x = 0.5f - SgtTerrain.currentCoord1.x / (Mathf.PI * 2.0f);
		SgtTerrain.currentCoord1.y = 0.5f + SgtTerrain.currentCoord1.y / Mathf.PI;

		SgtTerrain.currentCoord1 = SgtHelper.CartesianToPolarUV(SgtTerrain.currentPosition);

		if (SgtTerrain.currentCoord1.x < 0.001f)
		{
			if (SgtTerrain.currentPointCenter.x < 0.0f)
			{
				SgtTerrain.currentCoord1.x = 1.0f;
			}
		}
		else if (SgtTerrain.currentCoord1.x > 0.999f)
		{
			if (SgtTerrain.currentPointCenter.x > 0.0f)
			{
				SgtTerrain.currentCoord1.x = 0.0f;
			}
		}
	}
}
