using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class AnimationPath : MonoBehaviour
{
	CubicBezierPath _path;
	Vector3[] _points;
	Vector3[] _prevPoints;
	public Transform _animateObject;
	public Transform _animateCamera;
	public float _animationDuration;
	public AnimationCurve _animationCurve;
	public static bool _isRunning = false; 
	public static bool _simLoaded = false;
	//public PathData _pathData;
	[Header("Editor only")]
	public Vector2 _speedColor;
	public Animator _handsAnimLeft;
	public Animator _handsAnimRight;
	public Animator _camAnim;
	public CanvasGroup _cg;
	public int _pathCode; //0 = intro, 1 = icu, 2 = or
	IEnumerator _lookAround;
	public static float _lookLerp = .25f;
	public Transform _refDoor;
	public Transform _doorLeft;
	public Transform _doorRight;
	public Vector3 _forward;

    // Start is called before the first frame update
    void Start()
    {
		GeneratePath(true);

		if (Application.isPlaying && _pathCode==0){
			PlayAnim();
		}
		_animateCamera = _animateObject.GetChild(0);
    }

    // Update is called once per frame
    void Update()
    {
		GeneratePath();
    }

	//mqtt is null when called internally
	//but has a value when called via MyMQTT via topic
	[ContextMenu("Play animation")]
	public void PlayAnim(MyMQTT mqtt = null){
		StartCoroutine(PlayAnimR(mqtt));
	}
	IEnumerator FadeR(){
		float timer=0f;
		_cg.alpha=1f;
		while(timer<1f){
			timer+=Time.deltaTime;
			_cg.alpha=1-timer;
			yield return null;
		}
		_cg.alpha=0f;

	}
	IEnumerator PlayAnimR(MyMQTT mqtt){
		Debug.Log("waiting to walk on path: "+transform.name);
		if(_lookAround!=null)
			StopCoroutine(_lookAround);
		while (_isRunning == true) { yield return null; }
		Debug.Log("starting walk path: "+transform.name);
		_isRunning = true;
		float timer=0f;
		float t01;
		float t;
		_handsAnimLeft.SetBool("walk",true);
		_handsAnimRight.SetBool("walk",true);
		_animateCamera.forward=_animateObject.forward;
		while(timer<_animationDuration){
			timer+=Time.deltaTime;
			t01=_animationCurve.Evaluate(timer/_animationDuration);
			t=Mathf.Lerp(0,_path.GetNumCurveSegments(),t01);
			_animateObject.position=_path.GetPoint(t);
			_animateObject.forward = Vector3.Lerp(_animateObject.forward,_path.GetTangent(t),
					Time.deltaTime*_lookLerp*6);
			yield return null;
		}
		_handsAnimLeft.SetBool("walk",false);
		_handsAnimRight.SetBool("walk",false);
		_camAnim.SetBool("walk",false);
		_isRunning = false;
		if(_lookAround!=null)
			StopCoroutine(_lookAround);
		_lookAround = LookAroundR();
		if(_animateCamera!=null)
			StartCoroutine(_lookAround);

		//mqtt is null in the first section of walk
		//mqtt is not null when called via mqtt
		//basically this means we are in the second part of the walk
		if(mqtt!=null){
			StartCoroutine(OpenDoorR(mqtt));
		}
	}

	IEnumerator OpenDoorR(MyMQTT mqtt){
		yield return null;
		//wait for sim to load
		while(!_simLoaded){
			yield return null;
		}
		//get camera to look right at door
		if(_lookAround!=null)
			StopCoroutine(_lookAround);
		Quaternion startRot = _animateCamera.rotation;
		_animateCamera.LookAt(_refDoor);
		Vector3 eul = _animateCamera.localEulerAngles;
		eul.x=0;
		_animateCamera.localEulerAngles=eul;
		Quaternion endRot = _animateCamera.rotation;
		float timer=0;
		while(timer<1f){
			timer+=Time.deltaTime;
			_animateCamera.rotation = Quaternion.Slerp(startRot,endRot,timer);
			yield return null;
		}
		//set door targets for portal cam
		PortalCam pc = FindObjectOfType<PortalCam>();
		GameObject door = GameObject.FindGameObjectWithTag("PortalTarget");
		pc._myDoor=door.transform;
		pc._refDoor=_refDoor;
		pc._path=this;
		Transform portalDoorLeft = door.transform.GetChild(0);
		Transform portalDoorRight = door.transform.GetChild(1);
		//disable animator components
		Animator doorAnimHallway = _doorLeft.parent.GetComponent<Animator>();
		Animator doorAnimRoom = door.transform.GetComponent<Animator>();
		doorAnimRoom.enabled=false;
		doorAnimHallway.enabled=false;
		timer=0;
		//animate door
		while(timer<1f){
			timer+=Time.deltaTime;
			_doorLeft.localEulerAngles = Vector3.up*Mathf.Lerp(0,90f,timer);
			_doorRight.localEulerAngles = Vector3.down*Mathf.Lerp(0,90f,timer);
			portalDoorLeft.localEulerAngles = Vector3.up*Mathf.Lerp(0,90f,timer);
			portalDoorRight.localEulerAngles = Vector3.down*Mathf.Lerp(0,90f,timer);
			yield return null;
		}
		//walk in door
		//At some point here PortalCam.cs moves the camera rig into the icu room
		_handsAnimLeft.SetBool("walk",true);
		_handsAnimRight.SetBool("walk",true);
		timer=0f;
		Transform handl = _handsAnimLeft.transform;
		Transform handr = _handsAnimRight.transform;
		Vector3 eulers = _animateCamera.localEulerAngles;
		eulers.x=0;
		_animateCamera.localEulerAngles=eulers;
		_forward = _animateCamera.forward;
		//enter room until portal camera reaches PortalComplete trigger
		while(pc.enabled){
			_animateCamera.position+=_forward*Time.deltaTime*1.5f;
			handl.position+=Vector3.down*Time.deltaTime*.5f;
			handr.position+=Vector3.down*Time.deltaTime*.5f;
			yield return null;
		}
		//fix camera and hand positions
		_animateCamera.position = pc.transform.position;
		_animateCamera.rotation = pc.transform.rotation;
		_handsAnimLeft.SetBool("walk",false);
		_handsAnimRight.SetBool("walk",false);
		//transition to test cam
		Transform testCam = FindObjectOfType<TestCam>().transform;
		timer=0f;
		Vector3 startPos = _animateCamera.position;
		startRot = _animateCamera.rotation;
		float dur = 3f;
		while(timer<dur){
			timer+=Time.deltaTime;
			_animateCamera.position = Vector3.Lerp(startPos,testCam.position,timer/dur);
			_animateCamera.rotation = Quaternion.Slerp(startRot,testCam.rotation,timer/dur);
			yield return null;
		}
		_simLoaded=false;
		portalDoorLeft.localEulerAngles = Vector3.up*Mathf.Lerp(0,90f,0);
		portalDoorRight.localEulerAngles = Vector3.down*Mathf.Lerp(0,90f,0);
		//re-enable animator component
		doorAnimRoom.enabled=true;
		_animateCamera.GetComponent<BlendEffect>().Blink();
		yield return new WaitForSeconds(BlendEffect._blinkDur*2);
		//_animateCamera.gameObject.SetActive(false);
		mqtt.UnloadPreviousScene();
	}

	IEnumerator LookAroundR(){
		yield return null;
		Vector3 rootForward = _animateObject.forward;
		Vector3 targetForward = Vector3.zero;
		float timer=0;
		float maxTime=5f;
		float sqrM=100;
		while(!_isRunning){
			targetForward = new Vector3(rootForward.x+Random.Range(-.2f,.2f),
							rootForward.y+Random.Range(-.05f,.3f),
							rootForward.z+Random.Range(-.2f,.2f));
			sqrM=(targetForward-_animateCamera.forward).sqrMagnitude;
			timer=0;
			while(sqrM>0.01f && timer<maxTime&&!_isRunning){
				_animateCamera.forward=Vector3.Lerp(_animateCamera.forward,targetForward,
						Time.deltaTime*_lookLerp);
				timer+=Time.deltaTime;
				yield return null;
				sqrM=(targetForward-_animateCamera.forward).sqrMagnitude;
			}
			yield return null;
		}
	}

	void GeneratePath(bool force=false){
		List<Vector3> _pointList = new List<Vector3>();
		foreach(Transform t in transform)
			_pointList.Add(t.position);	
		_points = _pointList.ToArray();

		_path = new CubicBezierPath(_points);
	}

	void LoadNewScene(MyMQTT mqtt)
    {
		mqtt.ShowLoadingScreen(true);
		mqtt.SendPub("RoomStatus", 2); //2 = loading
		SceneManager.LoadScene(_pathCode);	//pathCode corresponds to the room index
	}

#if UNITY_EDITOR
	void OnDrawGizmos(){
		for(int i=0; i<transform.childCount; i++){
			Vector3 p = transform.GetChild(i).position;
			Gizmos.DrawSphere(p,.2f);
			Handles.Label(p+Vector3.up*.4f,"index: "+i);
		}
		if(_path!=null)
		{
			Vector3 last=Vector3.zero;
			for(int i=0; i<transform.childCount-1; i++){
				for(int j=0; j<10; j++){
					float t01 = (j+10*i) /((transform.childCount-1)*10f);
					float t=Mathf.Lerp(0,_path.GetNumCurveSegments(),t01);
					//sample from cubic bezier curve
					Vector3 pos = _path.GetPoint(t);
					Vector3 tan = _path.GetTangent(t);
					float ds = (pos-last).sqrMagnitude;
					Gizmos.color=Color.Lerp(Color.red,Color.green,
							Mathf.InverseLerp(_speedColor.x,_speedColor.y,ds));
					Gizmos.DrawSphere(pos,.1f);
					last=pos;
				}
			}
			for(int i=0; i<40; i++){
				float t01 = i/(float)(40-1);
				//sample from cubic bezier curve
				Vector3 pos = _path.GetPointNorm(t01);
				Vector3 tan = _path.GetTangentNorm(t01);
				float ds = (pos-last).sqrMagnitude;
				Gizmos.color=Color.Lerp(Color.red,Color.green,
					Mathf.InverseLerp(_speedColor.x,_speedColor.y,ds));
				Gizmos.DrawWireSphere(pos,.2f);
				last=pos;
			}
		}
	}
#endif
}
