using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(SgtRaycastDepth))]
public class SgtRaycastDepth_Editor : SgtEditor<SgtRaycastDepth>
{
	protected override void OnInspector()
	{
		BeginError(Any(t => t.Layers == 0));
			DrawDefault("Layers"); // Updated automatically
		EndError();
		DrawDefault("Ease"); // Updated automatically
		BeginError(Any(t => t.MaxThickness <= 0.0f));
			DrawDefault("MaxThickness"); // Updated automatically
		EndError();
	}
}
#endif

[ExecuteInEditMode]
public class SgtRaycastDepth : SgtDepth
{
	[Tooltip("For the depth to return 1, the raycast must go through an object with this thickness in world space")]
	public float MaxThickness = 1.0f;
	
	public static SgtRaycastDepth CreateDepthRaycast(int layer = 0, Transform parent = null)
	{
		return CreateDepthRaycast(layer, parent, Vector3.zero, Quaternion.identity, Vector3.one);
	}

	public static SgtRaycastDepth CreateDepthRaycast(int layer, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
	{
		var gameObject = SgtHelper.CreateGameObject("Raycast Depth", layer, parent, localPosition, localRotation, localScale);
		var flare      = gameObject.AddComponent<SgtRaycastDepth>();

		return flare;
	}

#if UNITY_EDITOR
	[MenuItem(SgtHelper.GameObjectMenuPrefix + "Raycast Depth", false, 10)]
	public static void CreateDepthRaycastMenuItem()
	{
		var parent       = SgtHelper.GetSelectedParent();
		var depthRaycast = CreateDepthRaycast(parent != null ? parent.gameObject.layer : 0, parent);

		SgtHelper.SelectAndPing(depthRaycast);
	}
#endif

	protected override float DoCalculate(Vector3 eye, Vector3 target)
	{
		var coverage = 0.0f;

		if (MaxThickness > 0.0f)
		{
			var direction = Vector3.Normalize(target - eye);
			var magnitude = Vector3.Distance(eye, target);
			var hitA      = default(RaycastHit);

			// Raycast forward
			if (Physics.Raycast(eye, direction, out hitA, magnitude, Layers) == true)
			{
				var hitB = default(RaycastHit);
				
				// One side hit, so assume max coverage
				coverage = 1.0f;

				// Raycast backward
				if (Physics.Raycast(target, -direction, out hitB, magnitude, Layers) == true)
				{
					var thickness = Vector3.Distance(hitA.point, hitB.point);
					
					// If we raycast through less than the MaxThickness, we have partial coverage
					if (thickness < MaxThickness)
					{
						coverage = thickness / MaxThickness;
					}
				}
			}
		}

		return coverage;
	}
}