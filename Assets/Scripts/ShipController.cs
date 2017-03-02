using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

public class ShipController : NetworkBehaviour
{


    [SerializeField]
    Camera myCamera;

    [SerializeField]
    AudioListener audioListener;

    [SerializeField]
    MagnetSensor magnetSensor; // there porbablt exists a function for this, just bypassing for now.

    [SerializeField]
    GvrAudioListener gaList;

    Transform pointer;

	GvrPointerInputModule inputModule; //its a little harder without touch controls, some vr only have magnets

	bool toggleMove = false;

    // Use this for initialization
	void Start ()
    {
        if (isLocalPlayer)
        {
            myCamera.enabled = true;
            audioListener.enabled = true;
            magnetSensor.enabled = true;
            gaList.enabled = true;
            GvrViewer.AddStereoControllerToCameras();

			magnetSensor.OnCardboardTrigger += ToggleMove;
			magnetSensor.OnCardboardTrigger += CmdTestServerCall;

			pointer = transform.GetChild(0);

			inputModule = (GameObject.Find("GvrEventSystem") as GameObject).GetComponent<GvrPointerInputModule>();
			inputModule.triggerLong.AddListener(new UnityAction(Move));

			SetFollowerManager follower = (GameObject.Find("Observer") as GameObject).GetComponent<SetFollowerManager>();
			follower.SetFollower(transform);
        }
    }
	
	// Update is called once per frame
	void Update ()
    {
		if(isLocalPlayer)
        {
            if(Input.GetKey(KeyCode.W))
            {
                Move();
            }

			if(toggleMove)
			{
				Move();
			}
        }
	}

	[Command]
	void CmdTestServerCall()
	{
		//Debug.Log("User id " +this.netId + " called");
	}

	[Command]
	void CmdDoFire(float lifeTime)
	{
	}

    public void Move()
    {
        if (isLocalPlayer)
        {
			//CmdTestServerCall();
			transform.position += pointer.forward * Time.deltaTime;
			// Debug.Log("Move stuff");
        }
    }

	/// <summary>
	/// Maybe a double tap :)
	/// </summary>
	public void ToggleMove()
	{
		if (isLocalPlayer)
		{
			toggleMove = !toggleMove;
        }
	}
}
