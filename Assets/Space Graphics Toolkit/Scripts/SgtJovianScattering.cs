using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(SgtJovianScattering))]
public class SgtJovianScattering_Editor : SgtEditor<SgtJovianScattering>
{
	protected override void OnInspector()
	{
		var updateApply   = false;
		var updateTexture = false;
		
		BeginError(Any(t => t.Jovian == null));
			DrawDefault("Jovian", ref updateApply);
		EndError();
		BeginError(Any(t => t.Width <= 1));
			DrawDefault("Width", ref updateTexture);
		EndError();
		BeginError(Any(t => t.Height <= 1));
			DrawDefault("Height", ref updateTexture);
		EndError();
		DrawDefault("Format", ref updateTexture);
		
		Separator();
		
		BeginError(Any(t => t.Mie < 1.0f));
			DrawDefault("Mie", ref updateTexture);
		EndError();
		BeginError(Any(t => t.Rayleigh < 0.0f));
			DrawDefault("Rayleigh", ref updateTexture);
		EndError();

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

		if (updateApply   == true) DirtyEach(t => t.UpdateApply  ());
		if (updateTexture == true) DirtyEach(t => t.UpdateTexture());
	}
}
#endif

[ExecuteInEditMode]
[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Jovian Scattering")]
public class SgtJovianScattering : MonoBehaviour
{
	[Tooltip("The jovian this texture will be applied to")]
	public SgtJovian Jovian;
	
	[Tooltip("The resolution of the day/sunset/night color transition in pixels")]
	public int Width = 64;

	[Tooltip("The resolution of the scattering transition in pixels")]
	public int Height = 512;

	[Tooltip("The format of the generated texture")]
	public TextureFormat Format = TextureFormat.ARGB32;
	
	[Tooltip("The sharpness of the forward scattered light")]
	public float Mie = 150.0f;
	
	[Tooltip("The brightness of the front and back scattered light")]
	public float Rayleigh = 0.1f;

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
		var importer = SgtHelper.ExportTextureDialog(generatedTexture, "Jovian Scattering");
		
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

	[ContextMenu("Update Texture")]
	public void UpdateTexture()
	{
		if (Width > 0 && Height > 0)
		{
			// Destroy if invalid
			if (generatedTexture != null)
			{
				if (generatedTexture.width != Width || generatedTexture.height != Height || generatedTexture.format != Format)
				{
					generatedTexture = SgtHelper.Destroy(generatedTexture);
				}
			}

			// Create?
			if (generatedTexture == null)
			{
				generatedTexture = SgtHelper.CreateTempTexture2D("Jovian Scattering (Generated)", Width, Height, Format);

				generatedTexture.wrapMode = TextureWrapMode.Clamp;

				UpdateApply();
			}

			var stepX = 1.0f / (Width  - 1);
			var stepY = 1.0f / (Height - 1);

			for (var y = 0; y < Height; y++)
			{
				var v = y * stepY;

				for (var x = 0; x < Width; x++)
				{
					var u = x * stepX;

					WriteTexture(u, v, x, y);
				}
			}
			
			generatedTexture.Apply();
		}
	}
	
	private void WriteTexture(float u, float v, int x, int y)
	{
		var ray        = Mathf.Abs(v * 2.0f - 1.0f); ray = Rayleigh * ray * ray;
		var mie        = Mathf.Pow(v, Mie);
		var scattering = ray + mie * (1.0f - ray);
		var sunsetU    = Mathf.InverseLerp(SunsetEnd, SunsetStart, u);
		var color      = default(Color);
		
		color.r = 1.0f - SgtEase.Evaluate(SunsetEase, Mathf.Pow(sunsetU, SunsetPowerR));
		color.g = 1.0f - SgtEase.Evaluate(SunsetEase, Mathf.Pow(sunsetU, SunsetPowerG));
		color.b = 1.0f - SgtEase.Evaluate(SunsetEase, Mathf.Pow(sunsetU, SunsetPowerB));
		color.a = (color.r + color.g + color.b) / 3.0f;

		generatedTexture.SetPixel(x, y, color * scattering);
	}
	
	[ContextMenu("Update Apply")]
	public void UpdateApply()
	{
		if (Jovian != null)
		{
			Jovian.ScatteringTex = generatedTexture;

			Jovian.UpdateScatteringTex();
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
			UpdateTexture();
		}

		UpdateApply();
	}
}