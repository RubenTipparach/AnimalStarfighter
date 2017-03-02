using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(SgtBoxStarfield))]
public class SgtBoxStarfield_Editor : SgtPointStarfield_Editor<SgtBoxStarfield>
{
	protected override void OnInspector()
	{
		var updateMaterial        = false;
		var updateMeshesAndModels = false;
		
		DrawMaterial(ref updateMaterial);

		Separator();

		DrawAtlas(ref updateMaterial, ref updateMeshesAndModels);

		Separator();

		DrawPointMaterial(ref updateMaterial);

		Separator();

		DrawDefault("Seed", ref updateMeshesAndModels);
		BeginError(Any(t => t.Extents == Vector3.zero));
			DrawDefault("Extents", ref updateMeshesAndModels);
		EndError();
		DrawDefault("Offset", ref updateMeshesAndModels);
		
		Separator();
		
		BeginError(Any(t => t.StarCount < 0));
			DrawDefault("StarCount", ref updateMeshesAndModels);
		EndError();
		BeginError(Any(t => t.StarRadiusMin < 0.0f || t.StarRadiusMin > t.StarRadiusMax));
			DrawDefault("StarRadiusMin", ref updateMeshesAndModels);
		EndError();
		BeginError(Any(t => t.StarRadiusMax < 0.0f || t.StarRadiusMin > t.StarRadiusMax));
			DrawDefault("StarRadiusMax", ref updateMeshesAndModels);
		EndError();
		DrawDefault("StarPulseMax", ref updateMeshesAndModels);
		
		RequireObserver();

		serializedObject.ApplyModifiedProperties();

		if (updateMaterial        == true) DirtyEach(t => t.UpdateMaterial       ());
		if (updateMeshesAndModels == true) DirtyEach(t => t.UpdateMeshesAndModels());
	}
}
#endif

// This component allows you to make star distributions that are box/cube shaped
[ExecuteInEditMode]
[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Box Starfield")]
public class SgtBoxStarfield : SgtPointStarfield
{
	[Tooltip("The random seed used when generating the stars")]
	[SgtSeed]
	public int Seed;

	[Tooltip("The size of the starfield box")]
	public Vector3 Extents = Vector3.one;

	[Tooltip("How far from the center of the box the stars will spawn from. If this is 1, then the stars will only spawn at the edges")]
	[Range(0.0f, 1.0f)]
	public float Offset = 0.0f;

	[Tooltip("The amount of stars that will be generated in the starfield")]
	public int StarCount = 1000;

	[Tooltip("The minimum radius of stars in the starfield")]
	public float StarRadiusMin = 0.0f;

	[Tooltip("The maximum radius of stars in the starfield")]
	public float StarRadiusMax = 0.05f;

	[Tooltip("The maximum amount a star's size can pulse over time. A value of 1 means the star can potentially pulse between its maximum size, and 0")]
	[Range(0.0f, 1.0f)]
	public float StarPulseMax = 1.0f;
	
	// This allows you to create a box starfield GameObject under the specified parent
	public static SgtBoxStarfield CreateBoxStarfield(int layer = 0, Transform parent = null)
	{
		return CreateBoxStarfield(layer, parent, Vector3.zero, Quaternion.identity, Vector3.one);
	}

	public static SgtBoxStarfield CreateBoxStarfield(int layer, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
	{
		var gameObject = SgtHelper.CreateGameObject("Box Starfield", layer, parent, localPosition, localRotation, localScale);
		var starfield  = gameObject.AddComponent<SgtBoxStarfield>();

		return starfield;
	}

#if UNITY_EDITOR
	// Show the editor-only GameObject menu option to make a box starfield
	[MenuItem(SgtHelper.GameObjectMenuPrefix + "Box Starfield", false, 10)]
	private static void CreateBoxStarfieldMenuItem()
	{
		var parent    = SgtHelper.GetSelectedParent();
		var starfield = CreateBoxStarfield(parent != null ? parent.gameObject.layer : 0, parent);

		SgtHelper.SelectAndPing(starfield);
	}
#endif

#if UNITY_EDITOR
	protected override void OnDrawGizmosSelected()
	{
		base.OnDrawGizmosSelected();

		Gizmos.matrix = transform.localToWorldMatrix;

		Gizmos.DrawWireCube(Vector3.zero, Extents * Offset);
		Gizmos.DrawWireCube(Vector3.zero, Extents);
	}
#endif

	protected override int BeginQuads()
	{
		SgtHelper.BeginRandomSeed(Seed);

		return StarCount;
	}
	
	protected override void NextQuad(ref SgtPointStar star, int starIndex)
	{
		var x        = Random.Range(-0.5f, 0.5f);
		var y        = Random.Range(-0.5f, 0.5f);
		var z        = Random.Range(Offset * 0.5f, 0.5f);
		var position = default(Vector3);

		if (Random.value >= 0.5f)
		{
			z = -z;
		}

		switch (Random.Range(0, 3))
		{
			case 0: position = new Vector3(z, x, y); break;
			case 1: position = new Vector3(x, z, y); break;
			case 2: position = new Vector3(x, y, z); break;
		}

		star.Variant     = Random.Range(int.MinValue, int.MaxValue);
		star.Color       = Color.white;
		star.Radius      = Random.Range(StarRadiusMin, StarRadiusMax);
		star.Angle       = Random.Range(-180.0f, 180.0f);
		star.Position    = Vector3.Scale(position, Extents);
		star.PulseRange  = Random.value * StarPulseMax;
		star.PulseSpeed  = Random.value;
		star.PulseOffset = Random.value;
	}

	protected override void EndQuads()
	{
		SgtHelper.EndRandomSeed();
	}
}