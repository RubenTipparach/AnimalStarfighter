using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(SgtLightningSpawner))]
public class SgtLightningSpawner_Editor : SgtEditor<SgtLightningSpawner>
{
	protected override void OnInspector()
	{
		var updateMesh = false;

		BeginError(Any(t => t.DelayMin > t.DelayMax));
			DrawDefault("DelayMin");
			DrawDefault("DelayMax");
		EndError();

		Separator();

		BeginError(Any(t => t.LifeMin > t.LifeMax));
			DrawDefault("LifeMin");
			DrawDefault("LifeMax");
		EndError();

		Separator();
		
		BeginError(Any(t => t.Radius <= 0.0f));
			DrawDefault("Radius", ref updateMesh);
		EndError();
		BeginError(Any(t => t.Size < 0.0f));
			DrawDefault("Size", ref updateMesh);
		EndError();
		BeginError(Any(t => t.Detail <= 0.0f));
			DrawDefault("Detail", ref updateMesh);
		EndError();
		DrawDefault("Colors");
		DrawDefault("Brightness");
		BeginError(Any(t => t.Sprites == null || t.Sprites.Count == 0));
			DrawDefault("Sprites");
		EndError();

		if (updateMesh == true) DirtyEach(t => t.UpdateMesh());
	}
}
#endif

