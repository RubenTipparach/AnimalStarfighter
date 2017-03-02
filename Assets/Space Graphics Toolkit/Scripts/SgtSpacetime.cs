using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(SgtSpacetime))]
public class SgtSpacetime_Editor : SgtEditor<SgtSpacetime>
{
	protected override void OnInspector()
	{
		var updateMaterial  = false;
		var updateRenderers = false;

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
		BeginError(Any(t => t.Tile <= 0));
			DrawDefault("Tile", ref updateMaterial);
		EndError();

		Separator();

		DrawDefault("AmbientColor", ref updateMaterial);
		BeginError(Any(t => t.AmbientBrightness < 0.0f));
			DrawDefault("AmbientBrightness", ref updateMaterial);
		EndError();

		Separator();

		DrawDefault("DisplacementColor", ref updateMaterial);
		BeginError(Any(t => t.DisplacementBrightness < 0.0f));
			DrawDefault("DisplacementBrightness", ref updateMaterial);
		EndError();

		Separator();
		
		DrawDefault("HighlightColor", ref updateMaterial);
		DrawDefault("HighlightBrightness", ref updateMaterial);
		DrawDefault("HighlightScale", ref updateMaterial);
		BeginError(Any(t => t.HighlightPower < 0.0f));
			DrawDefault("HighlightPower", ref updateMaterial);
		EndError();

		Separator();

		DrawDefault("Displacement", ref updateMaterial);
		BeginIndent();
			DrawDefault("Accumulate", ref updateMaterial);
			
			if (Any(t => t.Displacement == SgtSpacetime.DisplacementType.Pinch))
			{
				BeginError(Any(t => t.Power < 0.0f));
					DrawDefault("Power", ref updateMaterial);
				EndError();
			}

			if (Any(t => t.Displacement == SgtSpacetime.DisplacementType.Offset))
			{
				DrawDefault("Offset", ref updateMaterial);
			}
		EndIndent();
		
		Separator();
		
		BeginError(Any(t => t.Renderers != null && (t.Renderers.Count == 0 || t.Renderers.Exists(r => r == null) == true)));
			DrawDefault("Renderers", ref updateRenderers, false);
		EndError();
		
		Separator();
		
		DrawDefault("UseAllWells", ref updateMaterial);
		BeginIndent();
			if (Any(t => t.UseAllWells == true))
			{
				DrawDefault("RequireSameLayer", ref updateMaterial);
				DrawDefault("RequireSameTag", ref updateMaterial);
				DrawDefault("RequireNameContains", ref updateMaterial);
			}
			
			if (Any(t => t.UseAllWells == false))
			{
				BeginError(Any(t => t.Wells != null && (t.Wells.Count == 0 || t.Wells.Exists(r => r == null) == true)));
					DrawDefault("Wells", ref updateMaterial);
				EndError();
			}
		EndIndent();

		if (updateMaterial == true) DirtyEach(t => t.UpdateMaterial());

		if (updateRenderers == true)
		{
			Each(t => t.RemoveMaterial());

			serializedObject.ApplyModifiedProperties();

			DirtyEach(t => t.ApplyMaterial());
		}
	}
}
#endif

