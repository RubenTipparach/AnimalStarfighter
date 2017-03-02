using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(SgtRingLighting))]
public class SgtRingLighting_Editor : SgtEditor<SgtRingLighting>
{
	protected override void OnInspector()
	{
		var updateTexture = false;
		var updateApply   = false;
		
		DrawDefault("Ring", ref updateApply);
		BeginError(Any(t => t.Width < 1));
			DrawDefault("Width", ref updateTexture);
		EndError();
		DrawDefault("Format", ref updateTexture);

		Separator();
		
		BeginError(Any(t => t.FrontPower < 0.0f));
			DrawDefault("FrontPower", ref updateTexture);
		EndError();
		BeginError(Any(t => t.BackPower < 0.0f));
			DrawDefault("BackPower", ref updateTexture);
		EndError();
		
		BeginError(Any(t => t.BackStrength < 0.0f));
			DrawDefault("BackStrength", ref updateTexture);
		EndError();
		BeginError(Any(t => t.BackStrength < 0.0f));
			DrawDefault("BaseStrength", ref updateTexture);
		EndError();
		
		if (updateTexture == true) DirtyEach(t => t.UpdateTexture());
		if (updateApply   == true) DirtyEach(t => t.UpdateApply   ());
	}
}
#endif

[ExecuteInEditMode]
[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Ring Lighting")]
public class SgtRingLighting : MonoBehaviour
{
	[Tooltip("The ring this texture will be applied to")]
	public SgtRing Ring;

	[Tooltip("The resolution of the light/dark transition in pixels")]
	public int Width = 256;
	
	[Tooltip("The format of the generated texture")]
	public TextureFormat Format = TextureFormat.ARGB32;
	
	[Tooltip("How sharp the incoming light scatters forward")]
	public float FrontPower = 2.0f;
	
	[Tooltip("How sharp the incoming light scatters backward")]
	public float BackPower = 3.0f;

	[Tooltip("The strength of the back scattered light")]
	[Range(0.0f, 1.0f)]
	public float BackStrength = 0.5f;
	
	[Tooltip("The of the perpendicular scattered light")]
	[Range(0.0f, 1.0f)]
	public float BaseStrength = 0.2f;

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
		var importer = SgtHelper.ExportTextureDialog(generatedTexture, "Ring Lighting");

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
				generatedTexture = SgtHelper.CreateTempTexture2D("Ring Lighting (Generated)", Width, 1, Format);

				generatedTexture.wrapMode = TextureWrapMode.Clamp;

				UpdateApply();
			}

			var color = Color.clear;
			var step  = 1.0f / (Width - 1);

			for (var x = 0; x < Width; x++)
			{
				var u = x * step;

				WriteTexture(u, x);
			}
			
			generatedTexture.Apply();
		}
	}
	
	private void WriteTexture(float u, int x)
	{
		var back     = Mathf.Pow(       u,  BackPower) * BackStrength;
		var front    = Mathf.Pow(1.0f - u, FrontPower);
		var lighting = BaseStrength;

		lighting = Mathf.Lerp(lighting, 1.0f, back );
		lighting = Mathf.Lerp(lighting, 1.0f, front);

		var color = new Color(lighting, lighting, lighting, 0.0f);
		
		generatedTexture.SetPixel(x, 0, color);
	}
	
	[ContextMenu("Update Apply")]
	public void UpdateApply()
	{
		if (Ring != null)
		{
			if (generatedTexture != null)
			{
				if (Ring.LightingTex != generatedTexture)
				{
					Ring.LightingTex = generatedTexture;

					Ring.UpdateLightingTex();
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

			if (Ring == null)
			{
				Ring = GetComponent<SgtRing>();
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