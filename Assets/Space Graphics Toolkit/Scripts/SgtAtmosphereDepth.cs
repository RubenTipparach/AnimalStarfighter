using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(SgtAtmosphereDepth))]
public class SgtAtmosphereDepth_Editor : SgtEditor<SgtAtmosphereDepth>
{
	protected override void OnInspector()
	{
		var updateTexture = false;
		var updateApply   = false;
		
		BeginError(Any(t => t.Atmosphere == null));
			DrawDefault("Atmosphere", ref updateApply);
		EndError();
		BeginError(Any(t => t.Width < 1));
			DrawDefault("Width", ref updateTexture);
		EndError();
		DrawDefault("Format", ref updateTexture);
		DrawDefault("HorizonColor", ref updateTexture);

		Separator();
		
		DrawDefault("InnerEase", ref updateTexture);
		DrawDefault("InnerColor", ref updateTexture);
		BeginError(Any(t => t.InnerColorPower < 1.0f));
			DrawDefault("InnerColorPower", ref updateTexture);
		EndError();
		BeginError(Any(t => t.InnerAlphaPower < 1.0f));
			DrawDefault("InnerAlphaPower", ref updateTexture);
		EndError();

		Separator();
		
		DrawDefault("OuterEase", ref updateTexture);
		DrawDefault("OuterColor", ref updateTexture);
		BeginError(Any(t => t.OuterColorPower < 1.0f));
			DrawDefault("OuterColorPower", ref updateTexture);
		EndError();
		BeginError(Any(t => t.OuterAlphaPower < 1.0f));
			DrawDefault("OuterAlphaPower", ref updateTexture);
		EndError();

		if (updateTexture == true) DirtyEach(t => t.UpdateTextures());
		if (updateApply   == true) DirtyEach(t => t.UpdateApply   ());
	}
}
#endif

