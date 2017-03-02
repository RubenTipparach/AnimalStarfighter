using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(SgtTerrain))]
public abstract class SgtTerrainModifier : MonoBehaviour
{
	[System.NonSerialized]
	protected SgtTerrain terrain;

	public void DirtyTerrain()
	{
		if (terrain == null) terrain = GetComponent<SgtTerrain>();

		terrain.UpdateMeshesDirty();
	}

	protected virtual void OnEnable()
	{
		DirtyTerrain();
	}

	protected virtual void OnDisable()
	{
		DirtyTerrain();
	}
}
