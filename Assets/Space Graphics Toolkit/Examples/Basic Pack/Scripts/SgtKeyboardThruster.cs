using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(SgtKeyboardThruster))]
public class SgtKeyboardThruster_Editor : SgtEditor<SgtKeyboardThruster>
{
	protected override void OnInspector()
	{
		DrawDefault("RearThrusters");
		DrawDefault("LeftThrusters");
		DrawDefault("RightThrusters");
	}
}
#endif

// This component handles keyboard controls of thrusters
[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Keyboard Thruster")]
public class SgtKeyboardThruster : MonoBehaviour
{
	[Tooltip("The thrusters pointing out the back")]
	public List<SgtThruster> RearThrusters = new List<SgtThruster>();
	
	[Tooltip("The thrusters pointing out the left")]
	public List<SgtThruster> LeftThrusters = new List<SgtThruster>();
	
	[Tooltip("The thrusters pointing out the right")]
	public List<SgtThruster> RightThrusters = new List<SgtThruster>();
	
	protected virtual void Update()
	{
		// Forwards/backwards
		for (var i = 0; i < RearThrusters.Count; i++)
		{
			var thruster = RearThrusters[i];
			
			if (thruster != null)
			{
				thruster.Throttle = Input.GetAxisRaw("Vertical");
			}
		}
		
		// Left/right
		for (var i = 0; i < LeftThrusters.Count; i++)
		{
			var thruster = LeftThrusters[i];
			
			if (thruster != null)
			{
				thruster.Throttle = Input.GetAxisRaw("Horizontal");
			}
		}
		
		// Right/left
		for (var i = 0; i < RightThrusters.Count; i++)
		{
			var thruster = RightThrusters[i];
			
			if (thruster != null)
			{
				thruster.Throttle = 0.0f - Input.GetAxisRaw("Horizontal");
			}
		}
	}
}