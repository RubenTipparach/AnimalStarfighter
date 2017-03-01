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

	GvrPointerInputModule inputModule;

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
            magnetSensor.OnCardboardTrigger += Move;
            pointer = transform.GetChild(0);

			inputModule = (GameObject.Find("GvrEventSystem") as GameObject).GetComponent<GvrPointerInputModule>();
			inputModule.triggerLong.AddListener(new UnityAction(Move));
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
        }
	}

    public void Move()
    {
        if (isLocalPlayer)
        {
            transform.position += pointer.forward * Time.deltaTime;
			// Debug.Log("Move stuff");
        }
    }
}
