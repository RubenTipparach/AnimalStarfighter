using UnityEngine;
using UnityEngine.Serialization;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(SgtRing))]
public class SgtRing_Editor : SgtEditor<SgtRing>
{
	protected override void OnInspector()
	{
		var updateMaterial = false;
		var updateModels   = false;
		
		DrawDefault("Color", ref updateMaterial);
		BeginError(Any(t => t.Brightness < 0.0f));
			DrawDefault("Brightness", ref updateMaterial);
		EndError();
		DrawDefault("RenderQueue", ref updateMaterial);
		DrawDefault("RenderQueueOffset", ref updateMaterial);

		Separator();

		BeginError(Any(t => t.MainTex == null));
			DrawDefault("MainTex", ref updateMaterial);
		EndError();
		
		BeginError(Any(t => t.Segments < 1));
			DrawDefault("Segments", ref updateModels, ref updateModels);
		EndError();
		BeginError(Any(t => t.Mesh == null));
			DrawDefault("Mesh", ref updateModels);
		EndError();

		Separator();
		
		DrawDefault("Detail", ref updateMaterial);

		if (Any(t => t.Detail == true))
		{
			BeginIndent();
				BeginError(Any(t => t.DetailTex == null));
					DrawDefault("DetailTex", ref updateMaterial);
				EndError();
				BeginError(Any(t => t.DetailScaleX < 0.0f));
					DrawDefault("DetailScaleX", ref updateMaterial);
				EndError();
				BeginError(Any(t => t.DetailScaleY < 1));
					DrawDefault("DetailScaleY", ref updateMaterial);
				EndError();
				DrawDefault("DetailOffset", ref updateMaterial);
				DrawDefault("DetailSpeed", ref updateMaterial);
				DrawDefault("DetailTwist", ref updateMaterial);
				BeginError(Any(t => t.DetailTwistBias < 1.0f));
					DrawDefault("DetailTwistBias", ref updateMaterial);
				EndError();
			EndIndent();
		}

		Separator();

		DrawDefault("Fade", ref updateMaterial);

		if (Any(t => t.Fade == true))
		{
			BeginIndent();
				BeginError(Any(t => t.FadeTex == null));
					DrawDefault("FadeTex", ref updateMaterial);
				EndError();
				BeginError(Any(t => t.FadeDistance <= 0.0f));
					DrawDefault("FadeDistance", ref updateMaterial);
				EndError();
			EndIndent();
		}

		Separator();

		DrawDefault("Lit", ref updateMaterial);
		
		if (Any(t => t.Lit == true))
		{
			BeginIndent();
				BeginError(Any(t => t.LightingTex == null));
					DrawDefault("LightingTex", ref updateMaterial);
				EndError();
				DrawDefault("Scattering", ref updateMaterial);

				if (Any(t => t.Scattering == true))
				{
					BeginIndent();
						BeginError(Any(t => t.ScatteringMie <= 0.0f));
							DrawDefault("ScatteringMie", ref updateMaterial);
						EndError();
						DrawDefault("ScatteringStrength"); // Updated in LateUpdate
					EndIndent();
				}

				BeginError(Any(t => t.Lights != null && (t.Lights.Count == 0 || t.Lights.Exists(l => l == null))));
					DrawDefault("Lights", ref updateMaterial);
				EndError();
				BeginError(Any(t => t.Shadows != null && t.Shadows.Exists(s => s == null)));
					DrawDefault("Shadows", ref updateMaterial);
				EndError();
			EndIndent();
		}

		if (Any(t => t.Mesh == null && t.GetComponent<SgtRingMesh>() == null))
		{
			Separator();

			if (Button("Add Mesh") == true)
			{
				Each(t => SgtHelper.GetOrAddComponent<SgtRingMesh>(t.gameObject));
			}
		}

		if (Any(t => t.Fade == true && t.FadeTex == null && t.GetComponent<SgtRingFade>() == null))
		{
			Separator();

			if (Button("Add Fade") == true)
			{
				Each(t => SgtHelper.GetOrAddComponent<SgtRingFade>(t.gameObject));
			}
		}

		if (Any(t => t.Lit == true && t.LightingTex == null && t.GetComponent<SgtRingLighting>() == null))
		{
			Separator();

			if (Button("Add Lighting") == true)
			{
				Each(t => SgtHelper.GetOrAddComponent<SgtRingLighting>(t.gameObject));
			}
		}

		if (updateMaterial == true) DirtyEach(t => t.UpdateMaterial());
		if (updateModels   == true) DirtyEach(t => t.UpdateModels  ());
	}
}
#endif

