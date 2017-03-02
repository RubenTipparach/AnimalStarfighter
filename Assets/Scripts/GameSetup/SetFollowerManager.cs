using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetFollowerManager : MonoBehaviour {

	[SerializeField]
	List<SgtDebrisSpawner> debrises;

	// Use this for initialization
	void Start () {
		
    }
	
	// Update is called once per frame
	void Update () {
		
	}

	public void SetFollower(Transform followee)
	{
		foreach (var debris in debrises)
		{
			debris.Follower = followee;
		}
	}
}
