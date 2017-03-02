using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(SgtRotate))]
public class SgtRotate_Editor : SgtEditor<SgtRotate>
{
	protected override void OnInspector()
	{
		DrawDefault("DegreesPerSecond");
	}
}
#endif

// This component rotates the GameObject every frame
[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Rotate")]
public class SgtRotate : MonoBehaviour
{
	public Vector3 DegreesPerSecond = new Vector3(0.0f, 100.0f, 0.0f);
	
	protected virtual void Update()
	{
		transform.Rotate(DegreesPerSecond * Time.deltaTime);
	}
}