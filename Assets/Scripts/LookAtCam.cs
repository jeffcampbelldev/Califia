using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LookAtCam : MonoBehaviour
{
	Transform _cam;
	public bool _lookAway;
	// Start is called before the first frame update
	void Start()
	{
		GameObject cam = GameObject.Find("DefaultCamera");
		if(cam!=null)
			_cam = cam.transform;
	}

	/*
	void OnSceneLoaded(Scene scene, LoadSceneMode mode){
		_cam = Camera.main.transform;
	}

	void OnSceneUnloaded(Scene scene){
		_cam = Camera.main.transform;
	}
	*/

	// Update is called once per frame
	void Update()
	{
		if(_cam!=null)
		{
			transform.LookAt(_cam);
			if(_lookAway)
				transform.forward*=-1f;
		}
	}
}