[ExecuteInEditMode]
[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Ring")]
public class SgtRing : MonoBehaviour
{
	// All currently active and enabled rings
	public static List<SgtRing> AllRings = new List<SgtRing>();
	
	[Tooltip("The color tint")]
	public Color Color = Color.white;

	[Tooltip("The Color.rgb values are multiplied by this")]
	public float Brightness = 1.0f;
	
	[Tooltip("The render queue group for this ring")]
	public SgtRenderQueue RenderQueue = SgtRenderQueue.Transparent;

	[Tooltip("The render queue offset for this ring")]
	public int RenderQueueOffset;
	
	[Tooltip("The texture applied to the ring (left side = inside, right side = outside)")]
	public Texture MainTex;

	[Tooltip("The mesh applied to each ring model")]
	public Mesh Mesh;

	[Tooltip("The amount of segments this ring is split into")]
	[FormerlySerializedAs("SegmentCount")]
	public int Segments = 8;

	[Tooltip("Should the ring have a detail texture?")]
	public bool Detail;

	[Tooltip("The detail texture applied to the ring")]
	public Texture DetailTex;

	[Tooltip("The detail texture horizontal tiling")]
	public float DetailScaleX = 1.0f;

	[Tooltip("The detail texture vertical tiling")]
	public int DetailScaleY = 1;

	[Tooltip("The UV offset of the detail texture")]
	public Vector2 DetailOffset;
	
	[Tooltip("The scroll speed of the detail texture UV offset")]
	public Vector2 DetailSpeed;

	[Tooltip("The amount the detail texture is twisted around the ring")]
	public float DetailTwist;

	[Tooltip("The amount the twisting is pushed to the outer edge")]
	public float DetailTwistBias = 1.0f;

	[Tooltip("Fade out as the camera approaches?")]
	public bool Fade;

	[Tooltip("The lookup table used to calculate the fade")]
	public Texture FadeTex;
	
	[Tooltip("The distance the fading begins from in world space")]
	public float FadeDistance = 1.0f;
	
	[Tooltip("Should light scatter through the rings?")]
	public bool Scattering;
	
	[Tooltip("The sharpness of the front scattered light")]
	public float ScatteringMie = 8.0f;
	
	[Tooltip("The scattering brightness multiplier")]
	public float ScatteringStrength = 25.0f;
	
	[Tooltip("Does this receive light?")]
	public bool Lit;

	[Tooltip("The lookup table used to calculate the lighting")]
	public Texture LightingTex;

	[Tooltip("The lights shining on this ring (max = 1 light, 2 scatter)")]
	public List<Light> Lights;

	[Tooltip("The shadows casting on this ring (max = 2)")]
	public List<SgtShadow> Shadows;
	
	// The models used to render the full ring
	[FormerlySerializedAs("Segments")]
	public List<SgtRingModel> Models;

	// The material applied to all models
	[System.NonSerialized]
	public Material Material;
	
	[SerializeField]
	private bool startCalled;

	[System.NonSerialized]
	private bool updateMaterialCalled;
	
	[System.NonSerialized]
	private bool updateModelsCalled;
	
	protected virtual string ShaderName
	{
		get
		{
			return SgtHelper.ShaderNamePrefix + "Ring";
		}
	}
	
	public virtual void UpdateMainTex()
	{
		if (Material != null)
		{
			Material.SetTexture("_MainTex", MainTex);
		}
	}
	
	public virtual void UpdateFadeTex()
	{
		if (Material != null)
		{
			Material.SetTexture("_FadeTex", FadeTex);
		}
	}
	
	public virtual void UpdateLightingTex()
	{
		if (Material != null)
		{
			Material.SetTexture("_LightingTex", LightingTex);
		}
	}
	
	[ContextMenu("Update Material")]
	public virtual void UpdateMaterial()
	{
		updateMaterialCalled = true;

		if (Material == null)
		{
			Material = SgtHelper.CreateTempMaterial("Ring (Generated)", ShaderName);

			if (Models != null)
			{
				for (var i = Models.Count - 1; i >= 0; i--)
				{
					var model = Models[i];

					if (model != null)
					{
						model.SetMaterial(Material);
					}
				}
			}
		}

		var color       = SgtHelper.Brighten(Color, Brightness);
		var renderQueue = (int)RenderQueue + RenderQueueOffset;
		
		Material.renderQueue = renderQueue;
		
		Material.SetColor("_Color", color);
		Material.SetTexture("_MainTex", MainTex);
		
		if (Detail == true)
		{
			SgtHelper.EnableKeyword("SGT_B", Material); // Detail

			Material.SetTexture("_DetailTex", DetailTex);
			Material.SetVector("_DetailScale", new Vector2(DetailScaleX, DetailScaleY));
			Material.SetFloat("_DetailTwist", DetailTwist);
			Material.SetFloat("_DetailTwistBias", DetailTwistBias);
		}
		else
		{
			SgtHelper.DisableKeyword("SGT_B", Material); // Detail
		}

		if (Fade == true)
		{
			SgtHelper.EnableKeyword("SGT_C", Material); // Fade

			Material.SetTexture("_FadeTex", FadeTex);
			Material.SetFloat("_FadeDistanceRecip", SgtHelper.Reciprocal(FadeDistance));
		}
		else
		{
			SgtHelper.DisableKeyword("SGT_C", Material); // Fade
		}

		if (Lit == true)
		{
			Material.SetTexture("_LightingTex", LightingTex);
		}

		if (Scattering == true)
		{
			SgtHelper.EnableKeyword("SGT_A", Material); // Scattering
			
			Material.SetFloat("_ScatteringMie", ScatteringMie * ScatteringMie);
		}
		else
		{
			SgtHelper.DisableKeyword("SGT_A", Material); // Scattering
		}
		
		UpdateMaterialNonSerialized();
	}
	
	[ContextMenu("Update Mesh")]
	public void UpdateMesh()
	{
		if (Models != null)
		{
			for (var i = Models.Count - 1; i >= 0; i--)
			{
				var model = Models[i];

				if (model != null)
				{
					model.SetMesh(Mesh);
				}
			}
		}
	}

	[ContextMenu("Update Models")]
	public void UpdateModels()
	{
		updateModelsCalled = true;
		
		var angleStep = SgtHelper.Divide(360.0f, Segments);
		
		for (var i = 0; i < Segments; i++)
		{
			var model    = GetOrAddModel(i);
			var angle    = angleStep * i;
			var rotation = Quaternion.Euler(0.0f, angle, 0.0f);

			model.SetMesh(Mesh);
			model.SetMaterial(Material);
			model.SetRotation(rotation);
		}

		// Remove any excess
		if (Models != null)
		{
			var min = Mathf.Max(0, Segments);

			for (var i = Models.Count - 1; i >= min; i--)
			{
				SgtRingModel.Pool(Models[i]);

				Models.RemoveAt(i);
			}
		}
	}

	public static SgtRing CreateRing(int layer = 0, Transform parent = null)
	{
		return CreateRing(layer, parent, Vector3.zero, Quaternion.identity, Vector3.one);
	}

	public static SgtRing CreateRing(int layer, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
	{
		var gameObject = SgtHelper.CreateGameObject("Ring", layer, parent, localPosition, localRotation, localScale);
		var ring       = gameObject.AddComponent<SgtRing>();

		return ring;
	}

#if UNITY_EDITOR
	[MenuItem(SgtHelper.GameObjectMenuPrefix + "Ring", false, 10)]
	public static void CreateRingMenuItem()
	{
		var parent = SgtHelper.GetSelectedParent();
		var ring   = CreateRing(parent != null ? parent.gameObject.layer : 0, parent);

		SgtHelper.SelectAndPing(ring);
	}
#endif
	
	protected virtual void OnEnable()
	{
		AllRings.Add(this);

		Camera.onPreRender += CameraPreRender;

		if (Models != null)
		{
			for (var i = Models.Count - 1; i >= 0; i--)
			{
				var model = Models[i];

				if (model != null)
				{
					model.gameObject.SetActive(true);
				}
			}
		}

		if (startCalled == true)
		{
			CheckUpdateCalls();
		}
	}

	protected virtual void Start()
	{
		if (startCalled == false)
		{
			startCalled = true;
			
			CheckUpdateCalls();
		}
	}

	protected virtual void LateUpdate()
	{
		// The lights and shadows may have moved, so write them
		if (Material != null)
		{
			SgtHelper.SetTempMaterial(Material);

			SgtHelper.WriteLights(Lit, Lights, 2, transform.position, null, null, SgtHelper.Brighten(Color, Brightness), ScatteringStrength);
			SgtHelper.WriteShadows(Shadows, 2);

			if (Detail == true)
			{
				if (Application.isPlaying == true)
				{
					DetailOffset += DetailSpeed * Time.deltaTime;
				}

				Material.SetVector("_DetailOffset", DetailOffset);
			}
		}
	}

	protected virtual void OnDisable()
	{
		AllRings.Remove(this);

		Camera.onPreRender -= CameraPreRender;

		if (Models != null)
		{
			for (var i = Models.Count - 1; i >= 0; i--)
			{
				var model = Models[i];

				if (model != null)
				{
					model.gameObject.SetActive(false);
				}
			}
		}
	}

	protected virtual void OnDestroy()
	{
		if (Models != null)
		{
			for (var i = Models.Count - 1; i >= 0; i--)
			{
				SgtRingModel.MarkForDestruction(Models[i]);
			}
		}
		
		SgtHelper.Destroy(Material);
	}
	
	private void UpdateMaterialNonSerialized()
	{
		SgtHelper.SetTempMaterial(Material);

		SgtHelper.WriteShadowsNonSerialized(Shadows, 2);
	}

	private void CameraPreRender(Camera camera)
	{
		if (Material != null)
		{
			UpdateMaterialNonSerialized();
		}
	}

	private SgtRingModel GetOrAddModel(int index)
	{
		var model = default(SgtRingModel);

		if (Models == null)
		{
			Models = new List<SgtRingModel>();
		}

		if (index < Models.Count)
		{
			model = Models[index];

			if (model == null)
			{
				model = SgtRingModel.Create(this);

				Models[index] = model;
			}
		}
		else
		{
			model = SgtRingModel.Create(this);

			Models.Add(model);
		}

		return model;
	}

	private void CheckUpdateCalls()
	{
		if (updateMaterialCalled == false)
		{
			UpdateMaterial();
		}
		
		if (updateModelsCalled == false)
		{
			UpdateModels();
		}
	}
}