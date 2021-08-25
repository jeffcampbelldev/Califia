using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class Knob : MonoBehaviour
{
	bool _highlight=false;
	Material _highlightMat;
	Vector2 _prevDiff;
	public UnityEvent _OnDragStart;
	public UnityEvent _OnValueChanged;
	public UnityEvent _OnButtonPush;
	public UnityEvent _OnDragEnd;
	public int _highlightMatIndex;
	bool _dragging=false;
	Vector2 _centerPointScreen;
	public float _minVal;
	public float _maxVal;
	public float _startVal;
	public float _changeSpeed;
	float _prevAng;
	//[HideInInspector]
	public float _val;
	bool _valSet;
	public Text _dispText;
	public float _dispMult=1;
	public string _textConversion;
	MyMQTT _mqtt;
	//TestCam _cam;
	public string _pubAlias;
	Inventory _inventory;
	float _minAng, _maxAng;
	public Knob _combinesWith;
	bool _buttonPush;
	[Tooltip("0=left, 1=right, 2=up, 3=down, 4=forward, 5=back")]
	public int _rotationAxisCode=1;
	Vector3 _rotationAxis;
	Vector3 _eulerVector;
	Vector3 _originalEulers;
	AudioSource _buttonAudio;
	Camera _curCam;
	Camera _defaultCam;
	public Camera _altCam;
	public float _outlineThickness;
	SphereCollider _sphere;
	//todo rethink this hacky hack to get the main camera when disabled
	CircuitManager _circuit;
	float _turnThresh=15f;
	bool _arScene;
	string _camComponent;
	bool _init;

	// Start is called before the first frame update
	void Start()
	{
		_camComponent = "TestCam";
		//#todo - maybe find a better way to do this
		if(!_valSet)
			_val=_startVal;
		_circuit = FindObjectOfType<CircuitManager>();
		_defaultCam = GameObject.Find("DefaultCamera").GetComponent<Camera>();
		_inventory = _defaultCam.transform.Find("Inventory").GetComponent<Inventory>();
		if(!_init)
			Init();
		_buttonAudio = GetComponent<AudioSource>();
		CameraManager camMan = FindObjectOfType<CameraManager>();
		if(camMan!=null){
			if(camMan._hoffAlt==null)
				SetAltCamera(false);
			else
				_curCam=camMan._hoffAlt;
		}
		else
			_curCam=_defaultCam;
		_sphere = GetComponent<SphereCollider>();

		//get mqtt handle
		MyMQTT [] qts = FindObjectsOfType<MyMQTT>();
		foreach(MyMQTT qt in qts){
			if(qt.gameObject.tag=="GameController")
				_mqtt=qt;
		}

		//fill text
		string disp = (_val*_dispMult).ToString(_textConversion);
		if(_dispText!=null)
			_dispText.text=disp;
	}

	void Init(){
		float startAng=transform.localEulerAngles.x;
		_minAng = startAng-(_minVal-_startVal)/_changeSpeed;
		_maxAng = startAng-(_maxVal-_startVal)/_changeSpeed;
		switch(_rotationAxisCode){
			case 0:
				_rotationAxis=-transform.right;
				_eulerVector=Vector3.left;
				break;
			case 1:
			default:
				_rotationAxis=transform.right;
				_eulerVector=Vector3.right;
				break;
			case 2:
				_rotationAxis=transform.up;
				_eulerVector=Vector3.up;
				break;
			case 3:
				_rotationAxis=-transform.up;
				_eulerVector=Vector3.down;
				break;
			case 4:
				_rotationAxis=transform.forward;
				_eulerVector=Vector3.forward;
				break;
			case 5:
				_rotationAxis=-transform.forward;
				_eulerVector=Vector3.back;
				break;
		}
		_originalEulers=transform.localEulerAngles;

		_init=true;
	}

	void RotateKnob(float diff){
		//need to re-evaluate rotation axis in AR scene because the knobs may move
		//so the rotation vectors may need to change
		if(_arScene){
			switch(_rotationAxisCode){
				case 0:
					_rotationAxis=-transform.right;
					_eulerVector=Vector3.left;
					break;
				case 1:
				default:
					_rotationAxis=transform.right;
					_eulerVector=Vector3.right;
					break;
				case 2:
					_rotationAxis=transform.up;
					_eulerVector=Vector3.up;
					break;
				case 3:
					_rotationAxis=-transform.up;
					_eulerVector=Vector3.down;
					break;
				case 4:
					_rotationAxis=transform.forward;
					_eulerVector=Vector3.forward;
					break;
				case 5:
					_rotationAxis=-transform.forward;
					_eulerVector=Vector3.back;
					break;
			}
		}
		transform.RotateAround(transform.position, _rotationAxis, diff);
	}

	void OnEnable(){
		_highlightMat=GetComponent<MeshRenderer>().materials[_highlightMatIndex];
		ClearHighlight();
	}

	public void UpdateBounds(float min, float max){
		_minVal=min;
		_maxVal=max;
	}
	
	public void JustSetValue(float val,bool publish=false){
		if(val >= _minVal && val <= _maxVal){
			//set value
			_val=val;
			_valSet=true;
			string disp = (_val*_dispMult).ToString(_textConversion);
			if(_dispText!=null)
				_dispText.text=disp;
			if(publish)
			{
				if(_mqtt==null)
					_mqtt=FindObjectOfType<MyMQTT>();
				if(_pubAlias!="")
					_mqtt.ForceSendKnobVal(this,_combinesWith,_pubAlias);
			}
		}
	}

	public void SetValue(float val){
		if(!_init)
		{
			Init();
			_valSet=true;
			StartCoroutine(ValChangeNextFrameR());
		}
		if(val >= _minVal && val <= _maxVal){
			//set value
			_val=val;
			//set knob angle
			float localX=Mathf.Lerp(_minAng,_maxAng,Mathf.InverseLerp(_minVal,_maxVal,val));
			transform.localEulerAngles=_originalEulers+_eulerVector*localX;
			//fire event
			_OnValueChanged.Invoke();
		}
	}

	//#wackety wack
	IEnumerator ValChangeNextFrameR(){
		yield return null;
		_OnValueChanged.Invoke();
	}

	// Update is called once per frame
	void Update()
	{
		_sphere.enabled=(_curCam.transform.position-transform.position).sqrMagnitude<1;
		Vector2 curPos=Input.mousePosition;
		if (_dragging)
		{
			float prevVal = _val;
			float diff = 0;
			float ang = 0;
			//controller knob input
			if (TestCam._useController){
				Vector2 inAx = new Vector2(Input.GetAxis("Horizontal"),
						Input.GetAxis("Vertical"));
				if (inAx.sqrMagnitude > 0.02f)
				{
					ang = Mathf.Atan2(inAx.y, inAx.x) * Mathf.Rad2Deg;
					diff = ang - _prevAng;
				}
			}
			else{//mouse knob input
				_prevDiff = curPos - _centerPointScreen;
				ang = Mathf.Atan2(_prevDiff.y, _prevDiff.x) * Mathf.Rad2Deg;
				diff = (ang - _prevAng);
			}

			if (Mathf.Abs(diff) > _turnThresh){
				_buttonPush = false;
			}
			if (!_buttonPush){
				if (diff > 180)
					_val -= (diff - 360) * _changeSpeed;
				else if (diff < -180)
					_val -= (diff + 360) * _changeSpeed;
				else
					_val -= (diff) * _changeSpeed;
				_prevAng = ang;
				//only rotate if within bounds
				if (_val < _maxVal && _val > _minVal)
					RotateKnob(diff);
				_val = Mathf.Clamp(_val, _minVal, _maxVal);

				if (prevVal != _val && _mqtt!=null){
					if (_mqtt == null)
						_mqtt = FindObjectOfType<MyMQTT>();
					if (_pubAlias != "")
						_mqtt.SendKnobVal(this, _combinesWith, _pubAlias);
				}
				string disp = (_val * _dispMult).ToString(_textConversion);
				if (_dispText != null)
					_dispText.text = disp;
			}
			if (TestCam.GetPrimaryClickUp()){//Input.GetMouseButtonUp(0)){
				//drag ended
				DragEnd();
			}
			else
				_OnValueChanged.Invoke();
		}
		
		if(_highlight && !_dragging){
			if (TestCam.GetPrimaryClickDown()){
				DragStart(curPos);
			}
		}
	}

	public void SetAltCamera(bool alt){
		//set default camera to c
		if(_altCam==null)
			_curCam=_defaultCam;
		else
			_curCam=alt ? _altCam : _defaultCam;
	}

	public void SetAltCamera(Camera c){
		_curCam=c;
	}

	void DragStart(Vector2 pos){
		_dragging=true;
		_highlightMat.SetFloat("_OutlineThickness",_outlineThickness);
		_OnDragStart.Invoke();
		_centerPointScreen = _curCam.WorldToScreenPoint(transform.position);
		_prevDiff = pos-_centerPointScreen;
		float ang = Mathf.Atan2(_prevDiff.y,_prevDiff.x)*Mathf.Rad2Deg;
		_prevAng=ang;
		// if not in the AR Scene, disable first person cam
		var cam = _defaultCam.GetComponent(_camComponent) as MonoBehaviour;
		cam.enabled = false;
		//button push is considered true until proven false in the loop of update
		_buttonPush =true;
		//clear inventory
		_inventory.ReturnAllItems();
	}

	//test comment
	void DragEnd(){
		_dragging=false;
		// if not in the AR Scene, re-enable first person cam
		if (!_arScene) {
			var cam = _defaultCam.GetComponent(_camComponent) as MonoBehaviour;
			cam.enabled = true;
		}
		ClearHighlight();
		if(_mqtt==null)
			_mqtt=FindObjectOfType<MyMQTT>();
		if(_buttonPush){
			Debug.Log("button push");
			_OnButtonPush.Invoke();
			if(_buttonAudio!=null)
				_buttonAudio.Play();
		}
		else{
			if(_pubAlias!=""&&_mqtt!=null)
				_mqtt.ForceSendKnobVal(this,_combinesWith,_pubAlias);
		}
		_OnDragEnd.Invoke();
	}

	void ClearHighlight(){
		if(_highlightMat!=null)
			_highlightMat.SetFloat("_OutlineThickness",0);
	}

	void OnMouseEnter(){
		_highlight=true;
		_highlightMat.SetFloat("_OutlineThickness",_outlineThickness);
	}

	void OnMouseExit(){
		_highlight=false;
		ClearHighlight();
	}
}