[ExecuteInEditMode]
[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Atmosphere Depth")]
public class SgtAtmosphereDepth : MonoBehaviour
{
	[Tooltip("The atmosphere this texture will be applied to")]
	public SgtAtmosphere Atmosphere;

	[Tooltip("The resolution of the surface/space optical thickness transition in pixels")]
	public int Width = 256;
	
	[Tooltip("The texture format of the textures")]
	public TextureFormat Format = TextureFormat.ARGB32;
	
	[Tooltip("The horizon color for both textures")]
	public Color HorizonColor = Color.white;

	[Tooltip("The transition style between the surface and horizon")]
	public SgtEase.Type InnerEase = SgtEase.Type.Exponential;
	
	[Tooltip("The base color of the inner texture")]
	public Color InnerColor = new Color(0.15f, 0.54f, 1.0f);

	[Tooltip("The strength of the inner texture transition")]
	public float InnerColorPower = 2.0f;

	[Tooltip("The strength of the inner texture transition")]
	public float InnerAlphaPower = 2.0f;

	[Tooltip("The transition style between the sky and horizon")]
	public SgtEase.Type OuterEase = SgtEase.Type.Quadratic;

	[Tooltip("The base color of the outer texture")]
	public Color OuterColor = new Color(0.29f, 0.73f, 1.0f);

	[Tooltip("The strength of the outer texture transition")]
	public float OuterColorPower = 1.0f;

	[Tooltip("The strength of the outer texture transition")]
	public float OuterAlphaPower = 3.7f;
	
	[System.NonSerialized]
	private Texture2D generatedInnerTexture;
	
	[System.NonSerialized]
	private Texture2D generatedOuterTexture;

	[SerializeField]
	[HideInInspector]
	private bool startCalled;
	
	public Texture2D GeneratedInnerTexture
	{
		get
		{
			return generatedInnerTexture;
		}
	}

	public Texture2D GeneratedOuterTexture
	{
		get
		{
			return generatedOuterTexture;
		}
	}

	#if UNITY_EDITOR
	[ContextMenu("Export Inner Texture")]
	public void ExportInnerTexture()
	{
		var importer = SgtHelper.ExportTextureDialog(generatedOuterTexture, "Inner Depth");

		if (importer != null)
		{
			importer.textureType         = TextureImporterType.SingleChannel;
			importer.textureCompression  = TextureImporterCompression.Uncompressed;
			importer.alphaSource         = TextureImporterAlphaSource.FromInput;
			importer.wrapMode            = TextureWrapMode.Clamp;
			importer.filterMode          = FilterMode.Trilinear;
			importer.anisoLevel          = 16;
			importer.alphaIsTransparency = true;

			importer.SaveAndReimport();
		}
	}

	[ContextMenu("Export Outer Texture")]
	public void ExportOuterTexture()
	{
		var importer = SgtHelper.ExportTextureDialog(generatedOuterTexture, "Outer Depth");

		if (importer != null)
		{
			importer.textureType         = TextureImporterType.SingleChannel;
			importer.textureCompression  = TextureImporterCompression.Uncompressed;
			importer.alphaSource         = TextureImporterAlphaSource.FromInput;
			importer.wrapMode            = TextureWrapMode.Clamp;
			importer.filterMode          = FilterMode.Trilinear;
			importer.anisoLevel          = 16;
			importer.alphaIsTransparency = true;

			importer.SaveAndReimport();
		}
	}
	#endif

	[ContextMenu("Update Textures")]
	public void UpdateTextures()
	{
		if (Width > 0)
		{
			ValidateTexture(ref generatedInnerTexture, "Inner Depth (Generated)");
			ValidateTexture(ref generatedOuterTexture, "Outer Depth (Generated)");

			var color = Color.clear;
			var step  = 1.0f / (Width - 1);

			for (var x = 0; x < Width; x++)
			{
				var u = x * step;

				WriteTexture(generatedInnerTexture, u, x, InnerColor, InnerEase, InnerColorPower, InnerAlphaPower);
				WriteTexture(generatedOuterTexture, u, x, OuterColor, OuterEase, OuterColorPower, OuterAlphaPower);
			}
			
			generatedInnerTexture.Apply();
			generatedOuterTexture.Apply();
		}
	}

	private void ValidateTexture(ref Texture2D texture2D, string createName)
	{
		// Destroy if invalid
		if (texture2D != null)
		{
			if (texture2D.width != Width || texture2D.height != 1 || texture2D.format != Format)
			{
				texture2D = SgtHelper.Destroy(texture2D);
			}
		}

		// Create?
		if (texture2D == null)
		{
			texture2D = SgtHelper.CreateTempTexture2D(createName, Width, 1, Format);

			texture2D.wrapMode = TextureWrapMode.Clamp;

			UpdateApply();
		}
	}

	private void WriteTexture(Texture2D texture2D, float u, int x, Color baseColor, SgtEase.Type ease, float colorPower, float alphaPower)
	{
		var colorU = Mathf.Pow(u, colorPower); colorU = SgtEase.Evaluate(ease, colorU);
		var alphaU = Mathf.Pow(u, alphaPower); alphaU = SgtEase.Evaluate(ease, alphaU);
		
		var color = Color.Lerp(baseColor, HorizonColor, colorU);

		color.a = alphaU;
		
		texture2D.SetPixel(x, 0, color);
	}
	
	[ContextMenu("Update Apply")]
	public void UpdateApply()
	{
		if (Atmosphere != null)
		{
			if (generatedInnerTexture != null)
			{
				if (Atmosphere.InnerDepthTex != generatedInnerTexture)
				{
					Atmosphere.InnerDepthTex = generatedInnerTexture;

					Atmosphere.UpdateDepthTex();
				}
			}

			if (generatedOuterTexture != null)
			{
				if (Atmosphere.OuterDepthTex != generatedOuterTexture)
				{
					Atmosphere.OuterDepthTex = generatedOuterTexture;

					Atmosphere.UpdateDepthTex();
				}
			}
		}
	}

	protected virtual void OnEnable()
	{
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

			if (Atmosphere == null)
			{
				Atmosphere = GetComponent<SgtAtmosphere>();
			}

			CheckUpdateCalls();
		}
	}

	protected virtual void OnDestroy()
	{
		SgtHelper.Destroy(generatedInnerTexture);
		SgtHelper.Destroy(generatedOuterTexture);
	}

	private void CheckUpdateCalls()
	{
		if (generatedInnerTexture == null || generatedOuterTexture == null)
		{
			UpdateTextures();
		}

		UpdateApply();
	}
}