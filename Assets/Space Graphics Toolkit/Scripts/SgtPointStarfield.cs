using UnityEngine;
using UnityEngine.Serialization;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;

public class SgtPointStarfield_Editor<T> : SgtQuads_Editor<T>
	where T : SgtPointStarfield
{
	protected void DrawPointMaterial(ref bool updateMaterial)
	{
		DrawDefault("Softness", ref updateMaterial);
		
		if (Any(t => t.Softness > 0.0f))
		{
			foreach (var camera in Camera.allCameras)
			{
				if (SgtHelper.Enabled(camera) == true && camera.depthTextureMode == DepthTextureMode.None)
				{
					if ((camera.cullingMask & (1 << Target.gameObject.layer)) != 0)
					{
						if (HelpButton("You have enabled soft particles, but the '" + camera.name + "' camera does not write depth textures.", MessageType.Error, "Fix", 50.0f) == true)
						{
							var dtm = SgtHelper.GetOrAddComponent<SgtDepthTextureMode>(camera.gameObject);

							dtm.DepthMode = DepthTextureMode.Depth;

							dtm.UpdateDepthMode();

							Selection.activeObject = dtm;
						}
					}
				}
			}
		}

		Separator();
		
		DrawDefault("FollowCameras", ref updateMaterial);

		if (Any(t => t.FollowCameras == true && t.Wrap == true))
		{
			EditorGUILayout.HelpBox("This setting shouldn't be used with 'Wrap'", MessageType.Warning);
		}

		DrawDefault("Wrap", ref updateMaterial);
		
		if (Any(t => t.Wrap == true))
		{
			BeginIndent();
				BeginError(Any(t => t.WrapSize.x == 0.0f || t.WrapSize.y == 0.0f || t.WrapSize.z == 0.0f));
					DrawDefault("WrapSize", ref updateMaterial);
				EndError();
			EndIndent();
		}

		DrawDefault("FadeNear", ref updateMaterial);
		
		if (Any(t => t.FadeNear == true))
		{
			BeginIndent();
				BeginError(Any(t => t.FadeNearTex == null));
					DrawDefault("FadeNearTex", ref updateMaterial);
				EndError();
				BeginError(Any(t => t.FadeNearRadius < 0.0f));
					DrawDefault("FadeNearRadius", ref updateMaterial);
				EndError();
				BeginError(Any(t => t.FadeNearThickness <= 0.0f));
					DrawDefault("FadeNearThickness", ref updateMaterial);
				EndError();
			EndIndent();
		}
		
		DrawDefault("FadeFar", ref updateMaterial);
		
		if (Any(t => t.FadeFar == true))
		{
			BeginIndent();
				BeginError(Any(t => t.FadeFarTex == null));
					DrawDefault("FadeFarTex", ref updateMaterial);
				EndError();
				BeginError(Any(t => t.FadeFarRadius < 0.0f));
					DrawDefault("FadeFarRadius", ref updateMaterial);
				EndError();
				BeginError(Any(t => t.FadeFarThickness <= 0.0f));
					DrawDefault("FadeFarThickness", ref updateMaterial);
				EndError();
			EndIndent();
		}
		
		DrawDefault("Stretch", ref updateMaterial);
		
		if (Any(t => t.Stretch == true))
		{
			BeginIndent();
				DrawDefault("StretchVector", ref updateMaterial);
				BeginError(Any(t => t.StretchScale < 0.0f));
					DrawDefault("StretchScale", ref updateMaterial);
				EndError();
			EndIndent();
		}
		
		DrawDefault("Pulse", ref updateMaterial);

		if (Any(t => t.Pulse == true))
		{
			BeginIndent();
				DrawDefault("PulseOffset");
				BeginError(Any(t => t.PulseSpeed == 0.0f));
					DrawDefault("PulseSpeed");
				EndError();
			EndIndent();
		}

		if (Any(t => t.FadeNear == true && t.FadeNearTex == null && t.GetComponent<SgtStarfieldFadeNear>() == null))
		{
			Separator();

			if (Button("Add Fade Near") == true)
			{
				Each(t => SgtHelper.GetOrAddComponent<SgtStarfieldFadeNear>(t.gameObject));
			}
		}

		if (Any(t => t.FadeFar == true && t.FadeFarTex == null && t.GetComponent<SgtStarfieldFadeFar>() == null))
		{
			Separator();

			if (Button("Add Fade Far") == true)
			{
				Each(t => SgtHelper.GetOrAddComponent<SgtStarfieldFadeFar>(t.gameObject));
			}
		}
	}
}
#endif

