//Tube.cs
//
//Description: Tubes range from purely aesthetic to those with more depth
//Some tubes track a target object or a cannula in most cases
//Some tubes can be clamped
//Some tubes can be animated in the case of events
//Mesh creation can easily create memory leaks, so that is only done if control points change
//Actual curvature is created by Bezier.cs by Tristan Grimmer
//

//
//todo - figure out phantom clamp

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[ExecuteAlways]
public class Tube : MonoBehaviour
{
	//tube curve data
	CubicBezierPath _path;
	//num points along curve
	int _numPoints=40;
	//num points along tube rings
	public int _ringRes=4;
	MeshFilter _meshF;
	[HideInInspector]
	public MeshRenderer _meshR;
	public float _radius=.01f;
	Vector3[] _points; //control points
	Vector3[] _prevPoints; //used to check if mesh needs updating
	Vector3[] _verts; //ring center vertices
	public Transform _target;
	Transform _prevTarget;
	public Transform _root;
	MeshCollider _mc;
	Camera _mainCam;
	TestCam _tc;
	BoxCollider _blocker;
	float _lastSendTime=0;
	CircuitManager _circuit;

	//used for Physics.Raycast when placing clamps, sensors
	public LayerMask _layer;

	Material _tubeMat;
	bool _interactable;

	MyMQTT _mqtt;
	int _lastHoff;

	bool _init;

	public class Clamp {
		public Transform _clamp;
		public int _clampIndex;
		public float _clampVal;
		public float _clampValNorm=0f;
		public SkinnedMeshRenderer _clampMesh;
		public AudioSource _clampAudio;
		public bool _initialized=false;
		public int _type=0;

		public Clamp(Transform t, SkinnedMeshRenderer smr, AudioSource a, int i, float f){
			_clamp=t;
			_clampMesh=smr;
			_clampIndex=i;
			_clampVal=f;
			_clampAudio=a;
		}

		public void SetVis(bool vis){
			if(_clamp==null)
				return;
			if(_clampMesh!=null)
				_clampMesh.enabled=vis;
			if(_clamp.GetComponentInChildren<ClickDetection>()!=null)
				_clamp.GetComponentInChildren<ClickDetection>().enabled=vis;
			//_clamp.gameObject.SetActive(vis);
		}
	}

	[System.Serializable]
	public struct CircuitData{
		public TubeData[] Circuit_Data;
	}

	[System.Serializable]
	public struct TubeData{
		public int segment;
		public float tubing_size;
		public string blood_color;
		public string label_color;
		public float flow_rate;
	}


	List<Clamp> _clamps = new List<Clamp>();
	Vector3 _avgPos;
	public Transform _clampPrefab;
	public Transform _hoffmanPrefab;
	public Transform _flowSensorPrefab;
	Transform _flowSensor;
	MeshRenderer _flowSensorMesh;
	BoxCollider _flowSensorCol;
	public Item _flowSensorItem;
	FlowSensor _flow;
	Inventory _inventory;
	public Item _scissorClamp;
	bool _canClamp;
	bool _canSense;
	public bool _clampable;

	public bool _cannula;

	public Color _defaultBloodColor;
	public Color _defaultLabelColor;

	public TubeData _data;
	public string _type;
	public bool _fem;
	public bool _ibga;
	
	//tmp code - calculate tube
	float _tubeLength=1f;
	public Tube[] _next;
	public bool _canSetBloodDirect;
	public bool _sizeLock;
	[HideInInspector]
	public Color _col;
	public bool _sensingFlow;
	float _fillRate;
	Transform _clampParent;
	bool _clamping;
	bool _prevClamping;
	static Transform _foot;

	// Start is called before the first frame update
	void Start()
	{
		//create temp transforms to help with rotation
		if(_foot==null)
		{
			GameObject foo = GameObject.Find("EmptyTransform");
			if(foo==null)
				foo = new GameObject("EmptyTransform");
			_foot = foo.transform;
		}
		_mc = GetComponent<MeshCollider>();
		_meshR = GetComponent<MeshRenderer>();
		_verts = new Vector3[_numPoints];
		if(Application.isPlaying)
		{
			_tubeMat = _meshR.material;
			_tubeMat.SetFloat("_BloodFill",1);
			_tubeMat.SetFloat("_BloodFill2",1);
			//hidden tubing resets blood color when activated later
			//this check prevents the automatic resetting of color for those late-activation tubes
			if(Time.timeSinceLevelLoad<0.1f)
				_tubeMat.SetColor("_BloodColor",_defaultBloodColor);
			//_flow=FindObjectOfType<FlowSensor>();
		}
		GenerateMesh(true);
		ConfigureTube(_data);
		//_mc.enabled=false;
		_inventory = FindObjectOfType<Inventory>();
		//why do we regen right after generating mesh? wouldn't one suffice?
		//StartCoroutine(Regen());
		_prevTarget=_target;

		MyMQTT [] qts = FindObjectsOfType<MyMQTT>();
		foreach(MyMQTT qt in qts){
			if(qt.gameObject.tag=="GameController")
				_mqtt=qt;
		}
		//todo make this faster
		if(GameObject.Find("ClampParent")!=null)
			_clampParent = GameObject.Find("ClampParent").transform;

		_circuit = FindObjectOfType<CircuitManager>();

		//#todo tubes created in mosaic mode don't know about blocker and test cam
		//#remember tubes can be created any time a piece of equipment is toggled or model is changed
		GameObject defaultCam = GameObject.Find("DefaultCamera");
		if(defaultCam==null)
			return;
		_mainCam = GameObject.Find("DefaultCamera").GetComponent<Camera>();
		_tc = _mainCam.GetComponent<TestCam>();
		_blocker = _mainCam.transform.Find("Blocker").GetComponent<BoxCollider>();
		_init=true;
	}

