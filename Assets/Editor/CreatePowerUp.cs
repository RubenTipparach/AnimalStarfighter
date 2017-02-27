using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public class CreatePowerUp
{
	[MenuItem("Assets/Create/PowerUp")]
	public static PowerUp Create()
	{
		PowerUp asset = ScriptableObject.CreateInstance<PowerUp>();

		AssetDatabase.CreateAsset(asset, "Assets/PowerUp.asset");
		AssetDatabase.SaveAssets();
		return asset;
	}
}

