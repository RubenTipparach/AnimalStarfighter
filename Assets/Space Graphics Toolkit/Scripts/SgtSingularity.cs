using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(SgtSingularity))]
public class SgtSingularity_Editor : SgtEditor<SgtSingularity>
{
	protected override void OnInspector()
	{
		var updateMaterial = false;
		var updateModels   = false;

		DrawDefault("RenderQueue", ref updateMaterial);
		DrawDefault("RenderQueueOffset", ref updateMaterial);
		
		Separator();
		
		BeginError(Any(t => t.PinchPower < 0.0f));
			DrawDefault("PinchPower", ref updateMaterial);
		EndError();
		DrawDefault("PinchOffset", ref updateMaterial);
		
		Separator();

		BeginError(Any(t => t.HolePower < 0.0f));
			DrawDefault("HolePower", ref updateMaterial);
		EndError();
		DrawDefault("HoleColor", ref updateMaterial);

		Separator();

		DrawDefault("Tint", ref updateMaterial);

		if (Any(t => t.Tint == true))
		{
			BeginIndent();
				BeginError(Any(t => t.TintPower < 0.0f));
					DrawDefault("TintPower", ref updateMaterial);
				EndError();
				DrawDefault("TintColor", ref updateMaterial);
			EndIndent();
		}

		Separator();

		DrawDefault("EdgeFade", ref updateMaterial);

		if (Any(t => t.EdgeFade != SgtSingularity.EdgeFadeType.None))
		{
			BeginError(Any(t => t.EdgeFadePower < 0.0f));
				DrawDefault("EdgeFadePower", ref updateMaterial);
			EndError();
		}

		Separator();
		
		BeginError(Any(t => t.Meshes != null && (t.Meshes.Count == 0 || t.Meshes.FindIndex(m => m == null) != -1)));
			DrawDefault("Meshes", ref updateModels);
		EndError();

		if (updateMaterial == true) DirtyEach(t => t.UpdateMaterial());
		if (updateModels   == true) DirtyEach(t => t.UpdateModels  ());
	}
}
#endif