// This is the base class for all starfields that store star corner vertices the same point/location
// and are stretched out in the vertex shader, allowing billboarding in view space, and dynamic resizing
public abstract class SgtPointStarfield : SgtQuads
{
	[Tooltip("Should the stars automatically be placed on top of the currently rendering camera?")]
	[FormerlySerializedAs("FollowObservers")]
	public bool FollowCameras;

	[Tooltip("Should the stars fade out if they're intersecting solid geometry?")]
	[Range(0.0f, 1000.0f)]
	public float Softness;
	
	[Tooltip("Should the stars wrap around the camera if they get too far away?")]
	public bool Wrap;

	[Tooltip("The size of the wrapped area")]
	public Vector3 WrapSize = Vector3.one;

	[Tooltip("Should the stars stretch if an observer moves?")]
	[FormerlySerializedAs("StretchToObservers")]
	public bool Stretch;
	
	[Tooltip("The vector of the stretching")]
	public Vector3 StretchVector;

	[Tooltip("The scale of the stretching relative to the velocity")]
	public float StretchScale = 1.0f;

	[Tooltip("Should the stars fade out when the camera gets near?")]
	public bool FadeNear;

	[Tooltip("The lookup table used to calculate the fading amount based on the distance")]
	public Texture FadeNearTex;

	[Tooltip("The radius of the fading effect in world coordinates")]
	public float FadeNearRadius = 2.0f;

	[Tooltip("The thickness of the fading effect in local coordinates")]
	public float FadeNearThickness = 2.0f;

	[Tooltip("Should the stars fade out when the camera gets too far away?")]
	public bool FadeFar;

	[Tooltip("The lookup table used to calculate the fading amount based on the distance")]
	public Texture FadeFarTex;

	[Tooltip("The radius of the fading effect in world coordinates")]
	public float FadeFarRadius = 2.0f;

	[Tooltip("The thickness of the fading effect in world coordinates")]
	public float FadeFarThickness = 2.0f;
	
	[Tooltip("Should the stars pulse in size over time?")]
	[FormerlySerializedAs("AllowPulse")]
	public bool Pulse;

	[Tooltip("The amount of seconds this starfield has been animating")]
	[FormerlySerializedAs("Age")]
	public float PulseOffset;

	[Tooltip("The animation speed of this starfield")]
	[FormerlySerializedAs("TimeScale")]
	public float PulseSpeed = 1.0f;
	
	protected override string ShaderName
	{
		get
		{
			return SgtHelper.ShaderNamePrefix + "Starfield";
		}
	}

	public void UpdateFadeFarTex()
	{
		if (Material != null)
		{
			Material.SetTexture("_FadeFarTex", FadeFarTex);
		}
	}

	public void UpdateFadeNearTex()
	{
		if (Material != null)
		{
			Material.SetTexture("_FadeNearTex", FadeNearTex);
		}
	}

