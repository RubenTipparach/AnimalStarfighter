using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(SgtShadowLayer))]
public class SgtShadowLayer_Editor : SgtEditor<SgtShadowLayer>
{
	protected override void OnInspector()
	{
		var updateRenderers = false;

		BeginError(Any(t => t.Shadows != null && t.Shadows.Exists(s => s == null)));
			DrawDefault("Shadows"); // Updated automatically
		EndError();
		BeginError(Any(t => t.Renderers != null && t.Renderers.Exists(s => s == null)));
			DrawDefault("Renderers", ref updateRenderers, false);
		EndError();

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
[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Shadow Layer")]
public class SgtShadowLayer : MonoBehaviour
{
	[Tooltip("The shadows casting on this atmosphere")]
	public List<SgtShadow> Shadows;

	[Tooltip("The renderers that are used to render the inner atmosphere (surface)")]
	public List<MeshRenderer> Renderers;

	// The material added to all spacetime renderers
	[System.NonSerialized]
	public Material Material;

	[ContextMenu("Apply Material")]
	public void ApplyMaterial()
	{
		if (Renderers != null)
		{
			for (var i = Renderers.Count - 1; i >= 0; i--)
			{
				SgtHelper.AddMaterial(Renderers[i], Material);
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
				SgtHelper.RemoveMaterial(Renderers[i], Material);
			}
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
		Camera.onPreRender += CameraPreRender;

		if (Material == null)
		{
			Material = SgtHelper.CreateTempMaterial("Shadow Layer (Generated)", SgtHelper.ShaderNamePrefix + "ShadowLayer");
		}

		if (Renderers == null)
		{
			AddRenderer(GetComponent<MeshRenderer>());
		}

		ApplyMaterial();
	}

	protected virtual void OnDisable()
	{
		Camera.onPreRender -= CameraPreRender;

		RemoveMaterial();
	}

	protected virtual void CameraPreRender(Camera camera)
	{
		if (Material != null)
		{
			SgtHelper.SetTempMaterial(Material);

			SgtHelper.WriteShadows(Shadows, 2);
		}
	}
}
