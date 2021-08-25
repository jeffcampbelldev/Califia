using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PortalCam : MonoBehaviour
{
	public Transform _refCam;
	public Transform _refDoor;
	public Transform _myDoor;
	public Material _blendMat;

	[HideInInspector]
	public AnimationPath _path;
    // Start is called before the first frame update
    void Start()
    {
		_blendMat.SetFloat("_BlendAmount",0f);
    }

    // Update is called once per frame
    void Update()
    {
		//match angle
		Vector3 eulDif = _refCam.eulerAngles-_refDoor.eulerAngles;
		transform.eulerAngles=eulDif+_myDoor.eulerAngles;
		
		//match position
		Vector3 offset = _refCam.position-_refDoor.position;
		offset = Quaternion.Inverse(_refDoor.rotation)*offset;
		transform.position=_myDoor.rotation*offset+_myDoor.position;
    }

	void OnTriggerEnter(Collider other){
		if(other.name=="PortalComplete"){
			Debug.Log("Doing it!");
			_refCam.position=transform.position;
			_refCam.rotation=transform.rotation;
			_path._forward=transform.forward;
			this.enabled=false;
		}
	}
}