	public SgtCustomStarfield MakeEditableCopy(int layer, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
	{
#if UNITY_EDITOR
		SgtHelper.BeginUndo("Create Editable Starfield Copy");
#endif
		var gameObject      = SgtHelper.CreateGameObject("Editable Starfield Copy", layer, parent, localPosition, localRotation, localScale);
		var customStarfield = SgtHelper.AddComponent<SgtCustomStarfield>(gameObject, false);
		var quads           = new List<SgtPointStar>();
		var starCount       = BeginQuads();

		for (var i = 0; i < starCount; i++)
		{
			var quad = SgtClassPool<SgtPointStar>.Pop() ?? new SgtPointStar();

			NextQuad(ref quad, i);

			quads.Add(quad);
		}

		EndQuads();

		// Copy common settings
		customStarfield.Color             = Color;
		customStarfield.Brightness        = Brightness;
		customStarfield.MainTex           = MainTex;
		customStarfield.Layout            = Layout;
		customStarfield.LayoutColumns     = LayoutColumns;
		customStarfield.LayoutRows        = LayoutRows;
		customStarfield.RenderQueue       = RenderQueue;
		customStarfield.RenderQueueOffset = RenderQueueOffset;
		customStarfield.FollowCameras     = FollowCameras;
		customStarfield.Softness          = Softness;
		customStarfield.Wrap              = Wrap;
		customStarfield.WrapSize          = WrapSize;
		customStarfield.Stretch           = Stretch;
		customStarfield.StretchVector     = StretchVector;
		customStarfield.StretchScale      = StretchScale;
		customStarfield.FadeNear          = FadeNear;
		customStarfield.FadeNearRadius    = FadeNearRadius;
		customStarfield.FadeNearThickness = FadeNearThickness;
		customStarfield.FadeNearTex       = FadeNearTex;
		customStarfield.FadeFar           = FadeFar;
		customStarfield.FadeFarRadius     = FadeFarRadius;
		customStarfield.FadeFarThickness  = FadeFarThickness;
		customStarfield.FadeFarTex        = FadeFarTex;
		customStarfield.Pulse             = Pulse;
		customStarfield.PulseOffset       = PulseOffset;
		customStarfield.PulseSpeed        = PulseSpeed;

		// Copy custom settings
		customStarfield.Stars = quads;

		// Update
		customStarfield.UpdateMaterial();
		customStarfield.UpdateMeshesAndModels();

		return customStarfield;
	}

	public SgtCustomStarfield MakeEditableCopy(int layer = 0, Transform parent = null)
	{
		return MakeEditableCopy(layer, parent, Vector3.zero, Quaternion.identity, Vector3.one);
	}

#if UNITY_EDITOR
	[ContextMenu("Make Editable Copy")]
	public void MakeEditableCopyContext()
	{
		var customStarfield = MakeEditableCopy(gameObject.layer, transform.parent, transform.localPosition, transform.localRotation, transform.localScale);

		SgtHelper.SelectAndPing(customStarfield);
	}
#endif

	protected override void OnEnable()
	{
		Camera.onPreCull    += CameraPreCull;
		Camera.onPreRender  += CameraPreRender;
		Camera.onPostRender += CameraPostRender;
		
		base.OnEnable();
	}

	protected virtual void LateUpdate()
	{
		if (Application.isPlaying == true)
		{
			PulseOffset += Time.deltaTime * PulseSpeed;
		}

		if (Material != null)
		{
			if (Pulse == true)
			{
				Material.SetFloat("_PulseOffset", PulseOffset);
			}
		}
	}

	protected override void OnDisable()
	{
		Camera.onPreCull    -= CameraPreCull;
		Camera.onPreRender  -= CameraPreRender;
		Camera.onPostRender -= CameraPostRender;
		
		base.OnDisable();
	}

#if UNITY_EDITOR
	protected virtual void OnDrawGizmosSelected()
	{
		if (Wrap == true)
		{
			Gizmos.matrix = transform.localToWorldMatrix;

			Gizmos.DrawWireCube(Vector3.zero, WrapSize);
		}
	}
#endif
	
	protected override void BuildMaterial()
	{
		base.BuildMaterial();
		
		if (Wrap == true)
		{
			SgtHelper.EnableKeyword("SGT_B", Material); // Wrap

			Material.SetVector("_WrapSize", WrapSize);
		}
		else
		{
			SgtHelper.DisableKeyword("SGT_B", Material); // Wrap
		}

		if (Stretch == true)
		{
			SgtHelper.EnableKeyword("SGT_C", Material); // Stretch
		}
		else
		{
			SgtHelper.DisableKeyword("SGT_C", Material); // Stretch
		}

		if (FadeNear == true)
		{
			SgtHelper.EnableKeyword("SGT_D", Material); // Fade near

			Material.SetTexture("_FadeNearTex", FadeNearTex);
			Material.SetFloat("_FadeNearRadius", FadeNearRadius);
			Material.SetFloat("_FadeNearScale", SgtHelper.Reciprocal(FadeNearThickness));
		}
		else
		{
			SgtHelper.DisableKeyword("SGT_D", Material); // Fade near
		}

		if (FadeFar == true)
		{
			SgtHelper.EnableKeyword("SGT_E", Material); // Fade far

			Material.SetTexture("_FadeFarTex", FadeFarTex);
			Material.SetFloat("_FadeFarRadius", FadeFarRadius);
			Material.SetFloat("_FadeFarScale", SgtHelper.Reciprocal(FadeFarThickness));
		}
		else
		{
			SgtHelper.DisableKeyword("SGT_E", Material); // Fade far
		}

		if (Pulse == true)
		{
			SgtHelper.EnableKeyword("LIGHT_1", Material); // Pulse

			// This is also set in Update
			Material.SetFloat("_PulseOffset", PulseOffset);
		}
		else
		{
			SgtHelper.DisableKeyword("LIGHT_1", Material); // Pulse
		}

		if (Softness > 0.0f)
		{
			SgtHelper.EnableKeyword("LIGHT_2", Material); // Softness

			Material.SetFloat("_InvFade", SgtHelper.Reciprocal(Softness));
		}
		else
		{
			SgtHelper.DisableKeyword("LIGHT_2", Material); // Softness
		}
	}
	
	protected virtual void CameraPreCull(Camera camera)
	{
		if (FollowCameras == true || Wrap == true)
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
							if (FollowCameras == true)
							{
								model.transform.position = camera.transform.position;
							}
						}
						model.EndSave(camera);

						if (Wrap == true)
						{
							model.transform.position = camera.transform.position;
						}
					}
				}
			}
		}
	}

	protected abstract void NextQuad(ref SgtPointStar quad, int starIndex);
	
	protected override void BuildMesh(Mesh mesh, int starIndex, int starCount)
	{
		var positions = new Vector3[starCount * 4];
		var colors    = new Color[starCount * 4];
		var normals   = new Vector3[starCount * 4];
		var tangents  = new Vector4[starCount * 4];
		var coords1   = new Vector2[starCount * 4];
		var coords2   = new Vector2[starCount * 4];
		var indices   = new int[starCount * 6];
		var minMaxSet = false;
		var min       = default(Vector3);
		var max       = default(Vector3);
		
		for (var i = 0; i < starCount; i++)
		{
			NextQuad(ref SgtPointStar.Temp, starIndex + i);

			var offV     = i * 4;
			var offI     = i * 6;
			var position = SgtPointStar.Temp.Position;
			var radius   = SgtPointStar.Temp.Radius;
			var angle    = Mathf.Repeat(SgtPointStar.Temp.Angle / 180.0f, 2.0f) - 1.0f;
			var uv       = tempCoords[SgtHelper.Mod(SgtPointStar.Temp.Variant, tempCoords.Count)];
			
			ExpandBounds(ref minMaxSet, ref min, ref max, position, radius);
			
			positions[offV + 0] =
			positions[offV + 1] =
			positions[offV + 2] =
			positions[offV + 3] = position;

			colors[offV + 0] =
			colors[offV + 1] =
			colors[offV + 2] =
			colors[offV + 3] = SgtPointStar.Temp.Color;
			
			normals[offV + 0] = new Vector3(-1.0f,  1.0f, angle);
			normals[offV + 1] = new Vector3( 1.0f,  1.0f, angle);
			normals[offV + 2] = new Vector3(-1.0f, -1.0f, angle);
			normals[offV + 3] = new Vector3( 1.0f, -1.0f, angle);

			tangents[offV + 0] =
			tangents[offV + 1] =
			tangents[offV + 2] =
			tangents[offV + 3] = new Vector4(SgtPointStar.Temp.PulseOffset, SgtPointStar.Temp.PulseSpeed, SgtPointStar.Temp.PulseRange, 0.0f);
			
			coords1[offV + 0] = new Vector2(uv.x, uv.y);
			coords1[offV + 1] = new Vector2(uv.z, uv.y);
			coords1[offV + 2] = new Vector2(uv.x, uv.w);
			coords1[offV + 3] = new Vector2(uv.z, uv.w);
			
			coords2[offV + 0] = new Vector2(radius,  0.5f);
			coords2[offV + 1] = new Vector2(radius, -0.5f);
			coords2[offV + 2] = new Vector2(radius,  0.5f);
			coords2[offV + 3] = new Vector2(radius, -0.5f);

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
		mesh.bounds    = SgtHelper.NewBoundsFromMinMax(min, max);
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

		if (Material != null)
		{
			var velocity = StretchVector;
			var observer = SgtObserver.Find(camera);
				
			if (observer != null)
			{
				Material.SetFloat("_CameraRollAngle", observer.RollAngle * Mathf.Deg2Rad);
					
				velocity += observer.Velocity * StretchScale;
			}

			if (Stretch == true)
			{
				Material.SetVector("_StretchVector", velocity);
				Material.SetVector("_StretchDirection", velocity.normalized);
				Material.SetFloat("_StretchLength", velocity.magnitude);
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

		if (Material != null)
		{
			Material.SetFloat("_CameraRollAngle", 0.0f);
			
			Material.SetVector("_StretchVector", Vector3.zero);
			Material.SetVector("_StretchDirection", Vector3.zero);
			Material.SetFloat("_StretchLength", 0.0f);
		}
	}
}