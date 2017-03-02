using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(SgtSpiralStarfield))]
public class SgtSpiralStarfield_Editor : SgtPointStarfield_Editor<SgtSpiralStarfield>
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
		DrawDefault("Radius", ref updateMeshesAndModels);
		BeginError(Any(t => t.ArmCount <= 0));
			DrawDefault("ArmCount", ref updateMeshesAndModels);
		EndError();
		DrawDefault("Twist", ref updateMeshesAndModels);
		DrawDefault("Thickness", ref updateMeshesAndModels);
		
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
[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Spiral Starfield")]
public class SgtSpiralStarfield : SgtPointStarfield
{
	[Tooltip("The random seed used when generating the stars")]
	[SgtSeed]
	public int Seed;

	[Tooltip("The radius of the starfield")]
	public float Radius = 1.0f;
	
	[Tooltip("The amount of spiral arms")]
	public int ArmCount = 1;

	[Tooltip("The amound each arm twists")]
	public float Twist = 1.0f;

	[Tooltip("The thickness of the spirals")]
	public AnimationCurve Thickness;

	[Tooltip("The amount of stars that will be generated in the starfield")]
	public int StarCount = 1000;

	[Tooltip("The minimum radius of stars in the starfield")]
	public float StarRadiusMin = 0.0f;

	[Tooltip("The maximum radius of stars in the starfield")]
	public float StarRadiusMax = 0.05f;

	[Tooltip("How likely the size picking will pick smaller stars over larger ones (1 = default/linear)")]
	public float StarRadiusBias = 1.0f;

	[Tooltip("The maximum amount a star's size can pulse over time. A value of 1 means the star can potentially pulse between its maximum size, and 0")]
	[Range(0.0f, 1.0f)]
	public float StarPulseMax = 1.0f;
	
	private static Keyframe[] defaultThicknessKeyframes = new Keyframe[] { new Keyframe(0.0f, 0.025f), new Keyframe(1.0f, 0.25f) };

	// Temp vars used during generation
	private static float armStep;
	private static float twistStep;

	public static SgtSpiralStarfield CreateSpiralStarfield(int layer = 0, Transform parent = null)
	{
		return CreateSpiralStarfield(layer, parent, Vector3.zero, Quaternion.identity, Vector3.one);
	}

	public static SgtSpiralStarfield CreateSpiralStarfield(int layer, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
	{
		var gameObject = SgtHelper.CreateGameObject("Spiral Starfield", layer, parent, localPosition, localRotation, localScale);
		var starfield  = gameObject.AddComponent<SgtSpiralStarfield>();

		return starfield;
	}

#if UNITY_EDITOR
	[MenuItem(SgtHelper.GameObjectMenuPrefix + "Spiral Starfield", false, 10)]
	private static void CreateSpiralStarfieldMenuItem()
	{
		var parent    = SgtHelper.GetSelectedParent();
		var starfield = CreateSpiralStarfield(parent != null ? parent.gameObject.layer : 0, parent);

		SgtHelper.SelectAndPing(starfield);
	}
#endif

	protected override void Start()
	{
		base.Start();

		if (Thickness == null)
		{
			Thickness = new AnimationCurve();
			Thickness.keys = defaultThicknessKeyframes;

			UpdateMeshesAndModels();
		}
	}

#if UNITY_EDITOR
	protected override void OnDrawGizmosSelected()
	{
		base.OnDrawGizmosSelected();

		Gizmos.matrix = transform.localToWorldMatrix;

		Gizmos.DrawWireSphere(Vector3.zero, Radius);
	}
#endif

	protected override int BeginQuads()
	{
		SgtHelper.BeginRandomSeed(Seed);

		if (Thickness != null)
		{
			armStep   = 360.0f * SgtHelper.Reciprocal(ArmCount);
			twistStep = 360.0f * Twist;

			return StarCount;
		}

		return 0;
	}

	protected override void NextQuad(ref SgtPointStar star, int starIndex)
	{
		var position  = Random.insideUnitSphere;
		var magnitude = 1 - (Random.insideUnitSphere).magnitude;

		position *= (1 - magnitude) * Thickness.Evaluate(Random.value);

		position += Quaternion.AngleAxis(starIndex * armStep + magnitude * twistStep, Vector3.up) * Vector3.forward * magnitude;

		star.Variant     = Random.Range(int.MinValue, int.MaxValue);
		star.Color       = Color.white;
		star.Radius      = Mathf.Lerp(StarRadiusMin, StarRadiusMax, Mathf.Pow(Random.value, StarRadiusBias));
		star.Angle       = Random.Range(-180.0f, 180.0f);
		star.Position    = position * Radius;
		star.PulseRange  = Random.value * StarPulseMax;
		star.PulseSpeed  = Random.value;
		star.PulseOffset = Random.value;
	}

	protected override void EndQuads()
	{
		SgtHelper.EndRandomSeed();
	}
}