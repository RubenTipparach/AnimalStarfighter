using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(SgtRingMainTexFilter))]
public class SgtRingMainTexFilter_Editor : SgtEditor<SgtRingMainTexFilter>
{
	protected override void OnInspector()
	{
		var updateTexture = false;
		var updateApply   = false;
		
		DrawDefault("Ring", ref updateApply);
		BeginError(Any(t => t.Source == null));
			DrawDefault("Source", ref updateTexture);
		EndError();
		DrawDefault("Format", ref updateTexture);

		Separator();
		
		BeginError(Any(t => t.Power < 0.0f));
			DrawDefault("Power", ref updateTexture);
		EndError();
		
		if (updateTexture == true) DirtyEach(t => t.UpdateTexture());
		if (updateApply   == true) DirtyEach(t => t.UpdateApply   ());
	}
}
#endif

[ExecuteInEditMode]
[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Ring Main Tex Filter")]
public class SgtRingMainTexFilter : MonoBehaviour
{
	[Tooltip("The ring this texture will be applied to")]
	public SgtRing Ring;

	[Tooltip("The source ring texture that will be filtered")]
	public Texture2D Source;
	
	[Tooltip("The format of the generated texture")]
	public TextureFormat Format = TextureFormat.ARGB32;
	
	[Tooltip("The sharpness of the light/dark transition")]
	public float Power = 0.5f;
	
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
		var importer = SgtHelper.ExportTextureDialog(generatedTexture, "Ring MainTex");

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
		if (Source != null)
		{
			// Destroy if invalid
			if (generatedTexture != null)
			{
				if (generatedTexture.width != Source.width || generatedTexture.height != 1 || generatedTexture.format != Format)
				{
					generatedTexture = SgtHelper.Destroy(generatedTexture);
				}
			}

			// Create?
			if (generatedTexture == null)
			{
				generatedTexture = SgtHelper.CreateTempTexture2D("Ring MainTex (Generated)", Source.width, 1, Format);

				generatedTexture.wrapMode = TextureWrapMode.Clamp;

				UpdateApply();
			}
			
			for (var x = Source.width - 1; x >= 0; x--)
			{
				WriteTexture(x);
			}
			
			generatedTexture.Apply();
		}
	}
	
	private void WriteTexture(int x)
	{
		var pixel   = Source.GetPixel(x, 0);
		var highest = 0.0f;

		if (pixel.r > highest) highest = pixel.r;
		if (pixel.g > highest) highest = pixel.g;
		if (pixel.b > highest) highest = pixel.b;

		if (highest > 0.0f)
		{
			highest = 1.0f - Mathf.Pow(1.0f - highest, Power);
			//var inv = 1.0f / highest;

			//pixel.r *= inv;
			//pixel.g *= inv;
			//pixel.b *= inv;
			pixel.a  = highest;
		}
		else
		{
			pixel.a = 0.0f;
		}

		generatedTexture.SetPixel(x, 0, pixel);
	}
	
	[ContextMenu("Update Apply")]
	public void UpdateApply()
	{
		if (Ring != null)
		{
			Ring.MainTex = generatedTexture;

			Ring.UpdateMainTex();
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