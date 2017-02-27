using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEditor;


public class EnemyEditor : EditorWindow
{
	public EnemyTypes enemyTypes;
	private int viewIndex = 1;

	[MenuItem("Window/Inventory Item Editor %#e")]
	static void Init()
	{
		EditorWindow.GetWindow(typeof(EnemyEditor));
	}

	void OnEnable()
	{
		if (EditorPrefs.HasKey("ObjectPath"))
		{
			string objectPath = EditorPrefs.GetString("ObjectPath");
			enemyTypes = AssetDatabase.LoadAssetAtPath(objectPath, typeof(EnemyTypes)) as EnemyTypes;
		}

	}

	void OnGUI()
	{
		GUILayout.BeginHorizontal();
		GUILayout.Label("Inventory Item Editor", EditorStyles.boldLabel);
		if (enemyTypes != null)
		{
			if (GUILayout.Button("Show Item List"))
			{
				EditorUtility.FocusProjectWindow();
				Selection.activeObject = enemyTypes;
			}
		}
		if (GUILayout.Button("Open Item List"))
		{
			Openenemies();
		}
		if (GUILayout.Button("New Item List"))
		{
			EditorUtility.FocusProjectWindow();
			Selection.activeObject = enemyTypes;
		}
		GUILayout.EndHorizontal();

		if (enemyTypes == null)
		{
			GUILayout.BeginHorizontal();
			GUILayout.Space(10);
			if (GUILayout.Button("Create New Item List", GUILayout.ExpandWidth(false)))
			{
				CreateNewenemies();
			}
			if (GUILayout.Button("Open Existing Item List", GUILayout.ExpandWidth(false)))
			{
				Openenemies();
			}
			GUILayout.EndHorizontal();
		}

		GUILayout.Space(20);

		if (enemyTypes != null)
		{
			GUILayout.BeginHorizontal();

			GUILayout.Space(10);

			if (GUILayout.Button("Prev", GUILayout.ExpandWidth(false)))
			{
				if (viewIndex > 1)
					viewIndex--;
			}
			GUILayout.Space(5);
			if (GUILayout.Button("Next", GUILayout.ExpandWidth(false)))
			{
				if (viewIndex < enemyTypes.enemies.Count)
				{
					viewIndex++;
				}
			}

			GUILayout.Space(60);

			if (GUILayout.Button("Add Item", GUILayout.ExpandWidth(false)))
			{
				AddItem();
			}
			if (GUILayout.Button("Delete Item", GUILayout.ExpandWidth(false)))
			{
				DeleteItem(viewIndex - 1);
			}

			GUILayout.EndHorizontal();
			if (enemyTypes.enemies == null)
				Debug.Log("wtf");
			if (enemyTypes.enemies.Count > 0)
			{
				GUILayout.BeginHorizontal();
				viewIndex = Mathf.Clamp(EditorGUILayout.IntField("Current Item", viewIndex, GUILayout.ExpandWidth(false)), 1, enemyTypes.enemies.Count);
				//Mathf.Clamp (viewIndex, 1, inventoryenemies.enemies.Count);
				EditorGUILayout.LabelField("of   " + enemyTypes.enemies.Count.ToString() + "  items", "", GUILayout.ExpandWidth(false));
				GUILayout.EndHorizontal();

				enemyTypes.enemies[viewIndex - 1].itemName = EditorGUILayout.TextField("Item Name", enemyTypes.enemies[viewIndex - 1].itemName as string);
				enemyTypes.enemies[viewIndex - 1].itemIcon = EditorGUILayout.ObjectField("Item Icon", enemyTypes.enemies[viewIndex - 1].itemIcon, typeof(Texture2D), false) as Texture2D;
				enemyTypes.enemies[viewIndex - 1].itemObject = EditorGUILayout.ObjectField("Item Object", enemyTypes.enemies[viewIndex - 1].itemObject, typeof(Rigidbody), false) as Rigidbody;

				GUILayout.Space(10);

				GUILayout.BeginHorizontal();
				enemyTypes.enemies[viewIndex - 1].isUnique = (bool)EditorGUILayout.Toggle("Unique", enemyTypes.enemies[viewIndex - 1].isUnique, GUILayout.ExpandWidth(false));
				enemyTypes.enemies[viewIndex - 1].isIndestructible = (bool)EditorGUILayout.Toggle("Indestructable", enemyTypes.enemies[viewIndex - 1].isIndestructible, GUILayout.ExpandWidth(false));
				enemyTypes.enemies[viewIndex - 1].isQuestItem = (bool)EditorGUILayout.Toggle("QuestItem", enemyTypes.enemies[viewIndex - 1].isQuestItem, GUILayout.ExpandWidth(false));
				GUILayout.EndHorizontal();

				GUILayout.Space(10);

				GUILayout.BeginHorizontal();
				enemyTypes.enemies[viewIndex - 1].isStackable = (bool)EditorGUILayout.Toggle("Stackable ", enemyTypes.enemies[viewIndex - 1].isStackable, GUILayout.ExpandWidth(false));
				enemyTypes.enemies[viewIndex - 1].destroyOnUse = (bool)EditorGUILayout.Toggle("Destroy On Use", enemyTypes.enemies[viewIndex - 1].destroyOnUse, GUILayout.ExpandWidth(false));
				enemyTypes.enemies[viewIndex - 1].encumbranceValue = EditorGUILayout.FloatField("Encumberance", enemyTypes.enemies[viewIndex - 1].encumbranceValue, GUILayout.ExpandWidth(false));
				GUILayout.EndHorizontal();

				GUILayout.Space(10);

			}
			else
			{
				GUILayout.Label("This Inventory List is Empty.");
			}
		}
		if (GUI.changed)
		{
			EditorUtility.SetDirty(enemyTypes);
		}
	}

	void CreateNewenemies()
	{
		// There is no overwrite protection here!
		// There is No "Are you sure you want to overwrite your existing object?" if it exists.
		// This should probably get a string from the user to create a new name and pass it ...
		viewIndex = 1;
		enemyTypes = CreateEnemyList.Create();
		if (enemyTypes)
		{
			enemyTypes.enemies = new List<EnemyAttributes>();
			string relPath = AssetDatabase.GetAssetPath(enemyTypes);
			EditorPrefs.SetString("ObjectPath", relPath);
		}
	}

	void Openenemies()
	{
		string absPath = EditorUtility.OpenFilePanel("Select Inventory Item List", "", "");
		if (absPath.StartsWith(Application.dataPath))
		{
			string relPath = absPath.Substring(Application.dataPath.Length - "Assets".Length);
			enemyTypes = AssetDatabase.LoadAssetAtPath(relPath, typeof(EnemyTypes)) as EnemyTypes;
			if (enemyTypes.enemies == null)
				enemyTypes.enemies = new List<EnemyAttributes>();
			if (enemyTypes)
			{
				EditorPrefs.SetString("ObjectPath", relPath);
			}
		}
	}

	void AddItem()
	{
		EnemyAttributes newItem = new EnemyAttributes();
		newItem.itemName = "New Item";
		enemyTypes.enemies.Add(newItem);
		viewIndex = enemyTypes.enemies.Count;
	}

	void DeleteItem(int index)
	{
		enemyTypes.enemies.RemoveAt(index);
	}
}