	void OnDisable(){
		foreach(Clamp c in _clamps)
			c.SetVis(false);
	}

	void OnEnable(){
		foreach(Clamp c in _clamps)
			c.SetVis(true);
	}

	IEnumerator Regen(){
		//to account for delays in animation system I think
		//maybe the animation state machine needs to be reworked
		yield return new WaitForSeconds(0.5f);
		GenerateMesh();
		ConfigureTube(_data);
		_init=true;
	}

	public void GenerateMesh(bool force=false){
		List<Vector3> _pointList = new List<Vector3>();
		//start at the root
		if(_root==null||_target==null)
		{
			return;
		}
		_pointList.Add(_root.position);
		//points along a path are defined in the scene
		foreach(Transform t in transform)
			_pointList.Add(t.position);	
		//last point is anchored to target pos
		_pointList.Add(_target.position);
		_points = _pointList.ToArray();
		//if we have a cannula at the end
		if(_cannula==true){
			//set last point to cannula pos
			_points[_points.Length-1]=_target.position;
			//let's ensure we get that nice downward angle
			//_points[_points.Length-2].y=_points[_points.Length-1].y+0.01f;
			_points[_points.Length-2]=_points[_points.Length-1]-_target.forward*.2f;
			//and a straight vector along the spine
			//_points[_points.Length-2].x=_points[_points.Length-1].x;
		}

		//only regen if needed
		if(!force && _prevPoints!=null && _prevPoints.Length==_points.Length)
		{
			bool same=true;
			for(int i=0; i<_points.Length; i++){
				if(_points[i]!=_prevPoints[i])
				{
					same=false;
					break;
				}
			}
			if(same)
			{
				return;
			}
		}


		_path = new CubicBezierPath(_points);
		Mesh m = new Mesh();
		Vector3[] vertices = new Vector3[_numPoints*(_ringRes+1)];
		int[] tris = new int[(_numPoints-1)*_ringRes*2*3];
		Vector3[] norms = new Vector3[vertices.Length];
		Vector2[] uvs = new Vector2[vertices.Length];


		int triIndex=0;
		int vertIndex=0;
		_tubeLength=0;
		for(int c=0; c<_numPoints; c++){
			//calculate overall t
			float t01 = c/(float)(_numPoints-1);
			//calcualte segment based t
			float t=Mathf.Lerp(0,_path.GetNumCurveSegments(),t01);
			//sample from cubic bezier curve
			Vector3 pos = _path.GetPoint(t);
			if(c>0){
				_tubeLength+=(pos-_verts[c-1]).magnitude;
			}
			Vector3 tan = _path.GetTangent(t);
			_verts[c]=pos;
			//default clamp level
			float clampAmount=1;
			if(_clampable){
				foreach(Clamp clamp in _clamps){
					if(clamp._clampIndex==c)
					{
						clampAmount= clamp._clampVal;
						//make sure clamp position is good
						if(clamp._type==0){
							clamp._clamp.position=pos;
							//clamp._clamp.right = Vector3.Cross(tan,Vector3.up);
							clamp._clamp.up=tan;
							//weird algorithm for rotation
							int tries=0;
							while(Vector3.Dot(clamp._clamp.forward,Vector3.up)<0.5f && tries<30){
								clamp._clamp.RotateAround(pos,clamp._clamp.up,10f);
								tries++;
							}
						}
					}
				}
			}
			if(clampAmount==0)
				clampAmount=0.01f;
			//get a right and up vector
			if(_foot==null)
			{
				GameObject foo = GameObject.Find("EmptyTransform");
				if(foo==null)
					foo = new GameObject("EmptyTransform");
				_foot = foo.transform;
			}
			_foot.forward=tan;
			Vector3 up=_foot.right;
			Vector3 right = _foot.up;
			//go around the ring
			for(int r=0; r<_ringRes; r++){
				//get the fraction around the ring res
				float frac = r/(float)_ringRes;
				//get angle about ring
				float ang=frac*Mathf.PI*2;
				//determine position and nomrals
				Vector3 point = pos+right*Mathf.Cos(ang)*_radius*clampAmount+up*Mathf.Sin(ang)*_radius*clampAmount;
				norms[vertIndex]=point-pos;
				//vertices[vertIndex]=point-transform.position;
				vertices[vertIndex]=transform.InverseTransformPoint(point);
				uvs[vertIndex]=new Vector2(frac,t01);
				//if not the first ring - gen tris
				if(c>0){
					tris[triIndex]=vertIndex;
					tris[triIndex+1]=vertIndex+1;
					tris[triIndex+2]=vertIndex-(_ringRes+1);
					tris[triIndex+3]=vertIndex+1;
					tris[triIndex+4]=vertIndex-(_ringRes+1)+1;
					tris[triIndex+5]=vertIndex-(_ringRes+1);
					triIndex+=6;
				}
				vertIndex++;
			}
			vertices[vertIndex]=vertices[vertIndex-_ringRes];
			norms[vertIndex]=norms[vertIndex-_ringRes];
			uvs[vertIndex]=new Vector2(1f,t01);
			vertIndex++;
		}

		m.vertices=vertices;
		m.triangles=tris;
		m.normals = norms;
		m.uv=uvs;
		m.RecalculateBounds();
		_meshF = GetComponent<MeshFilter>();
		_meshF.sharedMesh=m;
		if(_mc!=null)
			_mc.sharedMesh=m;

		Vector3 avg=Vector3.zero;
		foreach(Vector3 vec in _points){
			avg+=vec;
		}
		avg/=_points.Length;
		_avgPos=avg;

		//save prevPoints
		_prevPoints=_points;
	}

