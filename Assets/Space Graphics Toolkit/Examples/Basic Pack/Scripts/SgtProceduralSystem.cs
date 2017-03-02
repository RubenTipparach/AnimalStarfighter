using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(SgtProceduralSystem))]
public class SgtProceduralSystem_Editor : SgtEditor<SgtProceduralSystem>
{
	protected override void OnInspector()
	{
		DrawDefault("SphereMesh");
		DrawDefault("MainLight");
		DrawDefault("StarMaterials");
		DrawDefault("PlanetMaterials");
		DrawDefault("MoonMaterials");
		DrawDefault("JovianTextures");
	}
}
#endif

// This component will make a basic star system based on a bunch of preset materials, textures, and random values
[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Procedural System")]
public class SgtProceduralSystem : MonoBehaviour
{
	[Tooltip("The sphere mesh used by all stars, planets, etc")]
	public Mesh SphereMesh;

	[Tooltip("The lightshining on all stars, planets, etc")]
	public Light MainLight;

	[Tooltip("The star surface materials")]
	public List<Material> StarMaterials = new List<Material>();

	[Tooltip("The planet surface materials")]
	public List<Material> PlanetMaterials = new List<Material>();

	[Tooltip("The moon surface materials")]
	public List<Material> MoonMaterials = new List<Material>();

	[Tooltip("The gas giant surface cubemap textures")]
	public List<Cubemap> JovianTextures = new List<Cubemap>();

	public List<GameObject> generatedGameObjects = new List<GameObject>();

	[ContextMenu("Clear")]
	public void Clear()
	{
		for (var i = generatedGameObjects.Count - 1; i >= 0; i--)
		{
			SgtHelper.Destroy(generatedGameObjects[i]);
		}

		generatedGameObjects.Clear();
	}

	[ContextMenu("Add Star")]
	public void AddStar()
	{
		var gameObject   = AddBasicGameObject("Star", transform, 0.0f, 0.0f, 0.0f, 10.0f, 0.5f, 1.0f);
		var meshFilter   = gameObject.AddComponent<MeshFilter>();
		var meshRenderer = gameObject.AddComponent<MeshRenderer>();
		var atmosphere   = gameObject.AddComponent<SgtAtmosphere>();

		//corona.InnerPower = Random.Range(1.0f, 2.0f);

		meshFilter.sharedMesh = SphereMesh;

		meshRenderer.sharedMaterial = GetRandomElement(StarMaterials);

		atmosphere.InnerRenderers = new List<MeshRenderer>();
		atmosphere.InnerRenderers.Add(meshRenderer);

		atmosphere.OuterMeshes = new List<Mesh>();
		atmosphere.OuterMeshes.Add(SphereMesh);
	}

	[ContextMenu("Add Planet")]
	public void AddPlanet()
	{
		var gameObject   = AddBasicGameObject("Planet", transform, 5.0f, 30.0f, -5.0f, 10.0f, 0.5f, 1.0f);
		var meshFilter   = gameObject.AddComponent<MeshFilter>();
		var meshRenderer = gameObject.AddComponent<MeshRenderer>();
		var atmosphere   = gameObject.AddComponent<SgtAtmosphere>();

		meshFilter.sharedMesh = SphereMesh;

		meshRenderer.sharedMaterial = GetRandomElement(PlanetMaterials);

		//atmosphere.InnerPower = Random.Range(1.0f, 2.0f);

		atmosphere.Lights = new List<Light>();
		atmosphere.Lights.Add(MainLight);

		atmosphere.OuterMeshes = new List<Mesh>();
		atmosphere.OuterMeshes.Add(SphereMesh);

		// Add moons?
		for (var i = Random.Range(0,2); i >= 0; i--)
		{
			AddMoon(gameObject.transform);
		}
	}
	
	public void AddMoon(Transform parent)
	{
		var gameObject   = AddBasicGameObject("Moon", parent, 1.0f, 3.0f, -30.0f, 30.0f, 0.05f, 0.2f);
		var meshFilter   = gameObject.AddComponent<MeshFilter>();
		var meshRenderer = gameObject.AddComponent<MeshRenderer>();

		meshFilter.sharedMesh = SphereMesh;

		meshRenderer.sharedMaterial = GetRandomElement(MoonMaterials);
	}

	[ContextMenu("Add Jovian")]
	public void AddJovian()
	{
		var gameObject = AddBasicGameObject("Jovian", transform, 5.0f, 30.0f, -5.0f, 10.0f, 0.5f, 1.0f);
		var jovian     = gameObject.AddComponent<SgtJovian>();

		jovian.MainTex = GetRandomElement(JovianTextures);

		jovian.Lights = new List<Light>();
		jovian.Lights.Add(MainLight);

		jovian.Meshes = new List<Mesh>();
		jovian.Meshes.Add(SphereMesh);
	}

	public GameObject AddBasicGameObject(string name, Transform parent, float minOrbitDistance, float maxOrbitDistance, float minRotationSpeed, float maxRotationSpeed, float minScale, float maxScale)
	{
		// Create GO
		var gameObject  = new GameObject(name);
		var simpleOrbit = gameObject.AddComponent<SgtSimpleOrbit>();
		var rotate      = gameObject.AddComponent<SgtRotate>();
		var scale       = Random.Range(maxScale, maxScale);

		gameObject.transform.parent = parent;

		gameObject.transform.localScale = new Vector3(scale, scale, scale);

		// Setup orbit
		simpleOrbit.Angle = Random.Range(0.0f, 360.0f);

		simpleOrbit.Radius = Random.Range(minOrbitDistance, maxOrbitDistance);

		simpleOrbit.DegreesPerSecond = Random.Range(minRotationSpeed, maxRotationSpeed);

		// Setup rotation
		rotate.DegreesPerSecond = new Vector3(0.0f, Random.Range(minRotationSpeed, maxRotationSpeed), 0.0f);

		// Add to list and return
		generatedGameObjects.Add(gameObject);

		return gameObject;
	}

	protected virtual void Awake()
	{
		Clear();

		if (SphereMesh != null)
		{
			AddStar();

			for (var i = Random.Range(1, 6); i >= 0; i--)
			{
				AddPlanet();
			}

			for (var i = Random.Range(1, 4); i >= 0; i--)
			{
				AddJovian();
			}
		}
	}

	private T GetRandomElement<T>(List<T> list)
	{
		if (list != null && list.Count > 0)
		{
			var index = Random.Range(0, list.Count - 1);

			return list[index];
		}

		return default(T);
	}
}
