using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(SgtStaticStarfield))]
public class SgtStaticStarfield_Editor : SgtQuads_Editor<SgtStaticStarfield>
{
	protected override void OnInspector()
	{
		var updateMaterial        = false;
		var updateMeshesAndModels = false;

		DrawMaterial(ref updateMaterial);

		Separator();

		DrawAtlas(ref updateMaterial, ref updateMeshesAndModels);
		
		Separator();

		DrawDefault("Seed", ref updateMeshesAndModels);
		BeginError(Any(t => t.Radius <= 0.0f));
			DrawDefault("Radius", ref updateMeshesAndModels);
		EndError();
		DrawDefault("Symmetry", ref updateMeshesAndModels);
		
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
		DrawDefault("StarColors", ref updateMeshesAndModels);
		
		RequireObserver();

		if (updateMaterial        == true) DirtyEach(t => t.UpdateMaterial       ());
		if (updateMeshesAndModels == true) DirtyEach(t => t.UpdateMeshesAndModels());
	}
}
#endif

[ExecuteInEditMode]
[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Static Starfield")]
public class SgtStaticStarfield : SgtQuads
{
	[Tooltip("The random seed used when generating the stars")]
	[SgtSeed]
	public int Seed;

	[Tooltip("The radius of the starfield")]
	public float Radius = 1.0f;

	[Tooltip("Should more stars be placed near the horizon?")]
	[Range(0.0f, 1.0f)]
	public float Symmetry = 1.0f;

	[Tooltip("The amount of stars that will be generated in the starfield")]
	public int StarCount = 1000;

	[Tooltip("The minimum radius of stars in the starfield")]
	public float StarRadiusMin = 0.0f;

	[Tooltip("The maximum radius of stars in the starfield")]
	public float StarRadiusMax = 0.05f;

	[Tooltip("How likely the size picking will pick smaller stars over larger ones (1 = default/linear)")]
	public float StarRadiusBias = 1.0f;

	[Tooltip("Each star is given a random color from this gradient")]
	public Gradient StarColors;
	
	protected override string ShaderName
	{
		get
		{
			return SgtHelper.ShaderNamePrefix + "StaticStarfield";
		}
	}

	public static SgtStaticStarfield CreateStaticStarfield(int layer = 0, Transform parent = null)
	{
		return CreateStaticStarfield(layer, parent, Vector3.zero, Quaternion.identity, Vector3.one);
	}

	public static SgtStaticStarfield CreateStaticStarfield(int layer, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
	{
		var gameObject = SgtHelper.CreateGameObject("Static Starfield", layer, parent, localPosition, localRotation, localScale);
		var starfield  = gameObject.AddComponent<SgtStaticStarfield>();

		return starfield;
	}

#if UNITY_EDITOR
	[MenuItem(SgtHelper.GameObjectMenuPrefix + "Static Starfield", false, 10)]
	private static void CreateStaticStarfieldMenuItem()
	{
		var parent    = SgtHelper.GetSelectedParent();
		var starfield = CreateStaticStarfield(parent != null ? parent.gameObject.layer : 0, parent);

		SgtHelper.SelectAndPing(starfield);
	}
#endif
	
	protected override void OnEnable()
	{
		base.OnEnable();

		Camera.onPreCull    += CameraPreCull;
		Camera.onPreRender  += CameraPreRender;
		Camera.onPostRender += CameraPostRender;
	}
	
	protected override void OnDisable()
	{
		base.OnDisable();

		Camera.onPreCull    -= CameraPreCull;
		Camera.onPreRender  -= CameraPreRender;
		Camera.onPostRender -= CameraPostRender;
	}

	protected override int BeginQuads()
	{
		SgtHelper.BeginRandomSeed(Seed);
		
		return StarCount;
	}

	protected virtual void NextQuad(ref SgtStaticStar quad, int starIndex)
	{
		var position = Random.insideUnitSphere;

		position.y *= Symmetry;

		quad.Variant  = Random.Range(int.MinValue, int.MaxValue);
		quad.Radius   = Mathf.Lerp(StarRadiusMin, StarRadiusMax, Mathf.Pow(Random.value, StarRadiusBias));
		quad.Position = position.normalized * Radius;

		if (StarColors != null)
		{
			quad.Color = StarColors.Evaluate(Random.value);
		}
		else
		{
			quad.Color = Color.white;
		}
	}

	protected override void EndQuads()
	{
		SgtHelper.EndRandomSeed();
	}
	
	protected override void BuildMesh(Mesh mesh, int starIndex, int starCount)
	{
		var positions = new Vector3[starCount * 4];
		var colors    = new Color[starCount * 4];
		var coords1   = new Vector2[starCount * 4];
		var indices   = new int[starCount * 6];
		var minMaxSet = false;
		var min       = default(Vector3);
		var max       = default(Vector3);
		
		for (var i = 0; i < starCount; i++)
		{
			NextQuad(ref SgtStaticStar.Temp, starIndex + i);

			var offV     = i * 4;
			var offI     = i * 6;
			var radius   = SgtStaticStar.Temp.Radius;
			var uv       = tempCoords[SgtHelper.Mod(SgtStaticStar.Temp.Variant, tempCoords.Count)];
			var rotation = Quaternion.FromToRotation(Vector3.back, SgtStaticStar.Temp.Position);
			var up       = rotation * Vector3.up    * radius;
			var right    = rotation * Vector3.right * radius;

			ExpandBounds(ref minMaxSet, ref min, ref max, SgtStaticStar.Temp.Position, radius);
			
			positions[offV + 0] = SgtStaticStar.Temp.Position - up - right;
			positions[offV + 1] = SgtStaticStar.Temp.Position - up + right;
			positions[offV + 2] = SgtStaticStar.Temp.Position + up - right;
			positions[offV + 3] = SgtStaticStar.Temp.Position + up + right;

			colors[offV + 0] =
			colors[offV + 1] =
			colors[offV + 2] =
			colors[offV + 3] = SgtStaticStar.Temp.Color;
			
			coords1[offV + 0] = new Vector2(uv.x, uv.y);
			coords1[offV + 1] = new Vector2(uv.z, uv.y);
			coords1[offV + 2] = new Vector2(uv.x, uv.w);
			coords1[offV + 3] = new Vector2(uv.z, uv.w);
			
			indices[offI + 0] = offV + 0;
			indices[offI + 1] = offV + 1;
			indices[offI + 2] = offV + 2;
			indices[offI + 3] = offV + 3;
			indices[offI + 4] = offV + 2;
			indices[offI + 5] = offV + 1;
		}
		
		mesh.vertices  = positions;
		mesh.colors    = colors;
		mesh.uv        = coords1;
		mesh.triangles = indices;
		mesh.bounds    = SgtHelper.NewBoundsFromMinMax(min, max);
	}

	protected virtual void CameraPreCull(Camera camera)
	{
		if (Models != null)
		{
			for (var i = Models.Count - 1; i >= 0; i--)
			{
				var model = Models[i];

				if (model != null)
				{
					model.BeginSave();
					{
						model.transform.position = camera.transform.position;
					}
					model.EndSave(camera);
				}
			}
		}
	}

	protected void CameraPreRender(Camera camera)
	{
		if (Models != null)
		{
			for (var i = Models.Count - 1; i >= 0; i--)
			{
				var model = Models[i];

				if (model != null)
				{
					model.Restore(camera);
				}
			}
		}
	}

	protected void CameraPostRender(Camera camera)
	{
		if (Models != null)
		{
			for (var i = Models.Count - 1; i >= 0; i--)
			{
				var model = Models[i];

				if (model != null)
				{
					model.Restore();
				}
			}
		}
	}
}