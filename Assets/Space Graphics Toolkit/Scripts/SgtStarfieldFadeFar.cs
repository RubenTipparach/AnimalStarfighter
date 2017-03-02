using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(SgtStarfieldFadeFar))]
public class SgtStarfieldFadeFar_Editor : SgtEditor<SgtStarfieldFadeFar>
{
	protected override void OnInspector()
	{
		var updateTexture = false;
		var updateApply   = false;
		
		BeginError(Any(t => t.Starfield == null));
			DrawDefault("Starfield", ref updateApply);
		EndError();
		BeginError(Any(t => t.Width < 1));
			DrawDefault("Width", ref updateTexture);
		EndError();
		DrawDefault("Format", ref updateTexture);
		
		Separator();
		
		DrawDefault("Ease", ref updateTexture);
		BeginError(Any(t => t.Power < 1.0f));
			DrawDefault("Power", ref updateTexture);
		EndError();

		if (updateTexture == true) DirtyEach(t => t.UpdateTextures());
		if (updateApply   == true) DirtyEach(t => t.UpdateApply   ());
	}
}
#endif

[ExecuteInEditMode]
[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Starfield Fade Far")]
public class SgtStarfieldFadeFar : MonoBehaviour
{
	[Tooltip("The starfield this fade texture gets applied to")]
	public SgtPointStarfield Starfield;
	
	[Tooltip("The resolution of the fade transition")]
	public int Width = 256;
	
	[Tooltip("The texture format of the textures")]
	public TextureFormat Format = TextureFormat.ARGB32;
	
	[Tooltip("The transition style")]
	public SgtEase.Type Ease = SgtEase.Type.Smoothstep;

	[Tooltip("The sharpness of the transition")]
	public float Power = 2.0f;
	
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
		var importer = SgtHelper.ExportTextureDialog(generatedTexture, "Starfield Fade Far");

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
				generatedTexture = SgtHelper.CreateTempTexture2D("Starfield Fade Far (Generated)", Width, 1, Format);

				generatedTexture.wrapMode = TextureWrapMode.Clamp;

				UpdateApply();
			}

			var color = Color.clear;
			var stepY = 1.0f / (Width - 1);

			for (var x = 0; x < Width; x++)
			{
				var u = x * stepY;

				WriteTexture(u, x);
			}
			
			generatedTexture.Apply();
		}
	}
	
	private void WriteTexture(float u, int x)
	{
		var fade  = 1.0f - SgtEase.Evaluate(Ease, 1.0f - Mathf.Pow(u, Power));
		var color = new Color(fade, fade, fade, fade);
		
		generatedTexture.SetPixel(x, 0, color);
	}
	
	[ContextMenu("Update Apply")]
	public void UpdateApply()
	{
		if (Starfield != null)
		{
			Starfield.FadeFarTex = generatedTexture;

			Starfield.UpdateFadeFarTex();
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

			if (Starfield == null)
			{
				Starfield = GetComponent<SgtPointStarfield>();
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