	// Update is called once per frame
	// This is where we do mouse-casting and clamp interaction
	void Update()
	{
		_clamping=false;
		if(_target==null && _prevTarget!=null)
			StartCoroutine(CheckDualLumin());
		GenerateMesh();
		//clamp placement
		if(_clampable && _canClamp && (!_mqtt._hardware || _ibga)){
			//physics raycast
			RaycastHit hit;
			Vector3 ray = (Vector3)Input.mousePosition;
			//clamp on
			foreach(Clamp c in _clamps){
				if(c._clampIndex==-1){
					float radius=.01f;
					if(Physics.SphereCast(_mainCam.transform.position,radius,_mainCam.ScreenPointToRay(ray).direction,out hit,2f,_layer)&&hit.transform.name==transform.name){
						c._clamp.position=hit.point;
						int index = GetClosestIndex(c._clamp.position);
						bool indexClamped=false;
						foreach(Clamp cl in _clamps){
							if(cl._clampIndex==index)
							{
								c._clampMesh.enabled=false;
								indexClamped=true;
							}
						}
						if(indexClamped)
							break;
						c._clampMesh.enabled=true;
						c._clamp.up=Vector3.Cross(hit.normal,Vector3.up);
						if(TestCam.GetPrimaryClickDown())
							StartCoroutine(ClampRoutine(c,false));
						if(TestCam.GetSecondaryClick())
							StartCoroutine(ClampRoutine(c,true));
					}
					else
						c._clampMesh.enabled=false;
					_clamping=true;
					break;
				}
			}
		}	
		//sensor placement
		if(_clampable && _canSense){
			//physics raycast
			RaycastHit hit;
			Vector3 ray = (Vector3)Input.mousePosition;
			float radius=.01f;
			if(Physics.SphereCast(_mainCam.transform.position,radius,_mainCam.ScreenPointToRay(ray).direction,out hit,2f,_layer)&&hit.transform.name==transform.name){
				_flowSensor.position=hit.point;
				int index = GetClosestIndex(_flowSensor.position);
				float t01 = index/(float)_numPoints;
				float t=Mathf.Lerp(0,_path.GetNumCurveSegments(),t01);
				Vector3 tan = _path.GetTangent(t);
				_flowSensor.forward = Vector3.Cross(tan,Vector3.up);
				//click to place flow sensor
				if(TestCam.GetPrimaryClickDown()){
					_inventory.UseItem("FlowSensor");
					_canSense=false;
					_sensingFlow=true;
					_flowSensorCol.enabled=true;
					_flowSensor.GetComponentInChildren<Tube>().ConnectToTarget("FlowSensorPort");
				}
				_clamping=true;
			}
		}	
		if(_tubeMat==null)
			return;
		if(_prevClamping && !_clamping)
			_tubeMat.SetFloat("_OutlineThickness", 0f);
		else if(!_prevClamping && _clamping)
			_tubeMat.SetFloat("_OutlineThickness", 0.002f);
		_prevClamping=_clamping;
	}

	public void EnableCollision(bool isOn){
		//_mc.enabled=isOn;
		foreach(Clamp c in _clamps){
			if(c._clampIndex!=-1)
				c._clamp.GetChild(0).GetComponent<ClickDetection>().enabled=isOn;
		}
	}

	IEnumerator ClampRoutine(Clamp c,bool partial){
		//use this to clamp the tube
		int index = GetClosestIndex(c._clamp.position);
		c._clampIndex=index;
		c._clamp.position=_verts[index];
		_inventory.UseItem("clamp");
		_canClamp = _inventory.HasItem("clamp");
		//use this to clamp the blood
		float t = index/(float)_numPoints;
		//use this to animate the clamp
		Animator anim = c._clamp.GetChild(1).GetComponent<Animator>();
		
		//animation
		yield return new WaitForSeconds(0.2f);
		float timer=0;
		float dur=.5f;
		while(timer<dur){
			timer+=Time.deltaTime*2;
			anim.SetFloat("clamp",timer/dur*.9f);
			c._clampVal=Mathf.Lerp(1,.2f,timer/dur);
			GenerateMesh(true);
			yield return null;
		}

		c._clampAudio.pitch=1f;
		c._clampAudio.Play();

		//enable click detection
		c._clamp.GetChild(0).GetComponent<ClickDetection>().enabled=true;
		if(partial)
			ShowClampMenu(c._clamp);
		else
			HideClampMenu(c._clamp);
	}

	public void ShowClampMenu(Transform t){
		//disable click detection
		_blocker.enabled=true;
		//disable testcam
		_tc.enabled=false;
		//activate clamp sub menu
		t.GetComponentInChildren<ClampMenu>().ActivateOnTube(this);
	}