[ExecuteInEditMode]
[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Spacetime")]
public class SgtSpacetime : MonoBehaviour
{
	public enum DisplacementType
	{
		Pinch,
		Offset
	}
	
	// All currently active and enabled spacetimes in the scene
	public static List<SgtSpacetime> AllSpacetimes = new List<SgtSpacetime>();
	
	[Tooltip("The color tint")]
	public Color Color = Color.white;
	
	[Tooltip("The color brightness")]
	public float Brightness = 1.0f;
	
	[Tooltip("The render queue group")]
	public SgtRenderQueue RenderQueue = SgtRenderQueue.Transparent;
	
	[Tooltip("The render queue offset")]
	public int RenderQueueOffset;
	
	[Tooltip("The main texture applied to the spacetime")]
	public Texture2D MainTex;

	[Tooltip("How many times should the spacetime texture be tiled?")]
	public int Tile = 1;

	[Tooltip("The ambient color")]
	public Color AmbientColor = Color.white;

	[Tooltip("The ambient brightness")]
	public float AmbientBrightness = 0.25f;

	[Tooltip("The displacement color")]
	public Color DisplacementColor = Color.white;

	[Tooltip("The displacement brightness")]
	public float DisplacementBrightness = 1.0f;
	
	[Tooltip("The color of the highlight")]
	public Color HighlightColor = Color.white;

	[Tooltip("The brightness of the highlight")]
	public float HighlightBrightness = 0.1f;

	[Tooltip("The sharpness of the highlight")]
	public float HighlightPower = 1.0f;

	[Tooltip("The scale of the highlight")]
	public float HighlightScale = 3.0f;

	[Tooltip("How should the vertices in the spacetime get displaced when a well is nearby?")]
	public DisplacementType Displacement = DisplacementType.Pinch;
	
	[Tooltip("Should the displacement effect additively stack if wells overlap?")]
	public bool Accumulate;
	
	[Tooltip("The pinch power")]
	public float Power = 3.0f;

	[Tooltip("How strong the fading is")]
	public float FadeScale = 1.0f;

	[Tooltip("The offset direction/vector for vertices within range of a well")]
	public Vector3 Offset = new Vector3(0.0f, -1.0f, 0.0f);
	
	[Tooltip("Automatically use all active and enabled wells in the scene?")]
	public bool UseAllWells = true;
	
	[Tooltip("Filter all the wells to require the same layer at this GameObject")]
	public bool RequireSameLayer;
	
	[Tooltip("Filter all the wells to require the same tag at this GameObject")]
	public bool RequireSameTag;
	
	[Tooltip("Filter all the wells to require a name that contains this")]
	public string RequireNameContains;
	
	[Tooltip("The wells currently being checked by the spacetime")]
	public List<SgtSpacetimeWell> Wells;
	
	[Tooltip("The renderers this spacetime is being applied to")]
	public List<MeshRenderer> Renderers;
	
	// The material added to all spacetime renderers
	[System.NonSerialized]
	public Material Material;

	[SerializeField]
	[HideInInspector]
	private bool startCalled;
	
	[System.NonSerialized]
	private bool updateMaterialCalled;
	
	[System.NonSerialized]
	private bool updateWellsCalled;

	// The well data arrays that get copied to the shader
	[System.NonSerialized] private Vector4  [] gauPos = new Vector4[12];
	[System.NonSerialized] private Vector4  [] gauDat = new Vector4[12];
	[System.NonSerialized] private Vector4  [] ripPos = new Vector4[1];
	[System.NonSerialized] private Vector4  [] ripDat = new Vector4[1];
	[System.NonSerialized] private Vector4  [] twiPos = new Vector4[1];
	[System.NonSerialized] private Vector4  [] twiDat = new Vector4[1];
	[System.NonSerialized] private Matrix4x4[] twiMat = new Matrix4x4[1];
	
	[ContextMenu("Update Material")]
	public void UpdateMaterial()
	{
		updateMaterialCalled = true;

		if (Material == null)
		{
			Material = SgtHelper.CreateTempMaterial("Spacetime (Generated)", SgtHelper.ShaderNamePrefix + "Spacetime");

			ApplyMaterial();
		}
		
		var ambientColor      = SgtHelper.Brighten(AmbientColor, AmbientBrightness);
		var displacementColor = SgtHelper.Brighten(DisplacementColor, DisplacementBrightness);
		var higlightColor     = SgtHelper.Brighten(HighlightColor, HighlightBrightness);
		
		Material.renderQueue = (int)RenderQueue + RenderQueueOffset;

		Material.SetTexture("_MainTex", MainTex);
		Material.SetColor("_Color", SgtHelper.Brighten(Color, Brightness));
		Material.SetColor("_AmbientColor", ambientColor);
		Material.SetColor("_DisplacementColor", displacementColor);
		Material.SetColor("_HighlightColor", higlightColor);
		Material.SetFloat("_HighlightPower", HighlightPower);
		Material.SetFloat("_HighlightScale", HighlightScale);
		Material.SetFloat("_Tile", Tile);
		
		if (Displacement == DisplacementType.Pinch)
		{
			Material.SetFloat("_Power", Power);
		}

		if (Displacement == DisplacementType.Offset)
		{
			SgtHelper.EnableKeyword("SGT_A", Material);

			Material.SetVector("_Offset", Offset);
		}
		else
		{
			SgtHelper.DisableKeyword("SGT_A", Material);
		}
		
		if (Accumulate == true)
		{
			SgtHelper.EnableKeyword("SGT_B", Material);
		}
		else
		{
			SgtHelper.DisableKeyword("SGT_B", Material);
		}
	}

	[ContextMenu("Update Wells")]
	public void UpdateWells()
	{
		if (Material != null)
		{
			var gaussianCount = 0;
			var rippleCount   = 0;
			var twistCount    = 0;

			WriteWells(ref gaussianCount, ref rippleCount, ref twistCount); // 12 is the shader instruction limit
			
			if ((gaussianCount & 1 << 0) != 0)
			{
				SgtHelper.EnableKeyword("SGT_C", Material);
			}
			else
			{
				SgtHelper.DisableKeyword("SGT_C", Material);
			}
			
			if ((gaussianCount & 1 << 1) != 0)
			{
				SgtHelper.EnableKeyword("SGT_D", Material);
			}
			else
			{
				SgtHelper.DisableKeyword("SGT_D", Material);
			}
			
			if ((gaussianCount & 1 << 2) != 0)
			{
				SgtHelper.EnableKeyword("SGT_E", Material);
			}
			else
			{
				SgtHelper.DisableKeyword("SGT_E", Material);
			}
			
			if ((gaussianCount & 1 << 3) != 0)
			{
				SgtHelper.EnableKeyword("LIGHT_0", Material);
			}
			else
			{
				SgtHelper.DisableKeyword("LIGHT_0", Material);
			}

			if ((rippleCount & 1 << 0) != 0)
			{
				SgtHelper.EnableKeyword("LIGHT_1", Material);
			}
			else
			{
				SgtHelper.DisableKeyword("LIGHT_1", Material);
			}

			if ((twistCount & 1 << 0) != 0)
			{
				SgtHelper.EnableKeyword("SHADOW_1", Material);
			}
			else
			{
				SgtHelper.DisableKeyword("SHADOW_1", Material);
			}
		}
	}
	
	[ContextMenu("Apply Material")]
	public void ApplyMaterial()
	{
		for (var i = Renderers.Count - 1; i >= 0; i--)
		{
			var renderer = Renderers[i];
			
			if (renderer != null && renderer.sharedMaterial != Material)
			{
				renderer.sharedMaterial = Material;
			}
		}
	}

	[ContextMenu("Remove Material")]
	public void RemoveMaterial()
	{
		if (Renderers != null)
		{
			for (var i = Renderers.Count - 1; i >= 0; i--)
			{
				var renderer = Renderers[i];
			
				if (renderer != null && renderer.sharedMaterial == Material)
				{
					renderer.sharedMaterial = null;
				}
			}
		}
	}

	[ContextMenu("Add Well")]
	public SgtSpacetimeWell AddWell()
	{
		var well = SgtSpacetimeWell.Create(this);
#if UNITY_EDITOR
		SgtHelper.SelectAndPing(well);
#endif
		return well;
	}

	public void AddWell(SgtSpacetimeWell well)
	{
		if (Wells == null)
		{
			Wells = new List<SgtSpacetimeWell>();
		}

		if (Wells.Contains(well) == false)
		{
			Wells.Add(well);
		}
	}

	public void AddRenderer(MeshRenderer renderer)
	{
		if (renderer != null)
		{
			if (Renderers == null)
			{
				Renderers = new List<MeshRenderer>();
			}

			if (Renderers.Contains(renderer) == false)
			{
				if (renderer.sharedMaterial != Material)
				{
					renderer.sharedMaterial = Material;
				}

				Renderers.Add(renderer);
			}
		}
	}

	public void RemoveRenderer(MeshRenderer renderer)
	{
		if (renderer != null && Renderers != null)
		{
			if (renderer.sharedMaterial == Material)
			{
				renderer.sharedMaterial = null;
			}
			
			Renderers.Remove(renderer);
		}
	}
	
	protected virtual void OnEnable()
	{
		AllSpacetimes.Add(this);
		
		if (startCalled == true)
		{
			CheckUpdateCalls();
		}

		ApplyMaterial();
	}

	protected virtual void Start()
	{
		if (startCalled == false)
		{
			startCalled = true;
			
			if (Renderers == null)
			{
				AddRenderer(GetComponent<MeshRenderer>());
			}

			CheckUpdateCalls();
		}
	}

	protected virtual void Update()
	{
		// The wells might have moved or changed settings, so update them all in update
		UpdateWells();
	}
	
	protected virtual void OnDisable()
	{
		AllSpacetimes.Remove(this);
		
		RemoveMaterial();
	}

	private void WriteWells(ref int gaussianCount, ref int rippleCount, ref int twistCount)
	{
		var wells = UseAllWells == true ? SgtSpacetimeWell.AllWells : Wells;
		
		if (wells != null)
		{
			for (var i = wells.Count - 1; i >= 0; i--)
			{
				var well = wells[i];
			
				if (SgtHelper.Enabled(well) == true && well.Radius > 0.0f)
				{
					if (well.Distribution == SgtSpacetimeWell.DistributionType.Gaussian && gaussianCount >= gauPos.Length)
					{
						continue;
					}

					if (well.Distribution == SgtSpacetimeWell.DistributionType.Ripple && rippleCount >= ripPos.Length)
					{
						continue;
					}

					if (well.Distribution == SgtSpacetimeWell.DistributionType.Twist && twistCount >= twiPos.Length)
					{
						continue;
					}

					// If the well list is atuo generated, allow well filtering
					if (UseAllWells == true)
					{
						if (RequireSameLayer == true && gameObject.layer != well.gameObject.layer)
						{
							continue;
						}
					
						if (RequireSameTag == true && tag != well.tag)
						{
							continue;
						}
					
						if (string.IsNullOrEmpty(RequireNameContains) == false && well.name.Contains(RequireNameContains) == false)
						{
							continue;
						}
					}
				
					var wellPos = well.transform.position;

					switch (well.Distribution)
					{
						case SgtSpacetimeWell.DistributionType.Gaussian:
						{
							var index = gaussianCount++;

							gauPos[index] = new Vector4(wellPos.x, wellPos.y, wellPos.z, well.Radius);
							gauDat[index] = new Vector4(well.Strength, 0.0f, 0.0f, 0.0f);
						}
						break;

						case SgtSpacetimeWell.DistributionType.Ripple:
						{
							var index = rippleCount++;

							ripPos[index] = new Vector4(wellPos.x, wellPos.y, wellPos.z, well.Radius);
							ripDat[index] = new Vector4(well.Strength, well.Frequency, well.Offset, 0.0f);
						}
						break;

						case SgtSpacetimeWell.DistributionType.Twist:
						{
							var index = twistCount++;

							twiPos[index] = new Vector4(wellPos.x, wellPos.y, wellPos.z, well.Radius);
							twiDat[index] = new Vector4(well.Strength, well.Frequency, well.HoleSize, well.HolePower);
							twiMat[index] = well.transform.worldToLocalMatrix;
						}
						break;
					}
				}
			}

			Material.SetVectorArray("_GauPos", gauPos);
			Material.SetVectorArray("_GauDat", gauDat);
			Material.SetVectorArray("_RipPos", ripPos);
			Material.SetVectorArray("_RipDat", ripDat);
			Material.SetVectorArray("_TwiPos", twiPos);
			Material.SetVectorArray("_TwiDat", twiDat);
			Material.SetMatrixArray("_TwiMat", twiMat);
		}
	}

	private void CheckUpdateCalls()
	{
		if (updateMaterialCalled == false)
		{
			UpdateMaterial();
		}

		if (updateWellsCalled == false)
		{
			UpdateWells();
		}
	}
}