using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(SgtEllipticalStarfield))]
public class SgtEllipticalStarfield_Editor : SgtPointStarfield_Editor<SgtEllipticalStarfield>
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
		BeginError(Any(t => t.Radius <= 0.0f));
			DrawDefault("Radius", ref updateMeshesAndModels);
		EndError();
		DrawDefault("Symmetry", ref updateMeshesAndModels);
		DrawDefault("Offset", ref updateMeshesAndModels);
		DrawDefault("Inverse", ref updateMeshesAndModels);
		
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
		BeginError(Any(t => t.StarRadiusBias < 1.0f));
			DrawDefault("StarRadiusBias", ref updateMeshesAndModels);
		EndError();
		DrawDefault("StarPulseMax", ref updateMeshesAndModels);
		
		RequireObserver();

		serializedObject.ApplyModifiedProperties();

		if (updateMaterial        == true) DirtyEach(t => t.UpdateMaterial       ());
		if (updateMeshesAndModels == true) DirtyEach(t => t.UpdateMeshesAndModels());
	}
}
#endif

[ExecuteInEditMode]
[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Elliptical Starfield")]
public class SgtEllipticalStarfield : SgtPointStarfield
{
	[Tooltip("The random seed used when generating the stars")]
	[SgtSeed]
	public int Seed;

	[Tooltip("The radius of the starfield")]
	public float Radius = 1.0f;

	[Tooltip("Should more stars be placed near the horizon?")]
	[Range(0.0f, 1.0f)]
	public float Symmetry = 1.0f;

	[Tooltip("How far from the center the distribution begins")]
	[Range(0.0f, 1.0f)]
	public float Offset = 0.0f;

	[Tooltip("Invert the distribution?")]
	public bool Inverse;

	[Tooltip("The amount of stars that will be generated in the starfield")]
	public int StarCount = 1000;

	[Tooltip("The minimum radius of stars in the starfield")]
	public float StarRadiusMin = 0.01f;

	[Tooltip("The maximum radius of stars in the starfield")]
	public float StarRadiusMax = 0.05f;

	[Tooltip("How likely the size picking will pick smaller stars over larger ones (1 = default/linear)")]
	public float StarRadiusBias = 1.0f;

	[Tooltip("The maximum amount a star's size can pulse over time. A value of 1 means the star can potentially pulse between its maximum size, and 0")]
	[Range(0.0f, 1.0f)]
	public float StarPulseMax = 1.0f;
	
	public static SgtEllipticalStarfield CreateEllipticalStarfield(int layer = 0, Transform parent = null)
	{
		return CreateEllipticalStarfield(layer, parent, Vector3.zero, Quaternion.identity, Vector3.one);
	}

	public static SgtEllipticalStarfield CreateEllipticalStarfield(int layer, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
	{
		var gameObject = SgtHelper.CreateGameObject("Elliptical Starfield", layer, parent, localPosition, localRotation, localScale);
		var starfield  = gameObject.AddComponent<SgtEllipticalStarfield>();

		return starfield;
	}

#if UNITY_EDITOR
	[MenuItem(SgtHelper.GameObjectMenuPrefix + "Elliptical Starfield", false, 10)]
	private static void CreateEllipticalStarfieldMenuItem()
	{
		var parent    = SgtHelper.GetSelectedParent();
		var starfield = CreateEllipticalStarfield(parent != null ? parent.gameObject.layer : 0, parent);

		SgtHelper.SelectAndPing(starfield);
	}
#endif

#if UNITY_EDITOR
	protected override void OnDrawGizmosSelected()
	{
		base.OnDrawGizmosSelected();

		Gizmos.matrix = transform.localToWorldMatrix;

		Gizmos.DrawWireSphere(Vector3.zero, Radius);
		Gizmos.DrawWireSphere(Vector3.zero, Radius * Offset);
	}
#endif

	protected override int BeginQuads()
	{
		SgtHelper.BeginRandomSeed(Seed);

		return StarCount;
	}

	protected override void NextQuad(ref SgtPointStar quad, int starIndex)
	{
		var position  = Random.insideUnitSphere;
		var magnitude = Offset;

		if (Inverse == true)
		{
			magnitude += (1.0f - position.magnitude) * (1.0f - Offset);
		}
		else
		{
			magnitude += position.magnitude * (1.0f - Offset);
		}

		position.y *= Symmetry;

		quad.Variant     = Random.Range(int.MinValue, int.MaxValue);
		quad.Color       = Color.white;
		quad.Radius      = Mathf.Lerp(StarRadiusMin, StarRadiusMax, Mathf.Pow(Random.value, StarRadiusBias));
		quad.Angle       = Random.Range(-180.0f, 180.0f);
		quad.Position    = position.normalized * magnitude * Radius;
		quad.PulseRange  = Random.value * StarPulseMax;
		quad.PulseSpeed  = Random.value;
		quad.PulseOffset = Random.value;
	}

	protected override void EndQuads()
	{
		SgtHelper.EndRandomSeed();
	}
}