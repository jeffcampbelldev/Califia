//ClickDetection.cs
//
//Description: Helper class for mouse events
//Handles mouse enter, exit, click
//Handles animating materials to indicate interactability
//Exposes UnityEvent's so functionality can be assigned via script or in inspector
//

//

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ClickDetection : MonoBehaviour
{
	public bool _animHighlight=true;
	Material _highlightMat;
	Vector2 _clickPos;
	public UnityEvent _onClick;
	public UnityEvent _onClickRight;
	public UnityEvent _onEnter;
	public UnityEvent _onExit;
	public UnityEvent _onPress;
	public UnityEvent _onRelease;
	public UnityEvent _onToggleOn;
	public UnityEvent _onToggleOff;
	public int _highlightMatIndex;
	public float _maxRim = 4f;
	Inventory _inventory;
	public bool _stayLit;
	public bool _isToggle;
	bool _isOn;
	Transform _mainCam;
	Vector3 _lastPos;
	Vector3 _mouseDownPos;
	Quaternion _lastRot;
	public float _outlineThickness;
	NavPanel _nav;
	bool _mouseIn;
	float _clickCoolDown=0;
	bool _clickCandidate;

	// Start is called before the first frame update
	void Start()
	{
		_lastPos=Vector3.zero;
		_lastRot=Quaternion.identity;
		//_main=FindObjectOfType<TestCam>().GetComponent<Camera>();
		_mainCam = Camera.main.transform;
		ClearHighlight();
		_inventory = FindObjectOfType<Inventory>();
		//_infoPanel.alpha=0;
		_nav = FindObjectOfType<NavPanel>();
	}

	void OnEnable(){
		if(GetComponent<SphereCollider>()!=null){
			GetComponent<SphereCollider>().enabled=true;
		}
		if(GetComponent<BoxCollider>()!=null){
			GetComponent<BoxCollider>().enabled=true;
		}
		if(GetComponent<MeshCollider>()!=null){
			GetComponent<MeshCollider>().enabled=true;
		}
		if(GetComponent<CapsuleCollider>()!=null){
			GetComponent<CapsuleCollider>().enabled=true;
		}
		_mouseIn=false;
	}

	void OnDisable(){
		ClearHighlight();
		//_infoPanel.alpha=0;
		if(GetComponent<SphereCollider>()!=null){
			GetComponent<SphereCollider>().enabled=false;
		}
		if(GetComponent<BoxCollider>()!=null){
			GetComponent<BoxCollider>().enabled=false;
		}
		if(GetComponent<MeshCollider>()!=null){
			GetComponent<MeshCollider>().enabled=false;
		}
		if(GetComponent<CapsuleCollider>()!=null){
			GetComponent<CapsuleCollider>().enabled=false;
		}
	}

	// Update is called once per frame
	void Update()
	{
		if(Input.GetMouseButtonDown(0)||Input.GetMouseButtonDown(1))
		{
			_clickPos=Input.mousePosition;
			_clickPos.x/=Screen.width;
			_clickPos.y/=Screen.height;
			//_mouseDownPos=_mainCam.position;
			_mouseDownPos=Camera.main.transform.position;
			
		}
		//manual click detections
		if(_mouseIn){
			if(TestCam.GetPrimaryClickDown()){
				_clickCandidate=true;
			}
			else if(TestCam.GetPrimaryClickUp()){
				if(_clickCandidate)
					Click(true,false);
			}
			else if(TestCam.GetSecondaryClick()){
				Click(false,true);
			}
		}
		_clickCoolDown+=Time.deltaTime;
	}

	public void ClearHighlight(bool forceUpdate=false){
		if(_animHighlight){
			if(_highlightMat==null||forceUpdate)
			{
				if(GetComponent<MeshRenderer>()!=null)
					_highlightMat=GetComponent<MeshRenderer>().materials[_highlightMatIndex];
				else
					_highlightMat=GetComponent<SkinnedMeshRenderer>().materials[_highlightMatIndex];
			}
			_highlightMat.SetFloat("_OutlineThickness",0);
		}
	}

	//todo
	//these comments make no sense and why is that variable called pdf
	void OnMouseEnter(){
		if(!enabled)
			return;
		if(_highlightMat!=null)
			_highlightMat.SetFloat("_OutlineThickness",_outlineThickness);
		if(_onEnter!=null)
			_onEnter.Invoke();
		_mouseIn=true;
	}

	void OnMouseExit(){
		if(!enabled)
			return;
		if(!_stayLit || _isToggle && !_isOn){
			ClearHighlight();
		}
		if(_onRelease!=null)
			_onRelease.Invoke();
		if(_onExit!=null)
			_onExit.Invoke();
		_mouseIn=false;
		_clickCandidate=false;
		//Debug.Log("Exit");
		//_clickCoolDown=0;
	}

	void OnMouseDown(){
		if(!enabled)
			return;
		//_mouseDownPos=_mainCam.position;
		_mouseDownPos=Camera.main.transform.position;
		if(_onPress!=null)
			_onPress.Invoke();
	}

	void OnMouseUp(){
		if(!enabled)
			return;
		if(_onRelease!=null)
			_onRelease.Invoke();
	}

	public void Click(bool rightTrigger=false,bool leftTrigger=false){
		if(_clickCoolDown<0.5f)
			return;
		//if(_mainCam.position!=_mouseDownPos)
		if(Camera.main.transform.position!=_mouseDownPos)
			return;
		//Debug.Log("Got click");
		_clickCoolDown=0;
		Vector2 curPos = Input.mousePosition;
		curPos.x/=Screen.width;
		curPos.y/=Screen.height;
		if(!_mouseIn)
			return;

		//Debug.Log(_clickCoolDown);
		if(!_stayLit || _isToggle && _isOn)
		{
			ClearHighlight();
		}
		if(_isToggle){
			_isOn=!_isOn;
			if(_isOn)
				_onToggleOn.Invoke();
			else
				_onToggleOff.Invoke();
		}
		if(rightTrigger)
			_onClick.Invoke();
		else if(leftTrigger)
			_onClickRight.Invoke();
		_clickCandidate=false;
	}

	public Material GetHighlightMat(){
		return _highlightMat;
	}

	public void SetLast(Vector3 pos, Quaternion rot){
		_lastPos=pos;
		_lastRot=rot;
	}

	public void NavigateByIndex(int i){
		_nav.NavigateByIndex(i);
	}
}
