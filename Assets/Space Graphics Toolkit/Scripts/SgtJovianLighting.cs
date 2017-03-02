using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(SgtJovianLighting))]
public class SgtJovianLighting_Editor : SgtEditor<SgtJovianLighting>
{
	protected override void OnInspector()
	{
		var updateTexture = false;
		var updateApply   = false;
		
		BeginError(Any(t => t.Jovian == null));
			DrawDefault("Jovian", ref updateApply);
		EndError();
		BeginError(Any(t => t.Width <= 1));
			DrawDefault("Width", ref updateTexture);
		EndError();
		DrawDefault("Format", ref updateTexture);
		
		Separator();
		
		DrawDefault("SunsetEase", ref updateTexture);
		BeginError(Any(t => t.SunsetStart >= t.SunsetEnd));
			DrawDefault("SunsetStart", ref updateTexture);
			DrawDefault("SunsetEnd", ref updateTexture);
		EndError();
		BeginError(Any(t => t.SunsetPowerR < 1.0f));
			DrawDefault("SunsetPowerR", ref updateTexture);
		EndError();
		BeginError(Any(t => t.SunsetPowerG < 1.0f));
			DrawDefault("SunsetPowerG", ref updateTexture);
		EndError();
		BeginError(Any(t => t.SunsetPowerB < 1.0f));
			DrawDefault("SunsetPowerB", ref updateTexture);
		EndError();

		if (updateTexture == true) DirtyEach(t => t.UpdateTextures());
		if (updateApply   == true) DirtyEach(t => t.UpdateApply   ());
	}
}
#endif

[ExecuteInEditMode]
[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Jovian Lighting")]
public class SgtJovianLighting : MonoBehaviour
{
	[Tooltip("The jovian this texture will be applied to")]
	public SgtJovian Jovian;

	[Tooltip("The resolution of the day/sunset/night color transition in pixels")]
	public int Width = 256;
	
	[Tooltip("The format of this texture")]
	public TextureFormat Format = TextureFormat.ARGB32;

	[Tooltip("The transition style between the day and night")]
	public SgtEase.Type SunsetEase = SgtEase.Type.Smoothstep;
	
	[Tooltip("The start point of the sunset (0 = dark side, 1 = light side)")]
	[Range(0.0f, 1.0f)]
	public float SunsetStart = 0.4f;

	[Tooltip("The end point of the sunset (0 = dark side, 1 = light side)")]
	[Range(0.0f, 1.0f)]
	public float SunsetEnd = 0.6f;

	[Tooltip("The power of the sunset red channel transition")]
	public float SunsetPowerR = 2.0f;
	
	[Tooltip("The power of the sunset green channel transition")]
	public float SunsetPowerG = 2.0f;
	
	[Tooltip("The power of the sunset blue channel transition")]
	public float SunsetPowerB = 2.0f;
	
	[System.NonSerialized]
	private Texture2D generatedTexture;

	[SerializeField]
	[HideInInspector]
	private bool startCalled;
	
	public Texture2D GeneratedTexture
	{
		get
		{
			return generatedTexture;
		}
	}

#if UNITY_EDITOR
	[ContextMenu("Export Texture")]
	public void ExportTexture()
	{
		var importer = SgtHelper.ExportTextureDialog(generatedTexture, "Jovian Lighting");

		if (importer != null)
		{
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
			// Destroy if invalid
			if (generatedTexture != null)
			{
				if (generatedTexture.width != Width || generatedTexture.height != 1 || generatedTexture.format != Format)
				{
					generatedTexture = SgtHelper.Destroy(generatedTexture);
				}
			}

			// Create?
			if (generatedTexture == null)
			{
				generatedTexture = SgtHelper.CreateTempTexture2D("Jovian Lighting (Generated)", Width, 1, Format);

				generatedTexture.wrapMode = TextureWrapMode.Clamp;

				UpdateApply();
			}
			
			var stepX = 1.0f / (Width  - 1);
			
			for (var x = 0; x < Width; x++)
			{
				var u = x * stepX;

				WriteTexture(u, x);
			}
			
			generatedTexture.Apply();
		}
	}
	
	private void WriteTexture(float u, int x)
	{
		var sunsetU = Mathf.InverseLerp(SunsetEnd, SunsetStart, u);
		var color   = default(Color);

		color.r = SgtEase.Evaluate(SunsetEase, 1.0f - Mathf.Pow(sunsetU, SunsetPowerR));
		color.g = SgtEase.Evaluate(SunsetEase, 1.0f - Mathf.Pow(sunsetU, SunsetPowerG));
		color.b = SgtEase.Evaluate(SunsetEase, 1.0f - Mathf.Pow(sunsetU, SunsetPowerB));
		color.a = 0.0f;
		
		generatedTexture.SetPixel(x, 0, color);
	}
	
	[ContextMenu("Update Apply")]
	public void UpdateApply()
	{
		if (Jovian != null)
		{
			Jovian.LightingTex = generatedTexture;

			Jovian.UpdateLightingTex();
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

			if (Jovian == null)
			{
				Jovian = GetComponent<SgtJovian>();
			}

			CheckUpdateCalls();
		}
	}

	protected virtual void OnDestroy()
	{
		SgtHelper.Destroy(generatedTexture);
	}

	private void CheckUpdateCalls()
	{
		if (generatedTexture == null)
		{
			UpdateTextures();
		}

		UpdateApply();
	}
}