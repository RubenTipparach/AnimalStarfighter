using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(SgtHideWireframe))]
public class SgtHideWireframe_Editor : SgtEditor<SgtHideWireframe>
{
	protected override void OnInspector()
	{
	}
}
#endif

// This component will hide all children wireframes in edit mode
[ExecuteInEditMode]
[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Hide Wireframe")]
public class SgtHideWireframe : MonoBehaviour
{
#if UNITY_EDITOR
	protected virtual void Update()
	{
		var renderers = GetComponentsInChildren<Renderer>();
		
		for (var i = renderers.Length - 1; i >= 0; i--)
		{
			UnityEditor.EditorUtility.SetSelectedRenderState(renderers[i], UnityEditor.EditorSelectedRenderState.Hidden);
		}
	}
	
	protected virtual void OnDisable()
	{
		var renderers = GetComponentsInChildren<Renderer>();
		
		for (var i = renderers.Length - 1; i >= 0; i--)
		{
			UnityEditor.EditorUtility.SetSelectedRenderState(renderers[i], UnityEditor.EditorSelectedRenderState.Highlight);
		}
	}
#endif
}