	public void HideClampMenu(Transform t){
		_tc.enabled=true;
		_blocker.enabled=false;
		foreach(Clamp c in _clamps){
			//get clamp
			if(t==c._clamp && c._clampIndex!=-1)
			{
				if(!c._initialized)
				{
					c._initialized=true;
					SendClamps();
				}
				return;
			}
		}
	}

	public void AdjustClamp(Transform t, float amount){
		foreach(Clamp c in _clamps){
			if(t==c._clamp && c._clampIndex!=-1)
			{
				StopAllCoroutines();
				c._clampValNorm=amount;
				//if initialized
				if(c._initialized)
					SendClamps();
				//send clamps
				StartCoroutine(AdjustClampR(c,amount));
				return;
			}
		}
	}

	public IEnumerator AdjustClampR(Clamp c,float amount){

		c._clampAudio.pitch=.9f;
		c._clampAudio.Play();
		//use this to clamp the tube
		int index = GetClosestIndex(c._clamp.position);
		c._clampIndex=index;
		c._clamp.position=_verts[index];
		
		//use this to animate the clamp
		Animator anim = c._clamp.GetChild(1).GetComponent<Animator>();
		
		//animation
		float dur=0.2f;
		float timer=0;
		float startClamp=anim.GetFloat("clamp");
		float endClamp = .9f*(1-amount);
		float startClampVal = c._clampVal;
		float endClampVal=(0.2f+amount*.8f);//*.6f+0.4f;
		while(timer<dur){
			timer+=Time.deltaTime;
			anim.SetFloat("clamp",Mathf.Lerp(startClamp,endClamp,timer/dur));
			c._clampVal=Mathf.Lerp(startClampVal,endClampVal,timer/dur);
			GenerateMesh(true);
			yield return null;
		}
		anim.SetFloat("clamp",endClamp);
		c._clampVal=endClampVal;
		GenerateMesh(true);

		yield return new WaitForEndOfFrame();
	}

	public void RemoveClamp(Transform t){
		if(_inventory.HasFreeHand()){
			foreach(Clamp c in _clamps){
				if(t==c._clamp)
					StartCoroutine(RemoveClampR(c));
			}
		}
	}

	public IEnumerator RemoveClampR(Clamp c){
		//disable click detection
		c._clamp.GetChild(0).GetComponent<ClickDetection>().enabled=false;
		//add to inventory
		_inventory.AddItem(_scissorClamp);

		c._clampAudio.pitch=.9f;
		c._clampAudio.Play();
		//use this to clamp the tube
		int index = GetClosestIndex(c._clamp.position);
		c._clampIndex=index;
		c._clamp.position=_verts[index];
		//use this to clamp the blood
		float t = index/(float)_numPoints;
		//use this to animate the clamp
		Animator anim = c._clamp.GetChild(1).GetComponent<Animator>();
		
		//animation
		float dur=0.5f;
		float timer=dur;
		while(timer>0f){
			timer-=Time.deltaTime*2;
			anim.SetFloat("clamp",timer/dur*.9f);
			c._clampVal=Mathf.Lerp(1,.4f,timer/dur);
			GenerateMesh(true);
			yield return null;
		}

		yield return new WaitForEndOfFrame();
		c._clampMesh.enabled=false;
		c._clampIndex=-1;
		c._initialized=false;
		c._clampValNorm=0;
		SendClamps();

		//this comes last because it calls StopAllCoroutines
		c._clamp.GetComponentInChildren<ClampMenu>().ResetClampMenu();
	}

	public float GetDistByIndex(int index){
		float length=0;
		//rem _tubeLength
		for(int i=1; i<_verts.Length; i++){
			length+=(_verts[i]-_verts[i-1]).magnitude;
			if(i==index)
				return length;
		}
		return -1;
	}

	public float GetNormDistByIndex(int index){
		float length=0;
		//rem _tubeLength
		for(int i=1; i<_verts.Length; i++){
			length+=(_verts[i]-_verts[i-1]).magnitude;
			if(i==index)
				return length/_tubeLength;
		}
		return -1;
	}

	public int GetIndexByDist(float dist){
		float length=0;
		int index=-1;
		float minDiff=1000;
		//rem _tubeLength
		for(int i=1; i<_verts.Length; i++){
			length+=(_verts[i]-_verts[i-1]).magnitude;
			float md = Mathf.Abs(length-dist);
			if(md<minDiff){
				minDiff=md;
				index=i;
			}
		}
		if(index==-1){
			Debug.Log("Error finding index");
			return 0;
		}
		return index;
	}

	public Vector3 GetWorldPosByDist(float dist){
		//return transform.InverseTransformPoint(_verts[index]);
		float t01 = dist;
		//calcualte segment based t
		float t=Mathf.Lerp(0,_path.GetNumCurveSegments(),t01);
		//sample from cubic bezier curve
		Vector3 pos = _path.GetPoint(t);
		return pos;
		//return _verts[index];
	}

	public Vector3 GetWorldForwardByDist(float dist){
		//_clamps[index]._clamp.position=_verts[_clamps[index]._clampIndex];
		float t01 = dist;
		//calcualte segment based t
		float t=Mathf.Lerp(0,_path.GetNumCurveSegments(),t01);
		//sample from cubic bezier curve
		Vector3 tan = _path.GetTangent(t);
		return tan;
	}

