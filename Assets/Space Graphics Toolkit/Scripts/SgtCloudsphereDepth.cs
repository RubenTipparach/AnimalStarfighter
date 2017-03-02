using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(SgtCloudsphereDepth))]
public class SgtCloudsphereDepth_Editor : SgtEditor<SgtCloudsphereDepth>
{
	protected override void OnInspector()
	{
		var updateTexture = false;
		var updateApply   = false;
		
		DrawDefault("Cloudsphere", ref updateApply);
		BeginError(Any(t => t.Width < 1));
			DrawDefault("Width", ref updateTexture);
		EndError();
		DrawDefault("Format", ref updateTexture);
		
		Separator();

		DrawDefault("RimEase", ref updateTexture);
		DrawDefault("RimColor", ref updateTexture);
		BeginError(Any(t => t.RimPower < 1.0f));
			DrawDefault("RimPower", ref updateTexture);
		EndError();

		Separator();
		
		BeginError(Any(t => t.AlphaDensity < 1.0f));
			DrawDefault("AlphaDensity", ref updateTexture);
		EndError();
		BeginError(Any(t => t.AlphaFade < 1.0f));
			DrawDefault("AlphaFade", ref updateTexture);
		EndError();

		if (updateTexture == true) DirtyEach(t => t.UpdateTexture());
		if (updateApply   == true) DirtyEach(t => t.UpdateApply  ());
	}
}
#endif

[ExecuteInEditMode]
[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Cloudsphere Depth")]
public class SgtCloudsphereDepth : MonoBehaviour
{
	[Tooltip("The cloudsphere this texture will be applied to")]
	public SgtCloudsphere Cloudsphere;
	
	[Tooltip("The resolution of the surface/space optical thickness transition in pixels")]
	public int Width = 256;
	
	[Tooltip("The format of this texture")]
	public TextureFormat Format = TextureFormat.ARGB32;
	
	[Tooltip("The rim transition style")]
	public SgtEase.Type RimEase = SgtEase.Type.Exponential;

	[Tooltip("The rim color")]
	public Color RimColor = new Color(1.0f, 0.0f, 0.0f, 0.25f);

	[Tooltip("The rim transition sharpness")]
	public float RimPower = 5.0f;

	[Tooltip("The density of the atmosphere")]
	public float AlphaDensity = 50.0f;

	[Tooltip("The strength of the density fading in the upper atmosphere")]
	public float AlphaFade = 2.0f;
	
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
		var importer = SgtHelper.ExportTextureDialog(generatedTexture, "Cloudsphere Depth");

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
				generatedTexture = SgtHelper.CreateTempTexture2D("Cloudsphere Depth (Generated)", Width, 1, Format);

				generatedTexture.wrapMode = TextureWrapMode.Clamp;

				UpdateApply();
			}

			var stepX = 1.0f / (Width - 1);

			for (var x = 0; x < Width; x++)
			{
				var v = x * stepX;

				WriteTexture(v, x);
			}
			
			generatedTexture.Apply();
		}
	}
	
	private void WriteTexture(float u, int x)
	{
		var rim   = SgtEase.Evaluate(RimEase, Mathf.Pow(1.0f - u, RimPower));
		var color = Color.Lerp(Color.white, RimColor, rim * RimColor.a);
		
		color.a = 1.0f - Mathf.Pow(1.0f - Mathf.Pow(u, AlphaFade), AlphaDensity);
		
		generatedTexture.SetPixel(x, 0, color);
	}
	
	[ContextMenu("Update Apply")]
	public void UpdateApply()
	{
		if (Cloudsphere != null)
		{
			if (generatedTexture != null)
			{
				if (Cloudsphere.DepthTex != generatedTexture)
				{
					Cloudsphere.DepthTex = generatedTexture;

					Cloudsphere.UpdateDeptchTex();
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

			if (Cloudsphere == null)
			{
				Cloudsphere = GetComponent<SgtCloudsphere>();
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