[ExecuteInEditMode]
[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Singularity")]
public class SgtSingularity : MonoBehaviour
{
	public enum EdgeFadeType
	{
		None,
		Center,
		Fragment
	}

	// All active and enabled singularities in the scene
	public static List<SgtSingularity> AllSingularities = new List<SgtSingularity>();

	[Tooltip("The meshes used to build the singularity mesh (should be a sphere)")]
	public List<Mesh> Meshes;
	
	[Tooltip("The render queue group for this singularity")]
	public SgtRenderQueue RenderQueue = SgtRenderQueue.Transparent;

	[Tooltip("The render queue offset for this singularity")]
	public int RenderQueueOffset;

	[Tooltip("How much the singulaity distorts the screen")]
	public float PinchPower = 10.0f;
	
	[Tooltip("How large the pinch start point is")]
	[Range(0.0f, 0.5f)]
	public float PinchOffset = 0.02f;

	[Tooltip("To prevent rendering issues the singularity can be faded out as it approaches the edges of the screen. This allows you to set how the fading is calculated")]
	public EdgeFadeType EdgeFade = EdgeFadeType.Fragment;

	[Tooltip("How sharp the fading effect is")]
	public float EdgeFadePower = 2.0f;
	
	[Tooltip("The color of the pinched hole")]
	public Color HoleColor = Color.black;

	[Tooltip("How sharp the hole color gradient is")]
	public float HolePower = 2.0f;

	[Tooltip("Enable this if you want the singulairty to tint nearby space")]
	public bool Tint;

	[Tooltip("The color of the tint")]
	public Color TintColor = Color.red;

	[Tooltip("How sharp the tint color gradient is")]
	public float TintPower = 2.0f;

	[Tooltip("The models used to render the full jovian")]
	[UnityEngine.Serialization.FormerlySerializedAs("models")]
	public List<SgtSingularityModel> Models;

	// The material applied to all models
	[System.NonSerialized]
	public Material Material;
	
	[SerializeField]
	[HideInInspector]
	private bool startCalled;
	
	[System.NonSerialized]
	private bool updateMaterialCalled;

	[System.NonSerialized]
	private bool updateModelsCalled;

	[ContextMenu("Update Material")]
	public void UpdateMaterial()
	{
		updateMaterialCalled = true;

		if (Material == null)
		{
			Material = SgtHelper.CreateTempMaterial("Singulairty (Generated)", SgtHelper.ShaderNamePrefix + "Singularity");

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
		
		Material.renderQueue = (int)RenderQueue + RenderQueueOffset;

		Material.SetVector("_Center", SgtHelper.NewVector4(transform.position, 1.0f));
		
		Material.SetFloat("_PinchPower", PinchPower);
		Material.SetFloat("_PinchScale", SgtHelper.Reciprocal(1.0f - PinchOffset));
		Material.SetFloat("_PinchOffset", PinchOffset);

		Material.SetFloat("_HolePower", HolePower);
		Material.SetColor("_HoleColor", HoleColor);

		SgtHelper.SetTempMaterial(Material);

		if (Tint == true)
		{
			SgtHelper.EnableKeyword("SGT_A"); // Tint

			Material.SetFloat("_TintPower", TintPower);
			Material.SetColor("_TintColor", TintColor);
		}
		else
		{
			SgtHelper.DisableKeyword("SGT_A"); // Tint
		}

		if (EdgeFade == EdgeFadeType.Center)
		{
			SgtHelper.EnableKeyword("SGT_B"); // Fade Center

			Material.SetFloat("_EdgeFadePower", EdgeFadePower);
		}
		else
		{
			SgtHelper.DisableKeyword("SGT_B"); // Fade Center
		}

		if (EdgeFade == EdgeFadeType.Fragment)
		{
			SgtHelper.EnableKeyword("SGT_C"); // Fade Fragment

			Material.SetFloat("_EdgeFadePower", EdgeFadePower);
		}
		else
		{
			SgtHelper.DisableKeyword("SGT_C"); // Fade Fragment
		}
	}

	[ContextMenu("Update Models")]
	public void UpdateModels()
	{
		updateModelsCalled = true;

		var meshCount = Meshes != null ? Meshes.Count : 0;
		
		for (var i = 0; i < meshCount; i++)
		{
			var mesh  = Meshes[i];
			var model = GetOrAddModel(i);

			model.SetMesh(mesh);
			model.SetMaterial(Material);
		}

		// Remove any excess
		if (Models != null)
		{
			for (var i = Models.Count - 1; i >= meshCount; i--)
			{
				SgtSingularityModel.Pool(Models[i]);

				Models.RemoveAt(i);
			}
		}
	}

	public static SgtSingularity CreateSingularity(int layer = 0, Transform parent = null)
	{
		return CreateSingularity(layer, parent, Vector3.zero, Quaternion.identity, Vector3.one);
	}

	public static SgtSingularity CreateSingularity(int layer, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
	{
		var gameObject  = SgtHelper.CreateGameObject("Singularity", layer, parent, localPosition, localRotation, localScale);
		var singularity = gameObject.AddComponent<SgtSingularity>();

		return singularity;
	}

#if UNITY_EDITOR
	[UnityEditor.MenuItem(SgtHelper.GameObjectMenuPrefix + "Singularity", false, 10)]
	public static void CreateSingularityMenuItem()
	{
		var parent      = SgtHelper.GetSelectedParent();
		var singularity = CreateSingularity(parent != null ? parent.gameObject.layer : 0, parent);

		SgtHelper.SelectAndPing(singularity);
	}
#endif

	protected virtual void OnEnable()
	{
		AllSingularities.Add(this);

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

	protected virtual void Start()
	{
		if (startCalled == false)
		{
			startCalled = true;

			// Add a mesh?
#if UNITY_EDITOR
			if (Meshes == null)
			{
				var mesh = SgtHelper.LoadFirstAsset<Mesh>("Geosphere40 t:mesh");

				if (mesh != null)
				{
					Meshes = new List<Mesh>();

					Meshes.Add(mesh);
				}
			}
#endif

			CheckUpdateCalls();
		}
	}

	protected virtual void OnDisable()
	{
		AllSingularities.Remove(this);

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
				SgtSingularityModel.MarkForDestruction(Models[i]);
			}
		}

		SgtHelper.Destroy(Material);
	}

	private SgtSingularityModel GetOrAddModel(int index)
	{
		var model = default(SgtSingularityModel);

		if (Models == null)
		{
			Models = new List<SgtSingularityModel>();
		}

		if (index < Models.Count)
		{
			model = Models[index];

			if (model == null)
			{
				model = SgtSingularityModel.Create(this);

				Models[index] = model;
			}
		}
		else
		{
			model = SgtSingularityModel.Create(this);

			Models.Add(model);
		}

		return model;
	}

	private void CheckUpdateCalls()
	{
		if (updateMaterialCalled == false)
		{
			UpdateMaterial();
		}

		if (updateModelsCalled == false)
		{
			UpdateModels();
		}
	}
}
