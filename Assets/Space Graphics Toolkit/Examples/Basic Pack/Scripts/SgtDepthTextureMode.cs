using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(SgtDepthTextureMode))]
public class SgtDepthTextureMode_Editor : SgtEditor<SgtDepthTextureMode>
{
	protected override void OnInspector()
	{
		DrawDefault("DepthMode");
	}
}
#endif

// This component allows you to control a Camera component's depthTextureMode setting.
[ExecuteInEditMode]
[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Depth Texture Mode")]
public class SgtDepthTextureMode : MonoBehaviour
{
	[Tooltip("The depth mode that will be applied to the camera")]
	public DepthTextureMode DepthMode = DepthTextureMode.None;

	[System.NonSerialized]
	private Camera cachedCamera;

	public void UpdateDepthMode()
	{
		if (cachedCamera == null) cachedCamera = GetComponent<Camera>();

		cachedCamera.depthTextureMode = DepthMode;
	}

	protected virtual void Update()
	{
		UpdateDepthMode();
	}
}