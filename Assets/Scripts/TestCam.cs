using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TestCam : MonoBehaviour
{
	public float _mouseRotateMultiplier;
	public float _joyMultiplier;
	public float _slerpRate;
	public float _moveSpeed;
	Quaternion _targetRot;
	Vector3 _prevPos;
	float _prevHeight;
	MoveCameraVertical _vertSlider;
	[HideInInspector]
	public bool _inputEnabled=true;
	[SerializeField] private InteractiveConfig _config = null;
	Vector3 _prevMouse;
	public bool _moveEnabled;
	public AnimationCurve _moveCurve;
	public LayerMask _navLayer;
	public static bool _useController=false;
	RawImage _cursor;
	static float _clickDelay=0;
	static float _clickDelayThreshold=0.1f;
	Vector3 _orbitTarget;

	// Start is called before the first frame update
	void Start()
	{
		_targetRot = transform.rotation;
		_prevHeight = transform.position.y;
		_vertSlider = FindObjectOfType<MoveCameraVertical>();

		if (_config != null && _moveEnabled)
        {
			_mouseRotateMultiplier = _config.lookSensitivity;
			_joyMultiplier = _config.joystickSensitivity;
			_moveSpeed = _config.moveSensitivity;
        }
		_cursor = GameObject.Find("3Dcursor").GetComponent<RawImage>();
	}

	void OnEnable(){
		_prevMouse=Input.mousePosition;
		_clickDelay=0f;
	}

	// Update is called once per frame
	void Update()
	{
		if(_inputEnabled){
			HandleRotation();
			HandleTranslation();
			if(_clickDelay<_clickDelayThreshold)
				_clickDelay+=Time.deltaTime;
		}
	}

	public void LerpTest(string target){
		StartCoroutine(LerpTestR(target,Vector3.zero));
	}

	public void GoTo(Vector3 position, Vector3 eulers){
		if(gameObject.activeSelf){
			StopAllCoroutines();
			StartCoroutine(LerpTo(position,eulers));
		}
	}

	public void LookAtTarget(Vector3 pos){
		StartCoroutine(LerpTestR("",pos));
	}

	public void LerpToTransform(Transform t){
		if(gameObject.activeInHierarchy)
			StartCoroutine(LerpToTransformR(t));
	}

	public void LerpToTransformDirect(Transform t){
		if(gameObject.activeInHierarchy)
			StartCoroutine(LerpToTransformR(t,true));
	}

	IEnumerator LerpTo(Vector3 pos, Vector3 eul){
		//position
		Vector3 start = transform.position;
		_prevPos=start;
		Vector3 endPos = pos;
		//rotation
		Quaternion startRot,endRot;
		startRot = transform.rotation;
		transform.eulerAngles=eul;
		endRot=transform.rotation;
		transform.rotation=startRot;
		float dist = (endPos-start).magnitude;
		dist = Mathf.Max(0.75f,dist);
		float speed = 2; //meters per sec
		float dur = dist/speed;
		float timer=0;
		while(timer<dur){
			timer+=Time.deltaTime;
			float t = _moveCurve.Evaluate(timer/dur);
			transform.position = Vector3.Lerp(start,endPos,t);
			transform.rotation = Quaternion.Slerp(startRot,endRot,t);
			yield return null;
		}
	}

	IEnumerator LerpTestR(string target, Vector3 pos){
		Vector3 start = transform.position;
		_prevPos=start;
		Quaternion startRot,endRot;
		Vector3 endPos;
		if(target!=""){
			startRot = transform.rotation;
			Transform tCam = GameObject.Find(target).transform;
			endPos = tCam.position;
			endRot = tCam.rotation;
		}
		else{
			startRot = transform.rotation;
			transform.LookAt(pos);
			endRot=transform.rotation;
			transform.rotation=startRot;
			endPos=transform.position;
		}

		float timer = 0;
		while(timer<1f){
			timer+=Time.deltaTime;
			transform.position = Vector3.Lerp(start,endPos,timer);
			transform.rotation = Quaternion.Slerp(startRot,endRot,timer);
			_targetRot=transform.rotation;
			yield return null;
		}
	}

	IEnumerator LerpToTransformR(Transform t,bool direct=false){
		Vector3 start = transform.position;
		_prevPos=start;
		Quaternion startRot,endRot;
		Vector3 endPos=Vector3.zero;
		startRot=transform.rotation;
		endRot=Quaternion.identity;
		if(!direct){
			endPos = t.position+t.forward*.3f;
			endRot.SetLookRotation(-t.forward,t.up);
		}
		else
		{
			endPos = t.position;
			endRot.SetLookRotation(t.forward,t.up);
		}
		//endRot = Quaternion.Euler(t.eulerAngles.x,t.eulerAngles.y,0);
		//endRot*=Quaternion.Euler
		//this stuff is kind of error prone, but I'm tired of getting spammed with
		//highlight fx when I just clicked a thing
		ClickDetection tmp = null;
		if(t.GetComponent<ClickDetection>()!=null){
			tmp=t.GetComponent<ClickDetection>();
		}
		else if(t.parent!=null && t.parent.GetComponent<ClickDetection>()!=null){
			tmp=t.parent.GetComponent<ClickDetection>();
		}
		if(tmp!=null){
			tmp.SetLast(endPos,endRot);
		}
		float timer = 0;
		float dur = 1f;
		while(timer<dur){
			timer+=Time.deltaTime;
			transform.position = Vector3.Lerp(start,endPos,timer/dur);
			transform.rotation = Quaternion.Slerp(startRot,endRot,timer/dur);
			_targetRot=transform.rotation;
			yield return null;
		}
		transform.position=endPos;
		transform.rotation=endRot;
	}

	public void UnlerpTest(){
		StartCoroutine(UnlerpTestR());
	}

	IEnumerator UnlerpTestR(){
		Vector3 start = transform.position;
		float timer=0;
		while(timer<1f){
			timer+=Time.deltaTime;
			transform.position = Vector3.Lerp(start,_prevPos,timer);
			HandleRotation();
			yield return null;
		}
	}

	public void FaceAvatar(Vector3 pos){
		Quaternion startRot = transform.rotation;
		transform.LookAt(pos);
		Quaternion endRot=transform.rotation;
		StartCoroutine(FaceAvatar(startRot,endRot));
	}
	IEnumerator FaceAvatar(Quaternion start, Quaternion end){
		float timer=0;
		while(timer<1f){
			timer+=Time.deltaTime;
			transform.rotation = Quaternion.Slerp(start,end,timer);
			yield return null;
		}
	}

	public void HandleRotation(){
		float joyX = Input.GetAxis("JoyX");
		float joyY = Input.GetAxis("JoyY");
		if(Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1)){
			CheckInputType(false);
			//drag start
			_prevMouse=Input.mousePosition;
			//stop camera animation
			StopAllCoroutines();
		}
		else if(Input.GetMouseButton(0)){
			//dragging
			Vector3 delta = Input.mousePosition-_prevMouse;
			transform.Rotate(-delta.y* _mouseRotateMultiplier, delta.x* _mouseRotateMultiplier, 0, Space.Self);
			//level out the z (roll component)
			Vector3 eulers = transform.localEulerAngles;
			eulers.z = 0;
			transform.localEulerAngles = eulers;
			_prevMouse=Input.mousePosition;
		}
#if UNITY_EDITOR
		else if(Input.GetMouseButton(1)){
			//dragging
			Vector3 delta = Input.mousePosition-_prevMouse;
			transform.RotateAround(_orbitTarget,Vector3.up,delta.x);
			transform.RotateAround(_orbitTarget,transform.right,-delta.y);
			_prevMouse=Input.mousePosition;
		}
#endif
		else if(joyX!=0 || joyY!=0){
			CheckInputType(true);
			transform.Rotate(-joyY* _joyMultiplier, joyX* _joyMultiplier, 0, Space.Self);
			//level out the z (roll component)
			Vector3 eulers = transform.localEulerAngles;
			eulers.z = 0;
			transform.localEulerAngles = eulers;

		}

	}

	public void CheckInputType(bool controller){
		if(_useController==controller)
			return;
		_useController=controller;
		if(_useController)
			Cursor.lockState = CursorLockMode.Locked;
		else
			Cursor.lockState = CursorLockMode.None;
		_cursor.enabled=_useController;
		//mouse lock state etc..
	}

	public void AddTranslation(Vector3 dir, float speed){
		dir.z=dir.y;
		Vector3 flatForward = transform.forward;
		Vector3 flatRight = transform.right;
		flatForward.y = 0;
		flatRight.y = 0;
		flatForward.Normalize();
		flatRight.Normalize();
		if(Input.GetKey(KeyCode.LeftShift))
			transform.position+=(flatForward*dir.z+flatRight*dir.x)*_moveSpeed*speed*2*Time.deltaTime;
		else
			transform.position+=(flatForward*dir.z+flatRight*dir.x)*_moveSpeed*speed*Time.deltaTime;

	}
	public void AddRotation(Vector3 dir, float speed){
		transform.Rotate(-dir.y* speed, dir.x* speed, 0, Space.Self);
		//level out the z (roll component)
		Vector3 eulers = transform.localEulerAngles;
		eulers.z = 0;
		transform.localEulerAngles = eulers;
	}

	public void SetHeight(float h){
		Vector3 pos = transform.position;
		pos.y=h;
		transform.position=pos;
	}

	public void HandleTranslation(){
		Vector3 flatForward = transform.forward;
		Vector3 flatRight = transform.right;
		flatForward.y = 0;
		flatRight.y = 0;
		flatForward.Normalize();
		flatRight.Normalize();

		float v = Input.GetAxis("Vertical");
		float h = Input.GetAxis("Horizontal");

		if(Input.GetKey(KeyCode.LeftShift)){
			transform.position+=flatForward*_moveSpeed*2 * Time.deltaTime*v;
			transform.position+=flatRight*_moveSpeed*2 * Time.deltaTime*h;
			transform.position+=Vector3.up*_moveSpeed*2 * Time.deltaTime*Input.GetAxis("DY");
		}
		else{
			transform.position+=flatForward*_moveSpeed * Time.deltaTime*v;
			transform.position+=flatRight*_moveSpeed * Time.deltaTime*h;
			transform.position+=Vector3.up*_moveSpeed * Time.deltaTime*Input.GetAxis("DY");
		}
		if(transform.position.y!=_prevHeight)
		{
			_vertSlider.UpdateSlider(transform.position.y);
		}
		if(v!=0 || h!=0)
			StopAllCoroutines();//stop camera motion on key down
		_prevHeight = transform.position.y;
	}

	public void SetOrbitTarget(Vector3 p){
		_orbitTarget=p;
	}

	public static bool GetPrimaryClickDown(){
		if(_clickDelay<_clickDelayThreshold)
			return false;
		return Input.GetMouseButtonDown(0) || Input.GetButtonDown("Submit");
	}

	public static bool GetPrimaryClickUp(){
		return Input.GetMouseButtonUp(0) || Input.GetButtonUp("Submit");
	}

	public static bool GetSecondaryClick(){
		if(_clickDelay<_clickDelayThreshold)
			return false;
		return Input.GetMouseButtonDown(1) || Input.GetButtonDown("Secondary");
	}
}
