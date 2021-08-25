using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PipHud : MonoBehaviour
{
	Canvas _can;
	public Camera _pipCam;
	public BoxCollider _pipCanBlocker;
    // Start is called before the first frame update
    void Start()
    {
		_can = GetComponent<Canvas>(); 
		_can.enabled=false;
		_pipCam.enabled=false;
		_pipCanBlocker.enabled=false;
    }

    // Update is called once per frame
    void Update()
    {
    }

	public void Activate(bool act){
		if(_can==null||_pipCam==null)
			return;
		_can.enabled=act;
		_pipCam.enabled=act;
		_pipCanBlocker.enabled=act;
	}
}
