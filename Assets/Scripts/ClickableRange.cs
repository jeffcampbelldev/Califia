using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ClickableRange : MonoBehaviour
{
	Transform _defaultCam;
	public Transform _altCam;
	Transform _curCam;
	public float _rangeSqr;
	GraphicRaycaster _gr;
    // Start is called before the first frame update
    void Start()
    {
		_defaultCam = FindObjectOfType<TestCam>().transform;
		_curCam=_defaultCam;
		_gr = GetComponent<GraphicRaycaster>();
    }

    // Update is called once per frame
    void Update()
    {
		if(_gr==null || _curCam==null)
			return;
		_gr.enabled=(_curCam.position-transform.position).sqrMagnitude<_rangeSqr;
    }

	public void SetAltCamera(bool alt){
		//set default camera to c
		if(_altCam==null)
			_curCam=_defaultCam;
		else
			_curCam=alt ? _altCam : _defaultCam;
	}

	public void SetAltCamera(Camera c){
		_curCam = c.transform;
	}
}
