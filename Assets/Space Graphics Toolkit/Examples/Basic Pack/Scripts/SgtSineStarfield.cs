using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(SgtSineStarfield))]
public class SgtSineStarfield_Editor : SgtEditor<SgtSineStarfield>
{
	protected override void OnInspector()
	{
		BeginError(Any(t => t.Starfield == null));
			DrawDefault("Starfield");
		EndError();
		DrawDefault("Angle");
		DrawDefault("AngleStep");
		DrawDefault("AnglePerSecond");
		DrawDefault("Position");
		DrawDefault("PositionStep");
		DrawDefault("Amplitude");
	}
}
#endif

// This component turns a custom starfield into a sine wave
[ExecuteInEditMode]
[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Sine Starfield")]
public class SgtSineStarfield : MonoBehaviour
{
	[Tooltip("The starfield we will turn into a sine wave")]
	public SgtCustomStarfield Starfield;

	[Tooltip("The base angle of the sine wave")]
	public float Angle;
	
	[Tooltip("The amount of degrees in the sine wave each star will appear at")]
	public float AngleStep = 5.0f;

	[Tooltip("The amount Angle changes per second")]
	public float AnglePerSecond = 5.0f;

	[Tooltip("The base position of the sine wave")]
	public Vector3 Position;

	[Tooltip("The amount of translation between each star")]
	public Vector3 PositionStep = Vector3.forward;

	[Tooltip("The maximum amplitude of the sine wave")]
	public Vector3 Amplitude = Vector3.up;

	protected virtual void Update()
	{
		if (Application.isPlaying == true)
		{
			Angle += AnglePerSecond * Time.deltaTime;
		}

		if (Starfield != null)
		{
			var stars = Starfield.Stars;

			if (stars != null)
			{
				var currentA = Angle;
				var currentP = Position;

				for (var i = 0; i < stars.Count; i++)
				{
					var star = stars[i];
					
					if (star != null)
					{
						star.Position = currentP + Amplitude * Mathf.Sin(currentA * Mathf.Deg2Rad);
						star.Radius   = 0.05f;
						star.Color    = Color.white;

						currentA +=    AngleStep;
						currentP += PositionStep;
					}
				}

				Starfield.UpdateMeshesAndModels();
			}
		}
	}
}