using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
public class SgtQuads_Editor<T> : SgtEditor<T>
	where T : SgtQuads
{
	protected virtual void DrawMaterial(ref bool updateMaterial)
	{
		DrawDefault("Color", ref updateMaterial);
		BeginError(Any(t => t.Brightness < 0.0f));
			DrawDefault("Brightness", ref updateMaterial);
		EndError();
		DrawDefault("RenderQueue", ref updateMaterial);
		DrawDefault("RenderQueueOffset", ref updateMaterial);
	}

	protected virtual void DrawAtlas(ref bool updateMaterial, ref bool updateMeshesAndModels)
	{
		BeginError(Any(t => t.MainTex == null));
			DrawDefault("MainTex", ref updateMaterial);
		EndError();
		DrawDefault("Layout", ref updateMeshesAndModels);
		BeginIndent();
			if (Any(t => t.Layout == SgtQuadsLayoutType.Grid))
			{
				BeginError(Any(t => t.LayoutColumns <= 0));
					DrawDefault("LayoutColumns", ref updateMeshesAndModels);
				EndError();
				BeginError(Any(t => t.LayoutRows <= 0));
					DrawDefault("LayoutRows", ref updateMeshesAndModels);
				EndError();
			}

			if (Any(t => t.Layout == SgtQuadsLayoutType.Custom))
			{
				DrawDefault("Rects", ref updateMeshesAndModels);
			}
		EndIndent();
	}
}
#endif

// This is the base class for all starfields, providing a simple interface for generating meshes
// from a list of stars, as well as the material to render it
public abstract class SgtQuads : MonoBehaviour
{
	[Tooltip("The color tint")]
	public Color Color = Color.white;

	[Tooltip("The amount the Color.rgb values are multiplied by")]
	public float Brightness = 1.0f;

	[Tooltip("The main texture of this material")]
	public Texture MainTex;

	[Tooltip("The layout of cells in the texture")]
	public SgtQuadsLayoutType Layout = SgtQuadsLayoutType.Grid;

	[Tooltip("The amount of columns in the texture")]
	public int LayoutColumns = 1;

	[Tooltip("The amount of rows in the texture")]
	public int LayoutRows = 1;
	
	[Tooltip("The rects of each cell in the texture")]
	public List<Rect> Rects;

	[Tooltip("The render queue group for this material")]
	public SgtRenderQueue RenderQueue = SgtRenderQueue.Transparent;

	[Tooltip("The render queue offset for this material")]
	public int RenderQueueOffset;
	
	// The models used to render all the quads (because each mesh can only store 65k vertices)
	[HideInInspector]
	public List<SgtQuadsModel> Models;

	// The material applied to all models
	[System.NonSerialized]
	public Material Material;

	[SerializeField]
	private bool startCalled;

	[System.NonSerialized]
	private bool updateMaterialCalled;

	[System.NonSerialized]
	private bool updateMeshesAndModelsCalled;
	
	protected static List<Vector4> tempCoords = new List<Vector4>();

	protected abstract string ShaderName
	{
		get;
	}
	
	public void UpdateMainTex()
	{
		if (Material != null)
		{
			Material.SetTexture("_MainTex", MainTex);
		}
	}

	[ContextMenu("Update Material")]
	public void UpdateMaterial()
	{
		updateMaterialCalled = true;

		if (Material == null)
		{
			Material = SgtHelper.CreateTempMaterial("Starfield (Generated)", ShaderName);

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
		
		BuildMaterial();
	}
	
	[ContextMenu("Update Meshes and Models")]
	public void UpdateMeshesAndModels()
	{
		updateMeshesAndModelsCalled = true;

		var starCount  = BeginQuads();
		var modelCount = 0;

		// Build meshes and models until starCount reaches 0
		if (starCount > 0)
		{
			BuildRects();
			ConvertRectsToCoords();

			while (starCount > 0)
			{
				var quadCount = Mathf.Min(starCount, SgtHelper.QuadsPerMesh);
				var model     = GetOrNewModel(modelCount);
				var mesh      = GetOrNewMesh(model);
				
				model.SetMaterial(Material);

				BuildMesh(mesh, modelCount * SgtHelper.QuadsPerMesh, quadCount);
				
				modelCount += 1;
				starCount  -= quadCount;
			}
		}

		// Remove any excess
		if (Models != null)
		{
			for (var i = Models.Count - 1; i >= modelCount; i--)
			{
				SgtQuadsModel.Pool(Models[i]);

				Models.RemoveAt(i);
			}
		}
		
		EndQuads();
	}
	
	protected virtual void Start()
	{
		if (startCalled == false)
		{
			startCalled = true;

			StartOnce();
		}
	}
	
	protected virtual void OnEnable()
	{
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

	protected virtual void OnDisable()
	{
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
				SgtQuadsModel.MarkForDestruction(Models[i]);
			}
		}

		SgtHelper.Destroy(Material);
	}
	
