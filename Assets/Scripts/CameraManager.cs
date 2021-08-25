//CameraManager.cs
//
//Description: Coordinates switching of cameras
//Used in transition to/from mosaic mode
//

//

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
	Knob [] _knobs;
	ClickDetection[] _cd;
	public Camera _gasCam;
	GameObject _defaultCam;
	public GameObject [] _mosaicCams;
	bool _inMosaic;
	public Transform _alaris;
	Canvas [] _alarisCans;
	public Transform _drager;
	Canvas [] _dragerCans;
	public CanvasGroup [] _uiHideOnMosaic;
	ClickableRange [] _clickables;
	CannulaMenu _cans;
	[HideInInspector]
	public Camera _hoffAlt;

    // Start is called before the first frame update
    void Start()
    {
		_defaultCam = FindObjectOfType<TestCam>().gameObject;
		_knobs = FindObjectsOfType<Knob>();
		_clickables = FindObjectsOfType<ClickableRange>();
		_cd = FindObjectsOfType<ClickDetection>();
		_alarisCans = _alaris.GetComponentsInChildren<Canvas>();
		_dragerCans = _drager.GetComponentsInChildren<Canvas>();
		//default cam at start
		_inMosaic=false;
		ActivateMosaic(_inMosaic);
		_cans = FindObjectOfType<CannulaMenu>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

	public void ToggleMosaic(){
		_inMosaic=!_inMosaic;
		ActivateMosaic(_inMosaic);
	}

	public void ActivateMosaic(bool active){
		foreach(ClickDetection cd in _cd)
		{
			if(cd!=null)
				cd.enabled=!active;
		}

		foreach(GameObject mc in _mosaicCams)
			mc.SetActive(active);
		_defaultCam.SetActive(!active);

		_knobs = FindObjectsOfType<Knob>();
		foreach(Knob k in _knobs)
			k.SetAltCamera(active);

		if(!active)
		{
			if(_cans==null)
				_cans = FindObjectOfType<CannulaMenu>();
			if(_cans!=null)
				_cans.SetAltCamera(null);
		}

		foreach(ClickableRange cr in _clickables)
			cr.SetAltCamera(active);

		Camera ventCam = _mosaicCams[3].GetComponent<Camera>();
		Camera defCam = _defaultCam.GetComponent<Camera>();
		foreach(Canvas c in _dragerCans){
			c.worldCamera=active ? ventCam : defCam;
		}

		foreach(Canvas c in _alarisCans){
			c.worldCamera=active ? ventCam : defCam;
		}

		foreach(CanvasGroup cg in _uiHideOnMosaic){
			cg.alpha = active ? 0f : 1f;
			cg.interactable = !active;
			cg.blocksRaycasts = !active;
			foreach(CanvasGroup cg2 in cg.transform.GetComponentsInChildren<CanvasGroup>())
			{
				cg2.alpha = active ? 0f : 1f;
				cg2.interactable = !active;
				cg2.blocksRaycasts = !active;
			}
		}

		if(active){
			PipHud [] pips = FindObjectsOfType<PipHud>();
			foreach(PipHud ph in pips)
				ph.Activate(false);
		}
		else
			_hoffAlt=null;
	}
}