	public int GetIndexByNormDist(float norm){
		float length=0;
		int index=-1;
		float minDiff=1000;
		//rem _tubeLength
		for(int i=1; i<_verts.Length; i++){
			length+=(_verts[i]-_verts[i-1]).magnitude;
			float nL = length/_tubeLength;
			float md = Mathf.Abs(nL-norm);
			if(md<minDiff){
				minDiff=md;
				index=i;
			}
		}
		if(index==-1){
			Debug.Log("Error finding index");
			return 0;
		}
		return index;
	}

	public int GetClosestIndex(Vector3 target){
		float minSqrDist=1000f;
		int closestIndex=0;
		for(int i=0; i<_verts.Length; i++){
			Vector3 diff = target-_verts[i];
			float sqrDist=Vector3.Dot(diff,diff);
			if(sqrDist<minSqrDist){
				minSqrDist=sqrDist;
				closestIndex=i;
			}
		}
		return closestIndex;
	}

	public void ZoomToTube(){
		_mainCam.transform.GetComponent<TestCam>().LookAtTarget(_avgPos);
	}

	void OnMouseEnter(){
		//inventory is null during vr testing!
		if(_clampable && _inventory!=null){
			//see if we have in inventory
			if(_inventory.HasItem("clamp")){
				_canClamp=true;
			}
			else{
				_canClamp=false;
			}
			//if have flow sensor and that's the only thing we are holding
			//otherwise there's a chance we are holding a flow sensor and a clamp
			//and then we don't know which the user means to place
			if(_inventory.HasItem("FlowSensor")&&_inventory.HasFreeHand() && _flowSensorPrefab!=null){
				_canSense=true;
			}
			else{
				_canSense=false;
			}
			//see if we need to create a clamp or not (if there is a clamp with index -1)
			bool createNew=true;
			foreach(Clamp c in _clamps){
				if(c._clampIndex==-1)
				{
					createNew=false;
					if(!_canClamp){
						c._clampMesh.enabled=false;
					}
				}
			}
			if(createNew){
				//create clamp
				Transform clamp = Instantiate(_clampPrefab,_clampParent);
				clamp.GetChild(0).GetComponent<ClickDetection>()._onClick.AddListener(delegate{RemoveClamp(clamp);});
				clamp.GetChild(0).GetComponent<ClickDetection>()._onClickRight.AddListener(delegate{ShowClampMenu(clamp);});
				_clamps.Add(new Clamp(clamp,clamp.GetChild(0).GetComponent<SkinnedMeshRenderer>(),clamp.GetComponent<AudioSource>(),-1,1f));
			}
			//set up flow sensor
			if(_canSense)
			{
				if(_flow==null){
					//get flow sensor
					_flow=FindObjectOfType<FlowSensor>();
				}
				//see if we need to create flow sensor or not
				if(_flowSensor==null)
				{
					_flowSensor = Instantiate(_flowSensorPrefab);
					_flowSensorMesh=_flowSensor.GetComponent<MeshRenderer>();
					_flowSensorCol=_flowSensor.GetComponent<BoxCollider>();
					_flowSensor.GetComponent<ClickDetection>()._onClick.AddListener(delegate{RemoveFlowSensor();});
				}
				//add click detection 
				_flowSensorCol.enabled=false;
				_flowSensorMesh.enabled=true;
			}
		}
	}

	void OnMouseExit(){
		_canClamp=false;
		_canSense=false;
		foreach(Clamp c in _clamps){
			if(c._clampIndex==-1)
			{
				c._clampMesh.enabled=false;
			}
		}
		if(_flowSensor!=null && !_sensingFlow){
			_flowSensorMesh.enabled=false;
		}
	}

	public int GetNumClamps(int type=-1){
		int count=0;
		foreach(Clamp c in _clamps){
			if(c._clampIndex==-1)
				continue;
			if(type==-1 || c._type==type)
				count++;
		}
		return count;
	}

