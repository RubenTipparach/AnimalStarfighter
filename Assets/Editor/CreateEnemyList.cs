using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class CreateEnemyList {

	[MenuItem("Assets/Create/EnemyType")]
	public static EnemyTypes Create()
	{
		EnemyTypes asset = ScriptableObject.CreateInstance<EnemyTypes>();

		AssetDatabase.CreateAsset(asset, "Assets/EnemyTypes.asset");
		AssetDatabase.SaveAssets();
		return asset;
	}
}
