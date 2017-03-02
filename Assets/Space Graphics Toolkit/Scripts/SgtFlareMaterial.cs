using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(SgtFlareMaterial))]
public class SgtFlareMaterial_Editor : SgtEditor<SgtFlareMaterial>
{
	protected override void OnInspector()
	{
		var updateMaterial = false;
		var updateTexture  = false;
		var updateApply    = false;
		
		BeginError(Any(t => t.Flare == null));
			DrawDefault("Flare", ref updateApply);
		EndError();

		Separator();
		
		DrawDefault("ZTest", ref updateMaterial);
		DrawDefault("RenderQueue", ref updateMaterial);
		DrawDefault("RenderQueueOffset", ref updateMaterial);
		
		Separator();
		
		DrawDefault("Format", ref updateTexture);
		BeginError(Any(t => t.Width < 1));
			DrawDefault("Width", ref updateTexture);
		EndError();

		Separator();
		
		DrawDefault("Color", ref updateTexture);
		DrawDefault("Ease", ref updateTexture);
		BeginError(Any(t => t.PowerR <= 0));
			DrawDefault("PowerR", ref updateTexture);
		EndError();
		BeginError(Any(t => t.PowerG <= 0));
			DrawDefault("PowerG", ref updateTexture);
		EndError();
		BeginError(Any(t => t.PowerB <= 0));
			DrawDefault("PowerB", ref updateTexture);
		EndError();
		
		serializedObject.ApplyModifiedProperties();

		if (updateMaterial == true) DirtyEach(t => t.UpdateMaterial());
		if (updateTexture  == true) DirtyEach(t => t.UpdateTexture ());
		if (updateApply    == true) DirtyEach(t => t.UpdateApply   ());
	}
}
#endif

[ExecuteInEditMode]
[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Flare Material")]
public class SgtFlareMaterial : MonoBehaviour
{
	public enum ZTestState
	{
		//Less     = 0,
		//Greater  = 1,
		LEqual   = 2,
		//GEqual   = 3,
		//Equal    = 4,
		//NotEqual = 5,
		Always   = 6
	}

	[Tooltip("The flare this material will be applied to")]
	public SgtFlare Flare;

	[Tooltip("The ZTest mode of the material (Always = draw on top)")]
	public ZTestState ZTest = ZTestState.LEqual;
	
	[Tooltip("The render queue group for this flare")]
	public SgtRenderQueue RenderQueue = SgtRenderQueue.Transparent;

	[Tooltip("The render queue offset for this flare")]
	public int RenderQueueOffset;

	[Tooltip("The format of this texture")]
	public TextureFormat Format = TextureFormat.ARGB32;

	[Tooltip("The width of this texture")]
	public int Width = 256;
	
	[Tooltip("The base color of the texture")]
	public Color Color = Color.white;

	[Tooltip("The color transition style")]
	public SgtEase.Type Ease = SgtEase.Type.Exponential;

	[Tooltip("The sharpness of the red transition")]
	public float PowerR = 3.0f;
	
	[Tooltip("The sharpness of the green transition")]
	public float PowerG = 2.0f;
	
	[Tooltip("The sharpness of the blue transition")]
	public float PowerB = 1.0f;
	
	[System.NonSerialized]
	private Material generatedMaterial;
	
	[System.NonSerialized]
	private Texture2D generatedTexture;
	
	[SerializeField]
	private bool startCalled;
	
	public Material GeneratedMaterial
	{
		get
		{
			return generatedMaterial;
		}
	}
	
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
		var importer = SgtHelper.ExportTextureDialog(generatedTexture, "Flare Texture");

		if (importer != null)
		{
			importer.textureCompression = TextureImporterCompression.Uncompressed;
			importer.alphaSource        = TextureImporterAlphaSource.None;
			importer.wrapMode           = TextureWrapMode.Clamp;
			importer.filterMode         = FilterMode.Trilinear;
			importer.anisoLevel         = 16;

			importer.SaveAndReimport();
		}
	}
#endif

	[ContextMenu("Update Material")]
	public void UpdateMaterial()
	{
		// Create?
		if (generatedMaterial == null)
		{
			generatedMaterial = SgtHelper.CreateTempMaterial("Flare Material (Generated)", "SgtFlare");
			
			UpdateApply();
		}

		generatedMaterial.SetTexture("_MainTex", generatedTexture);

		generatedMaterial.renderQueue = (int)RenderQueue + RenderQueueOffset;

		generatedMaterial.SetInt("_ZTest", (int)ZTest);
	}

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
				generatedTexture = SgtHelper.CreateTempTexture2D("Flare Texture (Generated)", Width, 1, Format);
				
				generatedTexture.wrapMode = TextureWrapMode.Clamp;

				if (generatedMaterial != null)
				{
					generatedMaterial.SetTexture("_MainTex", generatedTexture);
				}
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
		var color = Color;

		color.r *= 1.0f - SgtEase.Evaluate(Ease, 1.0f - Mathf.Pow(1.0f - u, PowerR));
		color.g *= 1.0f - SgtEase.Evaluate(Ease, 1.0f - Mathf.Pow(1.0f - u, PowerG));
		color.b *= 1.0f - SgtEase.Evaluate(Ease, 1.0f - Mathf.Pow(1.0f - u, PowerB));
		color.a  = color.grayscale;
		
		generatedTexture.SetPixel(x, 0, color);
	}
	
	[ContextMenu("Update Apply")]
	public void UpdateApply()
	{
		if (Flare == null)
		{
			Flare = GetComponent<SgtFlare>();
		}

		if (Flare != null)
		{
			Flare.Material = generatedMaterial;

			Flare.UpdateMaterial();
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
			
			CheckUpdateCalls();
		}
	}

	protected virtual void OnDestroy()
	{
		SgtHelper.Destroy(generatedMaterial);
		SgtHelper.Destroy(generatedTexture );
	}
	
	private void CheckUpdateCalls()
	{
		if (generatedMaterial == null)
		{
			UpdateMaterial();
		}

		if (generatedTexture == null)
		{
			UpdateTexture();
		}
	}
}