	public void SetupClamps(CircuitManager.ClampData[] clamps){
		//check old hoffman
		bool hoffPipVis=false;
		GameObject hoffm = GameObject.FindGameObjectWithTag("Hoffman");
		if(hoffm!=null && hoffm.transform.GetChild(2).gameObject.activeSelf)
			hoffPipVis=true;
		//remove old clamps
		for(int i=0; i<_clamps.Count; i++){
			Destroy(_clamps[i]._clamp.gameObject);
		}
		_clamps.Clear();
		if(clamps==null || clamps.Length==0){
			GenerateMesh(true);
			return;
		}

		//generate new clamps
		int index=0;
		bool echo=false;
		foreach(CircuitManager.ClampData c in clamps){
			if(c.type==0){//scissor clamp
				//instance go
				Transform clamp = Instantiate(_clampPrefab,_clampParent);
				//add onClick
				ClickDetection cd = clamp.GetChild(0).GetComponent<ClickDetection>();
				cd._onClick.AddListener(delegate{RemoveClamp(clamp);});
				cd._onClickRight.AddListener(delegate{ShowClampMenu(clamp);});
				cd.enabled=!_mqtt._hardware;

				//Instance Clamp
				_clamps.Add(new Clamp(clamp,clamp.GetChild(0).GetComponent<SkinnedMeshRenderer>(),clamp.GetComponent<AudioSource>(),-1,1f));

				//position clamp
				if(c.position<0 || c.position>_tubeLength)
				{
					c.position=_circuit._defaultSegmentOffsets[_data.segment]+(index)*0.3f;
					echo=true;
				}
				//#modify and here
				_clamps[index]._clampIndex=GetIndexByDist(c.position);
				//#check here
				_clamps[index]._clamp.position=_verts[_clamps[index]._clampIndex];
				float t01 = c.position;
				//calcualte segment based t
				float t=Mathf.Lerp(0,_path.GetNumCurveSegments(),t01);
				//sample from cubic bezier curve
				Vector3 tan = _path.GetTangent(t);
				_clamps[index]._clamp.right=Vector3.Cross(tan,Vector3.up);
				_clamps[index]._clamp.Rotate(_clamps[index]._clamp.right,-90f,Space.World);
				//set visible
				_clamps[index]._clampMesh.enabled=true;
				//close clamps per clamp value
				Animator anim = _clamps[index]._clamp.GetChild(1).GetComponent<Animator>();
				anim.SetFloat("clamp",.9f*(1-(c.value*.01f)));
				//close tube near clamp
				_clamps[index]._clampVal=(0.2f+(c.value*.01f)*.8f);
				//initialize
				_clamps[index]._initialized=true;
				index++;
			}
			else if(c.type==1 && _hoffmanPrefab!=null){
				//Set up hoffman
				Transform hoff = Instantiate(_hoffmanPrefab,_clampParent);
				//hook up events
				//ClickDetection cd = hoff.GetComponentInChildren<ClickDetection>();
				//cd._onClick.AddListener(
				//			delegate{_tc.LerpToTransform(hoff.GetChild(0));});
				Knob k = hoff.GetComponentInChildren<Knob>();
				k._OnValueChanged.AddListener(delegate{HoffmanRotated(hoff,k);});
				k._OnDragEnd.AddListener(delegate{SendClamps();});
				k._changeSpeed=_mqtt._hardware? 0 : -0.0025f;
				
				//Position, rotation, and add to _clamps list - so it can morph the tube
				_clamps.Add(new Clamp(hoff,null,null,0,1f));

				//position clamp
				if(c.position<0 || c.position>_tubeLength)
				{
					c.position=_circuit._defaultSegmentOffsets[_data.segment]+(index)*0.3f;
					echo=true;
				}
				_clamps[index]._clampVal=c.value/(float)100;
				StartCoroutine(SetHoffmanNextFrameR(k, index));
				//move the clamp up
				Transform children = k.transform.parent;
				children.localPosition = Vector3.up*_clamps[index]._clampVal*.02f;
				_clamps[index]._clampIndex=GetIndexByDist(c.position);
				_clamps[index]._clamp.position=_verts[_clamps[index]._clampIndex]+Vector3.down*0.005f;
				_clamps[index]._type=1;
				float t01 = c.position/_tubeLength;
				//calcualte segment based t
				float t=Mathf.Lerp(0,_path.GetNumCurveSegments(),t01);
				//sample from cubic bezier curve
				Vector3 tan = _path.GetTangent(t);
				_clamps[index]._clamp.forward=Vector3.Cross(tan,Vector3.up);

				//update nav panel to the location
				_mqtt._nav.SetHoffmanLoc(_clamps[index]._clamp.position);

				//keep panel vis
				if(hoffPipVis)
					_clamps[index]._clamp.GetChild(2).gameObject.SetActive(true);
			}
		}
		GenerateMesh(true);
		if(echo)
			SendClamps();
	}

	IEnumerator SetHoffmanNextFrameR(Knob k, int index){
		yield return null;
		k.JustSetValue(_clamps[index]._clampVal*k._maxVal);
		//if hardware mode
		if(EcmoCart._hardwareMode)
			_clamps[index]._clamp.gameObject.SetActive(false);
	}


	void SendClamps(){
		if(_mqtt==null)
			_mqtt = FindObjectOfType<MyMQTT>();
		CircuitManager.ClampData[] clamps = new CircuitManager.ClampData[GetNumClamps()];
		int counter=0;
		//compile clamp data
		foreach(Clamp c in _clamps){
			if(c._clampIndex!=-1)
			{
				//get clamp data
				CircuitManager.ClampData cd = new CircuitManager.ClampData();
				cd.type=c._type;
				cd.position = GetDistByIndex(c._clampIndex);
				cd.value=Mathf.RoundToInt(c._clampValNorm*100);
				clamps[counter]=cd;
				counter++;
			}
		}
		//update circuit manager
		_circuit.UpdateClampConfig(_data.segment,clamps);
	}

	public void SetLabelColor(string col){
		Color color;
		string colHex = "#"+col;
		if(!ColorUtility.TryParseHtmlString(colHex, out color))
			return;
		color.a=_tubeMat.GetColor("_Color").a;
		_tubeMat.SetColor("_Color", color);
	}

	public void SetBloodColor(string col){
		Color color;
		string colHex = "#"+col;
		if(!ColorUtility.TryParseHtmlString(colHex, out color))
		{
			return;
		}
		_col=color;
		if(gameObject.activeSelf)
		{
			if(_canSetBloodDirect || !_init || _data.flow_rate==0)
				FillBlood();
		}
	}

	public void FillBlood(){
		if(!gameObject.activeInHierarchy)
			return;
		if (_data.segment%2==0 && _data.segment!=4)
			StartCoroutine(FillBloodReverseR());
		else
			StartCoroutine(FillBloodR());

	}
	
