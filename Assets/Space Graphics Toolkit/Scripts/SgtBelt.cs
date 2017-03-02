using UnityEngine;
using UnityEngine.Serialization;
using System.Collections.Generic;

#if UNITY_EDITOR
public abstract class SgtBelt_Editor<T> : SgtQuads_Editor<T>
	where T : SgtBelt
{
	protected override void DrawMaterial(ref bool updateMaterial)
	{
		DrawDefault("Color", ref updateMaterial);
		BeginError(Any(t => t.Brightness < 0.0f));
			DrawDefault("Brightness", ref updateMaterial);
		EndError();
		DrawDefault("RenderQueue", ref updateMaterial);
		DrawDefault("RenderQueueOffset", ref updateMaterial);
		DrawDefault("OrbitOffset"); // Updated automatically
		DrawDefault("OrbitSpeed"); // Updated automatically
	}

	protected void DrawLighting(ref bool updateMaterial)
	{
		DrawDefault("Lit", ref updateMaterial);
		
		if (Any(t => t.Lit == true))
		{
			BeginIndent();
				BeginError(Any(t => t.LightingTex == null));
					DrawDefault("LightingTex", ref updateMaterial);
				EndError();
				BeginError(Any(t => t.Lights != null && (t.Lights.Count == 0 || t.Lights.Exists(l => l == null))));
					DrawDefault("Lights", ref updateMaterial);
				EndError();
				BeginError(Any(t => t.Shadows != null && t.Shadows.Exists(s => s == null)));
					DrawDefault("Shadows", ref updateMaterial);
				EndError();
			EndIndent();
		}

		if (Any(t => t.Lit == true && t.LightingTex == null && t.GetComponent<SgtBeltLighting>() == null))
		{
			Separator();

			if (Button("Add Lighting") == true)
			{
				Each(t => SgtHelper.GetOrAddComponent<SgtBeltLighting>(t.gameObject));
			}
		}
	}

	protected override void DrawAtlas(ref bool updateMaterial, ref bool updateMeshesAndModels)
	{
		BeginError(Any(t => t.MainTex == null));
			DrawDefault("MainTex", ref updateMaterial);
		EndError();
		BeginError(Any(t => t.HeightTex == null));
			DrawDefault("HeightTex", ref updateMaterial);
		EndError();
		DrawDefault("Layout", ref updateMeshesAndModels);
		
		BeginIndent();
			if (Any(t => t.Layout == SgtQuadsLayoutType.Grid))
			{
				BeginError(Any(t => t.LayoutColumns <= 0));
					DrawDefault("LayoutColumns", ref updateMeshesAndModels);
				EndError();
				BeginError(Any(t => t.LayoutRows <= 0));
					DrawDefault("LayoutRows", ref updateMeshesAndModels);
				EndError();
			}

			if (Any(t => t.Layout == SgtQuadsLayoutType.Custom))
			{
				DrawDefault("Rects", ref updateMeshesAndModels);
			}
		EndIndent();
	}
}
#endif

public abstract class SgtBelt : SgtQuads
{
	[Tooltip("The height texture of this belt")]
	public Texture HeightTex;
	
	[Tooltip("The amount of seconds this belt has been animating for")]
	[FormerlySerializedAs("Age")]
	public float OrbitOffset;

	[Tooltip("The animation speed of this belt")]
	[FormerlySerializedAs("TimeScale")]
	public float OrbitSpeed = 1.0f;

	[Tooltip("Does this receive light?")]
	public bool Lit;

	[Tooltip("The lookup table used to calculate the lighting")]
	public Texture LightingTex;

	[Tooltip("The lights shining on this belt (max = 1 light, 2 scatter)")]
	public List<Light> Lights;

	[Tooltip("The shadows casting on this belt (max = 2)")]
	public List<SgtShadow> Shadows;
	
	protected override string ShaderName
	{
		get
		{
			return SgtHelper.ShaderNamePrefix + "Belt";
		}
	}

	public SgtCustomBelt MakeEditableCopy(int layer = 0, Transform parent = null)
	{
		return MakeEditableCopy(layer, parent, Vector3.zero, Quaternion.identity, Vector3.one);
	}

