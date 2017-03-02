using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(SgtAdvancedBelt))]
public class SgtAdvancedBelt_Editor : SgtBelt_Editor<SgtAdvancedBelt>
{
	protected override void OnInspector()
	{
		var updateMaterial        = false;
		var updateMeshesAndModels = false;

		DrawMaterial(ref updateMaterial);
		
		Separator();

		DrawAtlas(ref updateMaterial, ref updateMeshesAndModels);
		
		Separator();

		DrawLighting(ref updateMaterial);

		Separator();
		
		DrawDefault("Seed", ref updateMeshesAndModels);
		DrawDefault("DistanceDistribution", ref updateMeshesAndModels);
		DrawDefault("HeightDistribution", ref updateMeshesAndModels);
		DrawDefault("SpeedDistribution", ref updateMeshesAndModels);
		DrawDefault("SpeedNoiseDistribution", ref updateMeshesAndModels);
		DrawDefault("RadiusDistribution", ref updateMeshesAndModels);
		DrawDefault("SpinDistribution", ref updateMeshesAndModels);
		DrawDefault("AsteroidCount", ref updateMeshesAndModels);
		
		if (Any(t => t.Lit == true && t.LightingTex == null && t.GetComponent<SgtBeltLighting>() == null))
		{
			Separator();

			if (Button("Add Lighting") == true)
			{
				Each(t => SgtHelper.GetOrAddComponent<SgtBeltLighting>(t.gameObject));
			}
		}

		RequireObserver();
		
		serializedObject.ApplyModifiedProperties();

		if (updateMaterial        == true) DirtyEach(t => t.UpdateMaterial       ());
		if (updateMeshesAndModels == true) DirtyEach(t => t.UpdateMeshesAndModels());
	}
}
#endif

[ExecuteInEditMode]
[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Advanced Belt")]
public class SgtAdvancedBelt : SgtBelt
{
	[Tooltip("The random seed used when generating the asteroids")]
	[SgtSeed]
	public int Seed;

	[Tooltip("The distribution of asteroid distances in local space")]
	public AnimationCurve DistanceDistribution;

	[Tooltip("The distribution of asteroid heights in local space")]
	public AnimationCurve HeightDistribution;

	[Tooltip("The distribution of asteroid speeds in radians per second")]
	public AnimationCurve SpeedDistribution;

	[Tooltip("The distribution of asteroid speed offsets as a multiplier")]
	public AnimationCurve SpeedNoiseDistribution;

	[Tooltip("The distribution of asteroid radii in local space")]
	public AnimationCurve RadiusDistribution;

	[Tooltip("The distribution of asteroid spin in radians")]
	public AnimationCurve SpinDistribution;

	[Tooltip("The amount of asteroids generated in this belt")]
	public int AsteroidCount = 1000;
	
	private static Keyframe[] defaultDistanceKeyframes = new Keyframe[] { new Keyframe(0.0f, 1.0f), new Keyframe(1.0f, 2.0f) };

	private static Keyframe[] defaultHeightKeyframes = new Keyframe[] { new Keyframe(0.0f, -0.1f), new Keyframe(1.0f, 0.1f) };

	private static Keyframe[] defaultSpeedKeyframes = new Keyframe[] { new Keyframe(0.0f, 0.1f), new Keyframe(1.0f, 0.05f) };

	private static Keyframe[] defaultSpeedOffsetKeyframes = new Keyframe[] { new Keyframe(0.0f, 0.1f), new Keyframe(1.0f, 0.1f) };

	private static Keyframe[] defaultRadiusKeyframes = new Keyframe[] { new Keyframe(0.0f, 0.1f), new Keyframe(1.0f, 0.2f) };

	private static Keyframe[] defaultSpinKeyframes = new Keyframe[] { new Keyframe(0.0f, -0.1f), new Keyframe(1.0f, 0.1f) };
	
	public static SgtAdvancedBelt CreateAdvancedBelt(int layer = 0, Transform parent = null)
	{
		return CreateAdvancedBelt(layer, parent, Vector3.zero, Quaternion.identity, Vector3.one);
	}

	public static SgtAdvancedBelt CreateAdvancedBelt(int layer, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
	{
		var gameObject = SgtHelper.CreateGameObject("Advanced Belt", layer, parent, localPosition, localRotation, localScale);
		var belt       = gameObject.AddComponent<SgtAdvancedBelt>();

		return belt;
	}

#if UNITY_EDITOR
	[MenuItem(SgtHelper.GameObjectMenuPrefix + "Advanced Belt", false, 10)]
	private static void CreateAdvancedBeltMenuItem()
	{
		var belt = CreateAdvancedBelt(0, SgtHelper.GetSelectedParent());

		SgtHelper.SelectAndPing(belt);
	}
#endif
	
	protected override void StartOnce()
	{
		if (DistanceDistribution == null)
		{
			DistanceDistribution = new AnimationCurve();
			DistanceDistribution.keys = defaultDistanceKeyframes;
		}

		if (HeightDistribution == null)
		{
			HeightDistribution = new AnimationCurve();
			HeightDistribution.keys = defaultHeightKeyframes;
		}

		if (SpeedDistribution == null)
		{
			SpeedDistribution = new AnimationCurve();
			SpeedDistribution.keys = defaultSpeedKeyframes;
		}

		if (SpeedNoiseDistribution == null)
		{
			SpeedNoiseDistribution = new AnimationCurve();
			SpeedNoiseDistribution.keys = defaultSpeedOffsetKeyframes;
		}

		if (RadiusDistribution == null)
		{
			RadiusDistribution = new AnimationCurve();
			RadiusDistribution.keys = defaultRadiusKeyframes;
		}

		if (SpinDistribution == null)
		{
			SpinDistribution = new AnimationCurve();
			SpinDistribution.keys = defaultSpinKeyframes;
		}

		base.StartOnce();
	}

	protected override int BeginQuads()
	{
		SgtHelper.BeginRandomSeed(Seed);

		return AsteroidCount;
	}

	protected override void NextQuad(ref SgtBeltAsteroid asteroid, int asteroidIndex)
	{
		var distance01 = Random.value;
		var offset     = SpeedNoiseDistribution.Evaluate(distance01);

		asteroid.Variant       = Random.Range(int.MinValue, int.MaxValue);
		asteroid.Color         = Color.white;
		asteroid.Radius        = RadiusDistribution.Evaluate(Random.value);
		asteroid.Height        = HeightDistribution.Evaluate(Random.value);
		asteroid.Angle         = Random.Range(0.0f, Mathf.PI * 2.0f);
		asteroid.Spin          = SpinDistribution.Evaluate(Random.value);
		asteroid.OrbitAngle    = Random.Range(0.0f, Mathf.PI * 2.0f);
		asteroid.OrbitSpeed    = SpeedDistribution.Evaluate(distance01) + Random.Range(-offset, offset);
		asteroid.OrbitDistance = DistanceDistribution.Evaluate(distance01);
	}

	protected override void EndQuads()
	{
		SgtHelper.EndRandomSeed();
	}
}