	IEnumerator FillBloodR(){
		_tubeMat.SetColor("_BloodColorB",_tubeMat.GetColor("_BloodColor"));
		_tubeMat.SetColor("_BloodColor",_col);
		_tubeMat.SetFloat("_BloodFill",0);
		float l=0;
		if(_fillRate>0){
			while(l<_tubeLength){
				l+=_fillRate*Time.deltaTime;
				_tubeMat.SetFloat("_BloodFill",l/_tubeLength);
				yield return null;
			}
		}
		else{
			yield return null;
		}
		_tubeMat.SetFloat("_BloodFill",1);
		_tubeMat.SetFloat("_BloodFill2",1);

		//Debug.Log("Done filling "+name+ "'s blood - now for the next...");
		//Debug.Log(name + " has "+_next.Length + " children");
		foreach(Tube b in _next)
		{
			if(b==null)
			{
				//Debug.Log("null child");
				continue;
			}
			//Debug.Log(b.name);
			if(b!=null)
				b.FillBlood();
		}
		RemoveNullNexts();
	}

	IEnumerator FillBloodReverseR(){
		_tubeMat.SetColor("_BloodColorB",_tubeMat.GetColor("_BloodColor"));
		_tubeMat.SetColor("_BloodColor",_col);
		_tubeMat.SetFloat("_BloodFill",0);
		_tubeMat.SetFloat("_BloodFill2",1);
		float l=0;
		bool cancel=false;
		if(_fillRate>0){

			while (l<_tubeLength && _fillRate>0){
				l+=_fillRate*Time.deltaTime;
				_tubeMat.SetFloat("_BloodFill2",1-l/_tubeLength);
				yield return null;
			}
			if(_fillRate<=0)
				cancel=true;
		}
		else
			yield return null;
		if(!cancel){
			_tubeMat.SetFloat("_BloodFill",1);
			_tubeMat.SetFloat("_BloodFill2",1);

			//Debug.Log("Done filling "+name+ "'s blood - now for the next...");
			//Debug.Log(name + " has "+_next.Length + " children");
			foreach(Tube b in _next)
			{
				if(b==null)
				{
					//Debug.Log("null child");
					continue;
				}
				//Debug.Log(b.name);
				if(b!=null)
					b.FillBlood();
			}
			RemoveNullNexts();
		}
	}

	public void ConfigureClamps(CircuitManager.ClampData[] clamps){
		if(_meshR.enabled && _clampable)
		{
			if(clamps==null){
				clamps = _circuit.GetClamps(_data.segment);
			}
			SetupClamps(clamps);
		}
	}

	public void ConfigureTube(TubeData cd){
		//set raw data
		_data.blood_color=cd.blood_color;
		_data.label_color=cd.label_color;
		_data.tubing_size=cd.tubing_size==0 ? 0.5f : cd.tubing_size;
		_data.flow_rate=cd.flow_rate;

		//set derived data
		if(!_sizeLock)
		{
			_radius=_data.tubing_size*.5f*2.5f*.01f;//convert to radius and them to cm then to m
		}
		_fillRate = (1 / (250 * Mathf.PI * Mathf.Pow(_radius * 2, 2f))) * _data.flow_rate * .5f;

		//For tubes with an active mesh
		if(_meshR.enabled){

			//need to redraw after changing radius
			GenerateMesh(true);
			SetBloodColor(_data.blood_color);
			SetLabelColor(_data.label_color);
			if(_sensingFlow){
				_flow.SetTarget(_data.flow_rate);
			}
		}
	}

	public void EnableTube(bool on){
		if(!gameObject.activeInHierarchy)
			return;
		_meshR.enabled=on;
		//_clampable=on;
		_mc.enabled=on;
		if(on && transform.gameObject.activeSelf){
			//dual lumin check
			StartCoroutine(CheckDualLumin());
			ConfigureTube(_data);
		}
		else//no flow sensor
		{
			//clear flow sensor
			SetFlowSensor(false);
		}
	}

	IEnumerator CheckDualLumin(){
		yield return null;
		bool isDual=false;
		if(_prevTarget.childCount>0){
			Transform can = _prevTarget.GetChild(0);
			foreach(Transform t in can){
				if(t.name=="VenCon" && _data.segment==0 ||
						t.name=="ArtCon" && _data.segment>0)
				{
					_target=t;
					isDual=true;
				}
			}
		}
		if(!isDual)
			_target=_prevTarget;
	}

	public void HoffmanRotated(Transform hoff, Knob k){
		float frac = k._val/k._maxVal;
		_clamps[0]._clampVal=frac;
		_clamps[0]._clampValNorm=frac;
		GenerateMesh(true);
		Transform children = k.transform.parent;
		children.localPosition = Vector3.up*frac*.02f;
		int h = Mathf.RoundToInt(frac*100);
		if(h!=_lastHoff&&Time.time-_lastSendTime>_mqtt._minUpdateDelay)
		{
			//gotta replace this call with a generic clamp call
			//_mqtt.ForceHoffman(_data.segment,Mathf.RoundToInt(frac*100));
			_lastHoff=h;
			SendClamps();
			_lastSendTime=Time.time;
		}
	}

	//gotta replace this with a generic clamp configure method
	//val from 1 to 100
	public void ConfigureHoffman(int val){
		if(_meshR.enabled && _clamps.Count>0){
			//set the tube width
			float frac = val/100f;
			//todo rethink this
			//may not be ideal to assume the hoffman is _clamps[0] - 
			_clamps[0]._clampVal=frac;
			GenerateMesh(true);
			//set the knob val so it does not get reset on next interaction
			Knob k = _clamps[0]._clamp.GetComponentInChildren<Knob>();
			k.JustSetValue(frac*k._maxVal);
			//move the clamp up
			Transform children = k.transform.parent;
			children.localPosition = Vector3.up*frac*.02f;
		}
		else{
			Debug.Log($"Error configuring hoffman. Tube {_data.segment} not active or no clamps were present");
		}
	}