	public SgtCustomBelt MakeEditableCopy(int layer, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
	{
#if UNITY_EDITOR
		SgtHelper.BeginUndo("Create Editable Belt Copy");
#endif
		var gameObject = SgtHelper.CreateGameObject("Editable Belt Copy", layer, parent, localPosition, localRotation, localScale);
		var customBelt = SgtHelper.AddComponent<SgtCustomBelt>(gameObject, false);
		var quads      = new List<SgtBeltAsteroid>();
		var quadCount  = BeginQuads();

		for (var i = 0; i < quadCount; i++)
		{
			var asteroid = SgtClassPool<SgtBeltAsteroid>.Pop() ?? new SgtBeltAsteroid();

			NextQuad(ref asteroid, i);

			quads.Add(asteroid);
		}

		EndQuads();
		
		// Copy common settings
		if (Lights != null)
		{
			customBelt.Lights = new List<Light>(Lights);
		}

		if (Shadows != null)
		{
			customBelt.Shadows = new List<SgtShadow>(Shadows);
		}
		
		customBelt.Color         = Color;
		customBelt.Brightness    = Brightness;
		customBelt.MainTex       = MainTex;
		customBelt.HeightTex     = HeightTex;
		customBelt.Layout        = Layout;
		customBelt.LayoutColumns = LayoutColumns;
		customBelt.LayoutRows    = LayoutRows;

		if (Rects != null)
		{
			customBelt.Rects = new List<Rect>(Rects);
		}

		customBelt.RenderQueue       = RenderQueue;
		customBelt.RenderQueueOffset = RenderQueueOffset;
		customBelt.OrbitOffset               = OrbitOffset;
		customBelt.OrbitSpeed         = OrbitSpeed;

		// Copy custom settings
		customBelt.Asteroids = quads;

		// Update
		customBelt.UpdateMaterial();
		customBelt.UpdateMeshesAndModels();

		return customBelt;
	}
	
	public virtual void UpdateLightingTex()
	{
		if (Material != null)
		{
			Material.SetTexture("_LightingTex", LightingTex);
		}
	}
	
#if UNITY_EDITOR
	[ContextMenu("Make Editable Copy")]
	public void MakeEditableCopyContext()
	{
		var customBelt = MakeEditableCopy(gameObject.layer, transform.parent, transform.localPosition, transform.localRotation, transform.localScale);

		SgtHelper.SelectAndPing(customBelt);
	}
#endif
	
	protected override void OnEnable()
	{
		Camera.onPreRender  += CameraPreRender;
		Camera.onPostRender += CameraPostRender;

		base.OnEnable();
	}
	
	protected virtual void LateUpdate()
	{
		if (Application.isPlaying == true)
		{
			OrbitOffset += Time.deltaTime * OrbitSpeed;
		}

		if (Material != null)
		{
			Material.SetFloat("_Age", OrbitOffset);
		}

		// The lights and shadows may have moved, so write them
		if (Material != null)
		{
			SgtHelper.SetTempMaterial(Material);

			SgtHelper.WriteLights(Lit, Lights, 2, transform.position, transform, null, SgtHelper.Brighten(Color, Brightness), 1.0f);
			SgtHelper.WriteShadows(Shadows, 2);
		}
	}
	
	protected override void OnDisable()
	{
		Camera.onPreRender  -= CameraPreRender;
		Camera.onPostRender -= CameraPostRender;

		base.OnDisable();
	}
	
	protected override void BuildMaterial()
	{
		base.BuildMaterial();
		
		Material.SetTexture("_HeightTex", HeightTex);
		Material.SetFloat("_Age", OrbitOffset);
		
		if (Lit == true)
		{
			Material.SetTexture("_LightingTex", LightingTex);
		}
	}
	
	protected abstract void NextQuad(ref SgtBeltAsteroid quad, int starIndex);

	protected override void BuildMesh(Mesh mesh, int asteroidIndex, int asteroidCount)
	{
		var positions = new Vector3[asteroidCount * 4];
		var colors    = new Color[asteroidCount * 4];
		var normals   = new Vector3[asteroidCount * 4];
		var tangents  = new Vector4[asteroidCount * 4];
		var coords1   = new Vector2[asteroidCount * 4];
		var coords2   = new Vector2[asteroidCount * 4];
		var indices   = new int[asteroidCount * 6];
		var maxWidth  = 0.0f;
		var maxHeight = 0.0f;
		
		for (var i = 0; i < asteroidCount; i++)
		{
			NextQuad(ref SgtBeltAsteroid.Temp, asteroidIndex + i);

			var offV     = i * 4;
			var offI     = i * 6;
			var radius   = SgtBeltAsteroid.Temp.Radius;
			var distance = SgtBeltAsteroid.Temp.OrbitDistance;
			var height   = SgtBeltAsteroid.Temp.Height;
			var uv       = tempCoords[SgtHelper.Mod(SgtBeltAsteroid.Temp.Variant, tempCoords.Count)];
			
			maxWidth  = Mathf.Max(maxWidth , distance + radius);
			maxHeight = Mathf.Max(maxHeight, height   + radius);
			
			positions[offV + 0] =
			positions[offV + 1] =
			positions[offV + 2] =
			positions[offV + 3] = new Vector3(SgtBeltAsteroid.Temp.OrbitAngle, distance, SgtBeltAsteroid.Temp.OrbitSpeed);

			colors[offV + 0] =
			colors[offV + 1] =
			colors[offV + 2] =
			colors[offV + 3] = SgtBeltAsteroid.Temp.Color;
			
			normals[offV + 0] = new Vector3(-1.0f,  1.0f, 0.0f);
			normals[offV + 1] = new Vector3( 1.0f,  1.0f, 0.0f);
			normals[offV + 2] = new Vector3(-1.0f, -1.0f, 0.0f);
			normals[offV + 3] = new Vector3( 1.0f, -1.0f, 0.0f);

			tangents[offV + 0] =
			tangents[offV + 1] =
			tangents[offV + 2] =
			tangents[offV + 3] = new Vector4(SgtBeltAsteroid.Temp.Angle / Mathf.PI, SgtBeltAsteroid.Temp.Spin / Mathf.PI, 0.0f, 0.0f);
			
			coords1[offV + 0] = new Vector2(uv.x, uv.y);
			coords1[offV + 1] = new Vector2(uv.z, uv.y);
			coords1[offV + 2] = new Vector2(uv.x, uv.w);
			coords1[offV + 3] = new Vector2(uv.z, uv.w);
					
			coords2[offV + 0] =
			coords2[offV + 1] =
			coords2[offV + 2] =
			coords2[offV + 3] = new Vector2(radius, height);

			indices[offI + 0] = offV + 0;
			indices[offI + 1] = offV + 1;
			indices[offI + 2] = offV + 2;
			indices[offI + 3] = offV + 3;
			indices[offI + 4] = offV + 2;
			indices[offI + 5] = offV + 1;
		}
		
		mesh.vertices  = positions;
		mesh.colors    = colors;
		mesh.normals   = normals;
		mesh.tangents  = tangents;
		mesh.uv        = coords1;
		mesh.uv2       = coords2;
		mesh.triangles = indices;
		mesh.bounds    = new Bounds(Vector3.zero, new Vector3(maxWidth * 2.0f, maxHeight * 2.0f, maxWidth * 2.0f));
	}
	
	private void ObserverPreRender(SgtObserver observer)
	{
		if (Material != null)
		{
			Material.SetFloat("_CameraRollAngle", observer.RollAngle * Mathf.Deg2Rad);
		}
	}

	protected void CameraPreRender(Camera camera)
	{
		if (Material != null)
		{
			var observer = SgtObserver.Find(camera);

			if (observer != null)
			{
				Material.SetFloat("_CameraRollAngle", observer.RollAngle * Mathf.Deg2Rad);
			}
		}
	}

	protected void CameraPostRender(Camera camera)
	{
		if (Material != null)
		{
			Material.SetFloat("_CameraRollAngle", 0.0f);
		}
	}
}