	protected abstract int BeginQuads();
	
	protected abstract void EndQuads();

	protected virtual void BuildMaterial()
	{
		Material.renderQueue = (int)RenderQueue + RenderQueueOffset;

		Material.SetTexture("_MainTex", MainTex);
		Material.SetColor("_Color", SgtHelper.Brighten(Color, Color.a * Brightness));
		Material.SetFloat("_Scale", transform.lossyScale.x);
	}

	protected virtual void StartOnce()
	{
		CheckUpdateCalls();
	}

	protected void BuildRects()
	{
		if (Layout == SgtQuadsLayoutType.Grid)
		{
			if (Rects == null) Rects = new List<Rect>();

			Rects.Clear();

			if (LayoutColumns > 0 && LayoutRows > 0)
			{
				var invX = SgtHelper.Reciprocal(LayoutColumns);
				var invY = SgtHelper.Reciprocal(LayoutRows   );

				for (var y = 0; y < LayoutRows; y++)
				{
					var offY = y * invY;

					for (var x = 0; x < LayoutColumns; x++)
					{
						var offX = x * invX;
						var rect = new Rect(offX, offY, invX, invY);

						Rects.Add(rect);
					}
				}
			}
		}
	}
	
	protected abstract void BuildMesh(Mesh mesh, int starIndex, int starCount);
	
	protected static void ExpandBounds(ref bool minMaxSet, ref Vector3 min, ref Vector3 max, Vector3 position, float radius)
	{
		var radius3 = new Vector3(radius, radius, radius);

		if (minMaxSet == false)
		{
			minMaxSet = true;

			min = position - radius3;
			max = position + radius3;
		}

		min = Vector3.Min(min, position - radius3);
		max = Vector3.Max(max, position + radius3);
	}
	
	private void ConvertRectsToCoords()
	{
		tempCoords.Clear();

		if (Rects != null)
		{
			for (var i = 0; i < Rects.Count; i++)
			{
				var rect = Rects[i];

				tempCoords.Add(new Vector4(rect.xMin, rect.yMin, rect.xMax, rect.yMax));
			}
		}

		if (tempCoords.Count == 0) tempCoords.Add(default(Vector4));
	}

	private SgtQuadsModel GetOrNewModel(int index)
	{
		var model = default(SgtQuadsModel);

		if (Models == null)
		{
			Models = new List<SgtQuadsModel>();
		}

		if (index < Models.Count)
		{
			model = Models[index];
		}
		else
		{
			Models.Add(model);
		}

		if (model == null || model.Quads != this)
		{
			model = Models[index] = SgtQuadsModel.Create(this);
			
			model.SetMaterial(Material);
		}

		return model;
	}

	private Mesh GetOrNewMesh(SgtQuadsModel model)
	{
		var mesh = model.Mesh;
		
		if (mesh == null)
		{
			mesh = SgtHelper.CreateTempMesh("Quads Mesh (Generated)");

			model.SetMesh(mesh);
		}
		else
		{
			mesh.Clear();
		}

		return mesh;
	}

	private void CheckUpdateCalls()
	{
		if (updateMaterialCalled == false)
		{
			UpdateMaterial();
		}

		if (updateMeshesAndModelsCalled == false)
		{
			UpdateMeshesAndModels();
		}
	}
}