	public void EnableHoffman(bool en){
		if(_data.segment==3&&_clamps.Count>0)
			_clamps[0]._clamp.gameObject.SetActive(en);
	}

	public void RemoveFlowSensor(){
		_sensingFlow=false;
		_inventory.AddItem(_flowSensorItem);
		_flowSensorMesh.enabled=false;
		_flowSensorCol.enabled=false;
		_flowSensor.GetComponentInChildren<Tube>().RemoveTube();
	}

	public void SetFlowSensor(bool flow){
		_sensingFlow=flow;
		//if already have a flow sensor - disable
		if(!flow && _flowSensor!=null){
			_flowSensorMesh.enabled=false;
			_flowSensorCol.enabled=false;
			_flowSensor.GetComponentInChildren<Tube>().RemoveTube();
		}
		else if(flow){
			//if already have sensor - enable it
			if(_flowSensor!=null){
				_flowSensorMesh.enabled=true;
				_flowSensorCol.enabled=true;
			}
			else{
				//else we need to instance
				_flowSensor = Instantiate(_flowSensorPrefab);
				_flowSensorMesh=_flowSensor.GetComponent<MeshRenderer>();
				_flowSensorCol=_flowSensor.GetComponent<BoxCollider>();
				_flowSensor.GetComponent<ClickDetection>()._onClick.AddListener(delegate{RemoveFlowSensor();});
				//add click detection 
				_flowSensorCol.enabled=true;
				_flowSensorMesh.enabled=true;

				//place it
				float t01 = 0.2f;
				float t=Mathf.Lerp(0,_path.GetNumCurveSegments(),t01);
				Vector3 tan = _path.GetTangent(t);
				_flowSensor.position=_path.GetPoint(t);
				_flowSensor.forward = Vector3.Cross(tan,Vector3.up);

				//connect it
				_flowSensor.GetComponentInChildren<Tube>().ConnectToTarget("FlowSensorPort");
			}
			//check if player holding one or there is one in the stand
			if(_inventory.HasItem("FlowSensor"))
				_inventory.UseItem("FlowSensor");
			//check if the item stand has one, then we want to hide it
			if(_flow==null)
				_flow=FindObjectOfType<FlowSensor>();
			_flow.TryHideSensor();
		}
	}

	public void ConnectToTarget(string targ){
		//todo code some things
		_target=GameObject.Find(targ).transform;
		for(int i=transform.childCount-1; i>=0; i--)
			Destroy(transform.GetChild(i).gameObject);
		for(int i=0; i<2; i++)
		{
			GameObject p = new GameObject();
			p.transform.SetParent(transform);
		}
		StartCoroutine(ConnectToTargetR());
	}

	IEnumerator ConnectToTargetR(){
		yield return null;
		//position linearly
		for(int i=0; i<transform.childCount; i++){
			float factor = (i+1)/(float)(transform.childCount+1);
			Vector3 p = Vector3.Lerp(_root.position,_target.position,factor);
			p.y+=.1f;
			transform.GetChild(i).position=p;
		}
		//raycast downward
		for(int i=0; i<transform.childCount; i++){
			RaycastHit hit;
			if(Physics.Raycast(transform.GetChild(i).position,Vector3.down, out hit)){
				transform.GetChild(i).position=hit.point;
			}
		}
		yield return null;
		_meshR.enabled=true;
	}

	public void RemoveTube(){
		_target=null;
		_meshR.enabled=false;
	}

	public void EnableClamps(bool e){
		foreach(Clamp c in _clamps){
			if(c._type==0)
				c._clamp.GetChild(0).GetComponent<ClickDetection>().enabled=e;
		}
	}

	public void AddNextTube(Tube t){
		List<Tube> ts = new List<Tube>();
		foreach(Tube tube in _next)
			ts.Add(tube);
		ts.Add(t);
		_next = ts.ToArray();
	}

	void RemoveNullNexts(){
		List<Tube> ts = new List<Tube>();
		foreach(Tube tube in _next)
		{
			if(tube!=null)
				ts.Add(tube);
		}
		_next = ts.ToArray();
	}

	public void DebugNexts(){
		Debug.Log(name+ " next tubes...");
		foreach(Tube t in _next){
			if(t==null)
				Debug.Log("Null");
			else
				Debug.Log(t.name);
		}
	}

	void OnDrawGizmos(){
		/*
		if(_root==null||_target==null)
			return;
		Gizmos.color = Color.green;
		Gizmos.DrawSphere(_root.position,.02f);
		Gizmos.color = Color.blue;

        if (_points!=null){
			for (int i = 1; i < _points.Length - 1; i++){
				Gizmos.DrawSphere(_points[i], 0.02f);
			}
		}

		Gizmos.color = Color.red;
		Gizmos.DrawSphere(_target.position,.02f);
		*/

		//Gizmos.color = Color.yellow;
		//Vector3 mid = Vector3.Lerp(_root.position,_target.position,0.5f);
		//Gizmos.DrawSphere(mid,0.01f);
	}
}
