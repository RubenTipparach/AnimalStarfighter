using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(SgtKeyboardMove))]
public class SgtKeyboardMove_Editor : SgtEditor<SgtKeyboardMove>
{
	protected override void OnInspector()
	{
		DrawDefault("Require");
		BeginError(Any(t => t.Speed <= 0.0f));
			DrawDefault("Speed");
		EndError();
		BeginError(Any(t => t.Dampening < 0.0f));
			DrawDefault("Dampening");
		EndError();

		Separator();

		DrawDefault("CheckTerrains");

		if (Any(t => t.CheckTerrains == true))
		{
			BeginIndent();
				DrawDefault("RepelDistance");
				BeginError(Any(t => t.SlowSpeed <= 0.0f));
					DrawDefault("SlowSpeed");
				EndError();
				BeginError(Any(t => t.SlowThickness <= 0.0f));
					DrawDefault("SlowThickness");
				EndError();
			EndIndent();
		}
	}
}
#endif

// This component handles keyboard movement when attached to the camera
[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Keyboard Move")]
public class SgtKeyboardMove : MonoBehaviour
{
	[Tooltip("The key that needs to be held down to move")]
	public KeyCode Require = KeyCode.None;
	
	[Tooltip("The maximum speed of the movement")]
	public float Speed = 1.0f;
	
	[Tooltip("How sharp the movements are")]
	public float Dampening = 5.0f;

	[Tooltip("Dampen speed when near SgtTerrains?")]
	public bool CheckTerrains = true;

	[Tooltip("The minimum distance to the surface of a terrain you can be in world space")]
	public float RepelDistance = 0.1f;

	[Tooltip("The speed multiplier when near the surface of a terrain")]
	public float SlowSpeed = 0.1f;

	[Tooltip("How thick the dampening field around each terrain is in world space")]
	public float SlowThickness = 10.0f;
	
	// The remaining position that this GameObject must move toward
	private Vector3 offset;
	
	protected virtual void OnEnable()
	{
		offset = Vector3.zero;
	}
	
	protected virtual void Update()
	{
		var maxSpeed = CalculateMaxSpeed();
		
		if (Require == KeyCode.None || Input.GetKey(Require) == true)
		{
			offset += transform.forward * Input.GetAxisRaw("Vertical") * maxSpeed * Time.deltaTime;
			
			offset += transform.right * Input.GetAxisRaw("Horizontal") * maxSpeed * Time.deltaTime;
		}

		// Dampen current position to target position
		var oldPosition    = transform.position;
		var targetPosition = transform.position + offset;
		var newPosition    = SgtHelper.Dampen3(oldPosition, targetPosition, Dampening, Time.deltaTime, 0.1f);
		
		transform.position = newPosition;
		
		// Reduce offset by dampen amount
		offset -= newPosition - oldPosition;

		// Make sure the position isn't inside a terrain
		RepelPositions();
	}

	private float CalculateMaxSpeed()
	{
		var maxSpeed       = Speed;
		var targetPosition = transform.position + offset;
		
		if (CheckTerrains == true && SlowThickness > 0.0f)
		{
			for (var i = SgtTerrain.AllTerrains.Count - 1; i >= 0; i--)
			{
				var terrain      = SgtTerrain.AllTerrains[i];
				var height       = terrain.GetSurfaceHeightWorld(targetPosition);
				var targetVector = targetPosition - terrain.transform.position;
				var height01     = Mathf.Clamp01((targetVector.magnitude - height) / SlowThickness);
				var newSpeed     = Mathf.SmoothStep(SlowSpeed, Speed, height01);
				
				if (newSpeed < maxSpeed)
				{
					maxSpeed = newSpeed;
				}
			}
		}

		return maxSpeed;
	}

	private void RepelPositions()
	{
		if (CheckTerrains == true)
		{
			var targetPosition = transform.position + offset;

			for (var i = SgtTerrain.AllTerrains.Count - 1; i >= 0; i--)
			{
				var terrain      = SgtTerrain.AllTerrains[i];
				var height       = terrain.GetSurfaceHeightWorld(targetPosition) + RepelDistance;
				var targetVector = targetPosition - terrain.transform.position;
				var localVector  = transform.position - terrain.transform.position;

				if (targetVector.magnitude < height)
				{
					targetPosition = terrain.transform.position + targetVector.normalized * height;
				}

				if (localVector.magnitude < height)
				{
					transform.position = terrain.transform.position + localVector.normalized * height;
				}
			}
		}
	}
}