[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Lightning Spawner")]
public class SgtLightningSpawner : MonoBehaviour
{
	// All active and enabled lightning spawners
	public List<SgtLightningSpawner> AllLightningSpawner = new List<SgtLightningSpawner>();

	[Tooltip("The minimum delay between lightning spawns")]
	public float DelayMin = 0.25f;

	[Tooltip("The maximum delay between lightning spawns")]
	public float DelayMax = 5.0f;

	[Tooltip("The minimum life of each spawned lightning")]
	public float LifeMin = 0.5f;

	[Tooltip("The maximum life of each spawned lightning")]
	public float LifeMax = 1.0f;
	
	[Tooltip("The radius of the spawned lightning mesh in local coordinates")]
	public float Radius = 1.0f;

	[Tooltip("The size of the lightning in degrees")]
	public float Size = 10.0f;

	[Tooltip("The amount of rows and columns in the lightning mesh")]
	[Range(1, 100)]
	public int Detail = 10;
	
	[Tooltip("The random color of the lightning")]
	public Gradient Colors;

	[Tooltip("The brightness of the lightning")]
	public float Brightness = 1.0f;

	[Tooltip("The random sprite used by the lightning")]
	public List<Sprite> Sprites;
	
	[System.NonSerialized]
	public Mesh Mesh;

	// When this reaches 0 a new lightning is spawned
	[System.NonSerialized]
	private float cooldown;

	[SerializeField]
	[HideInInspector]
	private bool startCalled;
	
	[System.NonSerialized]
	private bool updateMeshCalled;

	private static GradientColorKey[] defaultColors = new GradientColorKey[] { new GradientColorKey(Color.white, 0.4f), new GradientColorKey(Color.cyan, 0.6f) };

	public Sprite RandomSprite
	{
		get
		{
			if (Sprites != null)
			{
				var count = Sprites.Count;

				if (count > 0)
				{
					var index = Random.Range(0, count);

					return Sprites[index];
				}
			}

			return null;
		}
	}

	public Color RandomColor
	{
		get
		{
			if (Colors != null)
			{
				return Colors.Evaluate(Random.value);
			}

			return Color.white;
		}
	}

	[ContextMenu("Update Mesh")]
	public void UpdateMesh()
	{
		if (Mesh == null)
		{
			Mesh = SgtHelper.CreateTempMesh("Lightning");
		}
		else
		{
			Mesh.Clear();
		}

		var detailAddOne = Detail + 1;
		var positions    = new Vector3[detailAddOne * detailAddOne];
		var coords       = new Vector2[detailAddOne * detailAddOne];
		var indices      = new int[Detail * Detail * 6];
		var invDetail    = SgtHelper.Reciprocal(Detail);

		for (var y = 0; y < detailAddOne; y++)
		{
			for (var x = 0; x < detailAddOne; x++)
			{
				var vertex = x + y * detailAddOne;
				var fracX  = x * invDetail;
				var fracY  = y * invDetail;
				var angX   = (fracX - 0.5f) * Size;
				var angY   = (fracY - 0.5f) * Size;

				// TODO: Manually do this rotation
				positions[vertex] = Quaternion.Euler(angX, angY, 0.0f) * new Vector3(0.0f, 0.0f, Radius);

				coords[vertex] = new Vector2(fracX, fracY);
			}
		}

		for (var y = 0; y < Detail; y++)
		{
			for (var x = 0; x < Detail; x++)
			{
				var index  = (x + y * Detail) * 6;
				var vertex = x + y * detailAddOne;

				indices[index + 0] = vertex;
				indices[index + 1] = vertex + 1;
				indices[index + 2] = vertex + detailAddOne;
				indices[index + 3] = vertex + detailAddOne + 1;
				indices[index + 4] = vertex + detailAddOne;
				indices[index + 5] = vertex + 1;
			}
		}

		Mesh.vertices  = positions;
		Mesh.uv        = coords;
		Mesh.triangles = indices;
	}

	public SgtLightning Spawn()
	{
		if (Mesh != null && LifeMin > 0.0f && LifeMax > 0.0f)
		{
			var sprite = RandomSprite;

			if (sprite != null)
			{
				var lightning = SgtLightning.Create(this);
				var material  = lightning.Material;
				var uv        = SgtHelper.CalculateSpriteUV(sprite);

				if (material == null)
				{
					material = SgtHelper.CreateTempMaterial("Lightning (Generated)", SgtHelper.ShaderNamePrefix + "Lightning");

					lightning.SetMaterial(material);
				}

				lightning.Life = Random.Range(LifeMin, LifeMax);
				lightning.Age  = 0.0f;

				lightning.SetMesh(Mesh);
				
                material.SetTexture("_MainTex", sprite.texture);
				material.SetColor("_Color", SgtHelper.Brighten(RandomColor, Brightness));
				material.SetFloat("_Age", 0.0f);
				material.SetVector("_Offset", new Vector2(uv.x, uv.y));
				material.SetVector("_Scale", new Vector2(uv.z - uv.x, uv.w - uv.y));
				
				lightning.transform.localRotation = Random.rotation;

				return lightning;
			}
		}

		return null;
	}

	public static SgtLightningSpawner CreateLightningSpawner(int layer = 0, Transform parent = null)
	{
		return CreateLightningSpawner(layer, parent, Vector3.zero, Quaternion.identity, Vector3.one);
	}

	public static SgtLightningSpawner CreateLightningSpawner(int layer, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
	{
		var gameObject       = SgtHelper.CreateGameObject("Lightning Spawner", layer, parent, localPosition, localRotation, localScale);
		var lightningSpawner = gameObject.AddComponent<SgtLightningSpawner>();

		return lightningSpawner;
	}

#if UNITY_EDITOR
	[MenuItem(SgtHelper.GameObjectMenuPrefix + "Lightning Spawner", false, 10)]
	public static void CreateLightningSpawnerMenuItem()
	{
		var parent           = SgtHelper.GetSelectedParent();
		var lightningSpawner = CreateLightningSpawner(parent != null ? parent.gameObject.layer : 0, parent);

		SgtHelper.SelectAndPing(lightningSpawner);
	}
#endif

	protected virtual void Awake()
	{
		ResetDelay();
    }

	protected virtual void OnEnable()
	{
		if (startCalled == true)
		{
			CheckUpdateCalls();
		}
	}

	protected virtual void Start()
	{
		if (startCalled == false)
		{
			startCalled = true;

			if (Colors == null)
			{
				Colors = new Gradient();
				Colors.colorKeys = defaultColors;
			}

			CheckUpdateCalls();
		}
	}

	protected virtual void Update()
	{
		cooldown -= Time.deltaTime;

		// Spawn new lightning?
		if (cooldown <= 0.0f)
		{
			ResetDelay();

			Spawn();
        }
	}

	protected virtual void OnDestroy()
	{
		SgtObjectPool<Mesh>.Add(Mesh, m => m.Clear());
	}

	private void ResetDelay()
	{
		cooldown = Random.Range(DelayMin, DelayMax);
	}

	private void CheckUpdateCalls()
	{
		if (updateMeshCalled == false)
		{
			UpdateMesh();
		}
	}
}