using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(SgtCustomStarfield))]
public class SgtCustomStarfield_Editor : SgtPointStarfield_Editor<SgtCustomStarfield>
{
	protected override void OnInspector()
	{
		var updateMaterial        = false;
		var updateMeshesAndModels = false;
		
		DrawMaterial(ref updateMaterial);

		Separator();

		DrawAtlas(ref updateMaterial, ref updateMeshesAndModels);

		Separator();

		DrawPointMaterial(ref updateMaterial);

		Separator();
		
		DrawDefault("Stars", ref updateMeshesAndModels);
		
		RequireObserver();

		serializedObject.ApplyModifiedProperties();

		if (updateMaterial        == true) DirtyEach(t => t.UpdateMaterial       ());
		if (updateMeshesAndModels == true) DirtyEach(t => t.UpdateMeshesAndModels());
	}
}
#endif

[ExecuteInEditMode]
[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Custom Starfield")]
public class SgtCustomStarfield : SgtPointStarfield
{
	[Tooltip("The stars that will be rendered by this starfield")]
	public List<SgtPointStar> Stars;

	public static SgtCustomStarfield CreateCustomStarfield(int layer = 0, Transform parent = null)
	{
		return CreateCustomStarfield(layer, parent, Vector3.zero, Quaternion.identity, Vector3.one);
	}

	public static SgtCustomStarfield CreateCustomStarfield(int layer, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
	{
		var gameObject = SgtHelper.CreateGameObject("Custom Starfield", layer, parent, localPosition, localRotation, localScale);
		var starfield  = gameObject.AddComponent<SgtCustomStarfield>();

		return starfield;
	}
	
#if UNITY_EDITOR
	[MenuItem(SgtHelper.GameObjectMenuPrefix + "Custom Starfield", false, 10)]
	private static void CreateCustomStarfieldMenuItem()
	{
		var parent    = SgtHelper.GetSelectedParent();
		var starfield = CreateCustomStarfield(parent != null ? parent.gameObject.layer : 0, parent);

		SgtHelper.SelectAndPing(starfield);
	}
#endif
	
	protected override void OnDestroy()
	{
		base.OnDestroy();

		if (Stars != null)
		{
			for (var i = Stars.Count - 1; i >= 0; i--)
			{
				SgtClassPool<SgtPointStar>.Add(Stars[i]);
			}
		}
	}

	protected override int BeginQuads()
	{
		if (Stars != null)
		{
			return Stars.Count;
		}

		return 0;
	}

	protected override void NextQuad(ref SgtPointStar quad, int starIndex)
	{
		quad.CopyFrom(Stars[starIndex]);
	}

	protected override void EndQuads()
	{
	}
}