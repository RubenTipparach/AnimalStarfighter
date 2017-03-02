using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(SgtMove))]
public class SgtMove_Editor : SgtEditor<SgtMove>
{
	protected override void OnInspector()
	{
		DrawDefault("Speed");
	}
}
#endif

// This component will move the GameObject every frame
[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Move")]
public class SgtMove : MonoBehaviour
{
	[Tooltip("The speed this GameObject will be moved each second")]
	public Vector3 Speed = Vector3.forward;
	
	protected virtual void Update()
	{
		transform.Translate(Speed * Time.deltaTime);
	}
}