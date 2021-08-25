using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Invos : MonoBehaviour
{
	bool _powered;
	public CanvasGroup _screen;
	RawImage _bg;
	public Texture _splashA, _splashB, _landingScreen;
	public Texture _home;
	public GameObject _landingClock;
	public GameObject _homeClock;
	public GameObject _bottomOptions;
	public GameObject _graphs;
	int _state;
	public Text[] _menuOptions;
	Material _mat;
	MeshRenderer _mesh;
	ClickDetection _basket;
	GameObject [] _sensorTargets;
	float _recordTimer;
	MyMQTT _mqtt;
	static int _graphWidth=512;
	static int _graphHeight=256;
	static float _secsPerGraph=3600;

	[System.Serializable]
	public class ChannelData{
		public CanvasGroup _cg;
		[HideInInspector]
		public GameObject _bl;
		float _base;
		Text _baseline;
		[HideInInspector]
		public float _lastVal;
		Text _current;
		Transform _arrow;
		Text _delta;
		Vector3 _up, _down;
		[HideInInspector]
		public Color _color;

		public void Init(){
			_cg.alpha=0;
			_bl = _cg.transform.Find("BL").gameObject;
			_baseline=_bl.transform.Find("BaseLine").GetComponent<Text>();
			_bl.SetActive(false);
			Transform rso2 = _bl.transform.Find("rso2");
			_arrow = rso2.GetChild(0);
			_delta = rso2.GetChild(1).GetComponent<Text>();
			_current = _cg.transform.Find("Current").GetComponent<Text>();
			_lastVal=0f;
			_up=_arrow.localScale;
			_down=_up;
			_down.y*=-1f;
			_color = _cg.transform.Find("Channel").GetComponent<RawImage>().color;
		}

		public void ShowData(bool b){
			_cg.alpha=b?1f:0f;
		}

		public void SetBaseline(){
			_bl.SetActive(true);
			//copy values
			_base=_lastVal;
			_baseline.text=_base.ToString("0");
			//set arrow up
			_arrow.localScale=_up;
			//set % to 0
			_delta.text = "0 %";
		}

		public void SetVal(float f){
			_lastVal=f;
			_current.text=_lastVal.ToString("0");
			float percent = _lastVal/_base;
			if(percent>=1f){
				_delta.text=(percent-1).ToString("0 %");
				_arrow.localScale=_up;
			}
			else{
				_delta.text=(1-percent).ToString("0 %");
				_arrow.localScale=_down;
			}
		}
	}

	[System.Serializable]
	public class Sensor{
		public GameObject _sensor;
		public GameObject _sensorCable;
		[HideInInspector]
		public bool _placed;
		public ChannelData _channelData;
		int _lastPix;
		Vector3 _originalPos;
		Quaternion _originalRot;
		bool _cdInit=false;

		public void Init(){
			_originalPos=_sensor.transform.position;
			_originalRot=_sensor.transform.rotation;
		}

		public void ActivateSensor(bool active){
			_sensor.SetActive(active);
			_sensorCable.SetActive(active);
			if(!_cdInit){
				_channelData.Init();
				_cdInit=true;
			}
		}

		public void ShowData(bool b){
			_channelData.ShowData(b);
		}

		public void SetBaseline(){
			_channelData.SetBaseline();
		}

		public void SetVal(float f){
			_channelData.SetVal(f);
		}

		public void Place(GameObject target, bool disconnect){
			if(!disconnect)
			{
				_sensor.transform.position=target.transform.position;
				_sensor.transform.rotation=target.transform.rotation;
				_sensor.GetComponent<ClickDetection>().enabled=false;
			}
			else{
				_sensor.transform.position=_originalPos;
				_sensor.transform.rotation=_originalRot;
				_sensor.GetComponent<ClickDetection>().enabled=true;
			}
			_placed=!disconnect;
		}

		public void RecordPixel(int pos,Texture2D buffer,Vector4 bounds){
			float norm = Mathf.InverseLerp(bounds.y,bounds.w,_channelData._lastVal);
			int pix = Mathf.RoundToInt(norm*Invos._graphHeight);
			//Debug.Log($"sensor {_sensor.name} writing to pix {pix}");
			int min;
			int max;
			if(pix<_lastPix){
				min=pix;
				max=_lastPix;
			}
			else{
				min=_lastPix;
				max=pix;
			}
			for(int i=min; i<=max; i++)
				buffer.SetPixel(pos,i,_channelData._color);
			_lastPix=pix;
		}

	}

	[System.Serializable]
	public class SensorCable{
		public GameObject _port;
		public GameObject _ampCable;
		public GameObject _amp;
		[HideInInspector]
		public AudioSource _click;
		public RawImage _graph;
		Texture2D _graphBuffer;
		Vector4 _graphBounds;
		[HideInInspector]
		public bool _recording;
		int _lastPixel;
		Text _startTime;
		Text _endTime;
		Invos _invos;
		Sensor _sensorA;
		Sensor _sensorB;

		public void Init(Invos i,Sensor a, Sensor b){
			_invos=i;
			_port.SetActive(false);
			_amp.SetActive(false);
			_click=_ampCable.GetComponent<AudioSource>();
			_ampCable.SetActive(false);
			_sensorA=a;
			_sensorB=b;
			_sensorA.ActivateSensor(false);
			_sensorB.ActivateSensor(false);
			InitGraph(i);
			_startTime=_graph.transform.GetChild(0).GetComponent<Text>();
			_endTime=_graph.transform.GetChild(1).GetComponent<Text>();
		}
		
		public void Connect(){
			_port.SetActive(false);
			_ampCable.SetActive(true);
			_amp.SetActive(true);
			_sensorA.ActivateSensor(true);
			_sensorB.ActivateSensor(true);
		}

		public void CheckForRecording(){
			if(_sensorA._placed && _sensorB._placed){
				//both sensors placed
				if(_invos._powered&&_invos._state>=1)//and in home screen
				{
					_sensorA.ShowData(true);
					_sensorB.ShowData(true);
					//start data collection
					_recording = true;
				}
				System.DateTime n =System.DateTime.Now;
				_startTime.text=n.ToString("HH:mm");
				_endTime.text=n.AddSeconds(Invos._secsPerGraph).ToString("HH:mm");
			}
		}

		public void RevealPort(){
			if(!_amp.activeSelf)
				_port.SetActive(true);
		}

		public void HidePort(){
			if(!_amp.activeSelf)
				_port.SetActive(false);
		}

		public void PlaceSensor(GameObject go,bool ab,bool disconnect=false){//ab is 0 = a, 1= b
			if(!_amp.activeSelf)
				Connect();

			go.SetActive(disconnect);
			if(!ab)
			{
				_sensorA.Place(go,disconnect);
			}
			else{
				_sensorB.Place(go,disconnect);
			}
			CheckForRecording();
		}

		public void SetBaselines(){
			_sensorA.SetBaseline();
			_sensorB.SetBaseline();
		}

		public void InitGraph(Invos i){
			_graphBounds = new Vector4(0,30,Invos._secsPerGraph,100);
			_graphBuffer = new Texture2D(Invos._graphWidth,Invos._graphHeight,TextureFormat.ARGB32,false);
			float barH=Mathf.InverseLerp(_graphBounds.y,_graphBounds.w,40);
			for(int y=0; y<_graphBuffer.height;y++){
				for(int x=0; x<_graphBuffer.width; x++){
					if(Mathf.Abs((float)y/_graphBuffer.height-barH)<=0.01f &&
							((float)x/_graphBuffer.width)%.02f<=.01f)
						_graphBuffer.SetPixel(x,y,Color.red);
					else
						_graphBuffer.SetPixel(x,y,Color.black);
				}
			}
			_graphBuffer.Apply();
			_graph.texture=_graphBuffer;
			_lastPixel=0;
		}

		public void UpdateGraph(float time, Invos i){
			if(!_recording)
				return;
			int pix = Mathf.FloorToInt(time/_graphBounds.z*Invos._graphWidth);
			if(pix>_lastPixel){
				if(pix>=Invos._graphWidth-2){
					SlideTex(_graphBuffer);
					pix=Invos._graphWidth-2;
				}
				_sensorA.RecordPixel(pix,_graphBuffer,_graphBounds);
				_sensorB.RecordPixel(pix,_graphBuffer,_graphBounds);
				_lastPixel=pix;
				_graphBuffer.Apply();
			}
		}

		public void SlideTex(Texture2D buffer){
			Color[] cols = buffer.GetPixels();
			for(int y=0; y<Invos._graphHeight; y++){
				for(int x=0; x<Invos._graphWidth-1; x++){
					int i = y*Invos._graphWidth+x;
					if(cols[i]==Color.red){
						//ignore
					}
					else if(cols[i+1]==Color.red){
						buffer.SetPixel(x,y,Color.black);
					}
					else{
						buffer.SetPixel(x,y,cols[y*Invos._graphWidth+x+1]);
					}
				}
			}
			//todo update timestamp on left and right as well
			System.DateTime n =System.DateTime.Now;
			_endTime.text=n.ToString("HH:mm");
			_startTime.text=n.AddSeconds(-Invos._secsPerGraph).ToString("HH:mm");
		}

		public void Reset(Invos i){
			_sensorA.ShowData(false);
			_sensorB.ShowData(false);
			HidePort();
			_recording=false;
			InitGraph(i);
		}
	}

	public SensorCable _c12;
	public SensorCable _c34;
	public Sensor _s1;
	public Sensor _s2;
	public Sensor _s3;
	public Sensor _s4;

    // Start is called before the first frame update
    void Start()
    {
		_bg = _screen.GetComponent<RawImage>();
		_landingClock.SetActive(false);
		_homeClock.SetActive(false);
		_bottomOptions.SetActive(false);
		_graphs.SetActive(false);
		_screen.alpha=0f;
		_state=0;
		_mesh = GetComponent<MeshRenderer>();
		_mat = _mesh.materials[0];
		_mat.SetFloat("_EmissionPower",0);
		_s1.Init();
		_s2.Init();
		_s3.Init();
		_s4.Init();
		_c12.Init(this,_s1,_s2);
		_c34.Init(this,_s3,_s4);
		_basket=transform.parent.Find("basket").GetComponent<ClickDetection>();
		
		//get sensor targets
		ClickDetection [] clickys = GameObject.FindGameObjectWithTag("Patient").GetComponentsInChildren<ClickDetection>();
		_sensorTargets = new GameObject[4];
		for(int i=0; i<clickys.Length; i++)
		{
			GameObject go = clickys[i].gameObject;
			if(go.name[0]=='s'){//s for sensor, others would be c for cannula
				int sensorChannel = int.Parse(go.name.Split('-')[1]);
				_sensorTargets[sensorChannel-1]=go;
				go.SetActive(false);
			}
		}
		
		//mqtt
		MyMQTT [] qts = FindObjectsOfType<MyMQTT>();
		foreach(MyMQTT qt in qts){
			if(qt.gameObject.tag=="GameController")
				_mqtt=qt;
		}
		Power(false);
    }

    // Update is called once per frame
    void Update()
    {
		_recordTimer+=Time.deltaTime;
		_c12.UpdateGraph(_recordTimer,this);
		_c34.UpdateGraph(_recordTimer,this);
    }

	public void TogglePower(){
		Power(!_powered,false,true);
	}

	public void Power(bool on,bool home=false,bool pub=false){
		if(pub)
			_mqtt.SetInvosPower(on);
		if(on && !_powered)
			StartCoroutine(PowerR(home));
		else if(!on){
			_powered=false;
			_screen.alpha=0;
			_mat.SetFloat("_EmissionPower",0);
			_landingClock.SetActive(false);
			_bottomOptions.SetActive(false);
			_graphs.SetActive(false);
			_homeClock.SetActive(false);
			_state=0;
			_c12.Reset(this);
			_c34.Reset(this);
		}
	}

	IEnumerator PowerR(bool home){
		_mat.SetFloat("_EmissionPower",200);
		_screen.alpha=1;
		//show splash screen
		_bg.texture=_splashA;
		yield return new WaitForSeconds(.3f);
		//show some other screen
		//_bg.texture=_splashB;
		//yield return new WaitForSeconds(1f);
		//show landing screen
		_bg.texture=_landingScreen;
		_landingClock.SetActive(true);
		_bottomOptions.SetActive(true);
		_powered=true;
		if(home)
			GoHome();
	}

	public void PowerAndHome(bool powered){
		Power(powered,powered);
	}

	public void GoHome(){
		if(_powered){
			_bg.texture=_home;
			_landingClock.SetActive(false);
			_homeClock.SetActive(true);
			_state=1;
			_menuOptions[0].text="BASELINE MENU";
			_menuOptions[1].text="EVENT MARK";
			_menuOptions[2].text="ALARM AUDIO ON/OFF";
			_menuOptions[3].text="NEXT MENU";
			_c12.RevealPort();
			_c34.RevealPort();
			_c12.CheckForRecording();
			_c34.CheckForRecording();
			_graphs.SetActive(true);
			_recordTimer=0;
		}
	}

	public void MenuButton(int id){
		switch(_state){
			case 1:
				if(id==1){
					Debug.Log("Go go baseline menu");
					_state=2;
					_menuOptions[0].text="SET BASELINES";
					_menuOptions[1].text="SET CHANNEL";
					_menuOptions[2].text="MANUAL SET";
					_menuOptions[3].text="PREVIOUS MENU";
				}
				break;
			case 2:
				if(id==1){
					Debug.Log("Setting all baselines");
					_c12.SetBaselines();
					_c34.SetBaselines();
				}
				else if(id==2){
					Debug.Log("Ooh set channel");
					_menuOptions[0].text="CHANNEL 1 SET";
					_menuOptions[1].text="CHANNEL 2 SET";
					_menuOptions[2].text="CHANNEL 3 SET";
					_menuOptions[3].text="CHANNEL 4 SET";
					_state=3;
				}
				break;
			case 3:
				if(id==1){
					Debug.Log("Setting ch 1 baseline");
				}
				else if(id==2){
					Debug.Log("Setting ch 2 baseline");
				}
				else if(id==3){
					Debug.Log("Setting ch 3 baseline");
				}
				else if(id==4){
					Debug.Log("Setting ch 4 baseline");
				}
				break;
			case 4:
				break;
		}
	}

	public void ConnectAmplifier(int ampIndex,int port,bool moveCam=true){
		Debug.Log("Connecting amp: "+ampIndex +" to port: "+port);
		if(port==0){
			StartCoroutine(SnapIn(_c12,moveCam));
		}
		else if(port==1){
			StartCoroutine(SnapIn(_c34,moveCam));
		}
	}

	IEnumerator SnapIn(SensorCable sc,bool moveCam){
		sc.Connect();
		if(moveCam)
			_basket._onClick.Invoke();
		float timer=0;
		Vector3 conPos = sc._port.transform.localPosition;
		Vector3 disPos = conPos+Vector3.left*.25f;
		sc._port.transform.localPosition=disPos;
		yield return null;
		while(timer<1f){
			timer+=Time.deltaTime;
			sc._port.transform.localPosition=Vector3.Lerp(disPos,conPos,timer);
			yield return null;
		}
		sc._click.Play();
	}

	public void SelectSensor(int sensIndex){
		//enable the target
		_sensorTargets[sensIndex].SetActive(true);
		//hide blanket
		FindObjectOfType<Blanket>().Remove();
	}

	public void CommandPlaceSensor(int channelIndex, int siteIndex){
		bool disconnect=siteIndex==-1;
		switch(channelIndex){
			case 0:
				_c12.PlaceSensor(_sensorTargets[channelIndex],false,disconnect);
				break;
			case 1:
				_c12.PlaceSensor(_sensorTargets[channelIndex],true,disconnect);
				break;
			case 2:
				_c34.PlaceSensor(_sensorTargets[channelIndex],false,disconnect);
				break;
			case 3:
				_c34.PlaceSensor(_sensorTargets[channelIndex],true,disconnect);
				break;
			default:
				break;
		}
	}

	public void PlaceSensor(int channelIndex){
		switch(channelIndex){
			case 0:
				_c12.PlaceSensor(_sensorTargets[channelIndex],false);
				_mqtt.PlaceSensor(0,0);
				break;
			case 1:
				_c12.PlaceSensor(_sensorTargets[channelIndex],true);
				_mqtt.PlaceSensor(1,1);
				break;
			case 2:
				_c34.PlaceSensor(_sensorTargets[channelIndex],false);
				_mqtt.PlaceSensor(2,14);
				break;
			case 3:
				_c34.PlaceSensor(_sensorTargets[channelIndex],true);
				_mqtt.PlaceSensor(3,15);
				break;
			default:
				break;
		}
		_basket._onClick.Invoke();
	}

	public void SetValue(string top, float f){
		switch(top){
			case "NIRS/INVOS/CH1/value":
				//_c12.SetVal(false, f);
				_s1.SetVal(f);
				break;
			case "NIRS/INVOS/CH2/value":
				//_c12.SetVal(true, f);
				_s2.SetVal(f);
				break;
			case "NIRS/INVOS/CH3/value":
				//_c34.SetVal(false, f);
				_s3.SetVal(f);
				break;
			case "NIRS/INVOS/CH4/value":
				//_c34.SetVal(true, f);
				_s4.SetVal(f);
				break;
		}
	}

	public void SetVis(bool vis){
		Transform stand = transform.parent;
		stand.GetComponent<MeshRenderer>().enabled=vis;
		//basket
		stand.GetChild(1).gameObject.SetActive(vis);
		//cables
		stand.GetChild(2).gameObject.SetActive(vis);
		//invos itself
		transform.GetComponent<MeshRenderer>().enabled=vis;
		transform.GetComponent<ClickDetection>().enabled=vis;
		transform.GetComponent<BoxCollider>().enabled=vis;
		foreach(Transform t in transform)
			t.gameObject.SetActive(vis);

	}
}
