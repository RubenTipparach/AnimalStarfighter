using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(SgtSpacetimeBombs))]
public class SgtSpacetimeBombs_Editor : SgtEditor<SgtSpacetimeBombs>
{
	protected override void OnInspector()
	{
		DrawDefault("Requires");
		BeginError(Any(t => t.BombPrefab == null));
			DrawDefault("BombPrefab");
		EndError();
		BeginError(Any(t => t.BombPrefab == null));
			DrawDefault("Spacetime");
		EndError();
	}
}
#endif

// This component spawns spacetime bombs when you click the button
[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Spacetime Bombs")]
public class SgtSpacetimeBombs : MonoBehaviour
{
	[Tooltip("The key required to spawn bombs")]
	public KeyCode Requires = KeyCode.Space;
	
	[Tooltip("The bomb prefab that will be spawned")]
	public GameObject BombPrefab;

	[Tooltip("The spacetime we want these bombs to effect")]
	public SgtSpacetime Spacetime;

	public void SpawnBomb()
	{
		if (BombPrefab != null && Spacetime != null)
		{
			var bomb     = SgtHelper.CloneGameObject(BombPrefab, transform).GetComponent<SgtSpacetimeBomb>();
			var position = new Vector3(Random.Range(-5.0f, 5.0f), 0.0f, Random.Range(-5.0f, 5.0f));

			bomb.Spacetime = Spacetime;

			bomb.transform.localPosition = position;
		}
	}

	protected virtual void Update()
	{
		if (Input.GetKeyDown(Requires) == true)
		{
			SpawnBomb();
		}
	}
}
