//MyMQTT.cs
//
//Description: Manages config files and mqtt messaging
//This thing is a singleton, so may references obtained during Start are overwritten on scene load
//Lists function handles for Subscriptions
//Lists helper functions for Publishing
//Describes various small data types for use of json serialization over mqtt
//Helps with file i/o alongside RoomConfig.cs
//
//tldr:
//Gets a reference to basically anything and everything visualized in the simulation.
//This thing is like the command center
//


using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using System.Reflection;
using System.Globalization;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Networking;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using MqttLogger;
#if UNITY_EDITOR
	using UnityEditor;
#endif

public class MyMQTT : MonoBehaviour
{

	//method signature for [sub] handlers
	public delegate void RequestHandler(string topic, byte[] data);
	
	//data structure for passing commands from mqtt thread to main Update loop
	public struct StringByteArr {
		public string _str;
		public byte[] _bytes;
	}

	//mqtt data
	MqttClient _client;
	string _clientId;
	[HideInInspector]
	public string _prefix;
	byte _qos;//quality of service
	int _reconnectDelay;
	string _version;
	string _build;
	string _keyRev;
	bool _reconnecting;//true when in reconnecting routine
	bool _killTask;//set this to break free of reconnect loop
	[HideInInspector]
	public string _brokerAddress;
	int _brokerPort;

	//vitals
	TraceManager _traces;//handles the rendering of vital traces
	[HideInInspector]

	//in game camera
	public TestCam _cameraController; //Accessing camera script to assign values.

	//Config Object
	[SerializeField] private InteractiveConfig _config = null;

	//this stuff needs a rework to be scene independent
	//used for a subtle idicator of online status
	MeshRenderer _monitorMesh;
	int _ledIndex = 2;
	bool _greenLight=true;

	//These guys define what topics are sent and received
	Dictionary<int,string> _subs = new Dictionary<int,string>();
	Dictionary<string,string> _pubs = new Dictionary<string,string>();
	//This maps sub topics to methods assuming they have appropriate signature
	Dictionary<string,MethodInfo> _subActions = new Dictionary<string,MethodInfo>();
	[HideInInspector]
	//used for basic commands
	public List<StringByteArr> _commands = new List<StringByteArr>();
	//used for scalar commands which are processed in a separate batch
	List<StringByteArr> _scalarCommands = new List<StringByteArr>();

	//thread lock
	[HideInInspector]
	public bool _commandLock=false;

	//timing info
	[HideInInspector]
	public float _minUpdateDelay=.5f;//this will be sent as a signal
	float _rpmUpdatePeriod=.5f;//this will be sent as a signal
	float _updateRate=.1f;//random number for now
	System.DateTime _lastUpdate;
	
	//ecmo pump and circuit
	CircuitManager _circuitMan;
	CircuitMenu _circuitMenu;
	Cardiohelp _cardiohelp;
	EcmoCart _ecmo;

	//ventilator
	DragerStats _drager;

	//Patient params
	[HideInInspector]
	public Patient _patient;
	[HideInInspector]
	public string _patientId;

	DisplayManager _display;
	
	//state machine - status and handshake stuff
	public enum CommStates {TESTING,HANDSHAKE,STANDBY,RUN,EXIT};
	public CommStates _cState = CommStates.HANDSHAKE;
	
	//loading canvas
	Canvas _loadCanvas;

	//cannulas
	CannulaMenu _cannulas;

	//air mixer
	AirMixer _airMixer;

	//Room config
	[HideInInspector]
	public NavPanel _nav;
	[HideInInspector]
	public RoomConfig _room;
	EquipmentManager _equip;

	//Quantum (specific heart-lung machine developed for Spectrum Medical demo)
	Quantum _quantum;

	//Media Monitor
	MediaMonitor [] _media;

	//Roles / players
	RoleManager _roles;

	//editor / instructor mode
	int _mode=0;
	[HideInInspector]
	public EditorMenu _editor;

	//ibga - inline blood gas analysis
	Terumo _terumo;
	Ibga _ibga;

	//fluids
	Urometer _urine;
	IVmenu _ivMenu;
	Hemofilter _hemo;
	Alaris _alaris;

	//forms
	ClipboardHelper _clip;
	
	//oximeter
	Invos _invos;

	//bed
	BedManager _bed;

	//scenario
	ScenarioManager _scenario;
	Clock [] _clocks;
	[HideInInspector]
	public bool _hardware;

	//scene management
	[HideInInspector]
	public int _sceneCode;//used by roomConfig
	[HideInInspector]
	public int _prevSceneCode;
	SceneSelector _scene;

	//comms
	VoiceComs _voiceCom;

	//Path for file io
	//working path: USER SPECIFIC bucket for settings, config. We used to use StreamingAssets
	//but that does not work on multi-user systems
	//shared folder: A folder accessible by both learner and instructor software. Currently we
	//are using dropbox folder for testing
	[HideInInspector]
	public string _workDirectory;
	[HideInInspector]
	public string _sharedFolder;
	[HideInInspector]
	public string _scenCatalogPath;
	string _iniPath;
	string _topicTablePath;
	bool [] _flags;

	//platform stuff
	[HideInInspector]
	public bool _arBuild;

	//singleton
	public static bool _instanced=false;
	void Awake(){
		if(!_instanced){
			_instanced=true;
			DontDestroyOnLoad(transform.gameObject);
		}
		else{
			Destroy(transform.gameObject);
		}
	}

	// Start is called before the first frame update
	void Start()
	{
		SceneManager.sceneLoaded += OnSceneLoaded;

		//tag this as THE singleton because for some reason the Destroy method takes a frame to take effect
		gameObject.tag="GameController";

		//used for update frequency of continuous pubs
		_lastUpdate = System.DateTime.Now;

		//get singleton references
		_display=GetComponent<DisplayManager>();

		//get scene refernces
		GetSceneReferences();

		//CheckStatusLed();

		//ensure that loading screen is off at the start because sometimes we may have
		//left it up in edit mode
		_loadCanvas = transform.Find("LoadCanvas").GetComponent<Canvas>();
		ShowLoadingScreen(false);

		//determine build type
#if UNITY_ANDROID
		_arBuild=true;
#elif UNITY_EDITOR
		//simulate ar build while build target is set to android
		if(EditorUserBuildSettings.activeBuildTarget==BuildTarget.Android)
			_arBuild=true;
#else
		_arBuild=false;
#endif

		//get ini paths, and copy into working directory if not already there
		if(_arBuild){
			_workDirectory=Application.persistentDataPath;
			_flags = new bool[2];
			_iniPath=_workDirectory+"/EdgeAR.ini";
			_topicTablePath=_workDirectory+"/TopicTable.ini";
			StartCoroutine(CopyToWorkDirR("EdgeAR.ini",0));
			StartCoroutine(CopyToWorkDirR("TopicTable.ini",1));
			StartCoroutine(InitWhenReady());
		}
		else{
			string localAppData = System.Environment.ExpandEnvironmentVariables("%localappdata%").Replace("\\","/");
			_workDirectory=localAppData+"/Cal3D";
			_iniPath = GetWorkPath("EdgeCalifia3D.ini");
			_topicTablePath = GetWorkPath("TopicTable.ini");
			//immediately init if not ar build
			Init();
		}
	}

	public void Init(bool reInit=false){
		//load EdgeCalifia3D.ini - this is kind of the "main" config file for comms
		LoadIni();
		
		//Retrieve mqtt logs
#if UNITY_EDITOR
		LoggerManager.LoadLogHistory();
#endif

		//load topics, callbacks, and aliases
		LoadPubsAndSubs();

		//start up mqtt, sub to topics, setup callbacks
		if(reInit)
			_client.Disconnect();
		InitMqtt();
	}

	IEnumerator InitWhenReady(){
		while(!(_flags[0] && _flags[1]))
			yield return null;
		Init();
	}

	void LoadIni(){
		INIParser ini = new INIParser();
		ini.Open(_iniPath);
		
		//basic
		_version = ini.ReadValue("Base","Version","null");
		_build = ini.ReadValue("Base","Build","null");
		_keyRev = ini.ReadValue("Base","KeyRev","null");
		_brokerAddress=ini.ReadValue("Base","Server","undefined");
		_brokerPort = ini.ReadValue("Base","Port",8000);
		_prefix = ini.ReadValue("Base","Prefix","");
		_clientId = ini.ReadValue("Base","ClientId","id");
		_clientId=_prefix+"_"+_clientId;
		int qos = ini.ReadValue("Base","Qos",2);
		_reconnectDelay = ini.ReadValue("Base","ReconnectDelay",5000);
		_qos=(byte)qos;
		_rpmUpdatePeriod = (float)ini.ReadValue("Base","RPMUpdatePeriod",_rpmUpdatePeriod);
		_rpmUpdatePeriod *= 0.001f;
		_sharedFolder = ini.ReadValue("Base","SharedFolder","");
		_sharedFolder = _sharedFolder.Replace('\\','/');
		
		if(!_arBuild){
			//Check shared directory
			if(_sharedFolder==""){
				Debug.Log("setting up default shared folder");
				Debug.Log("prefix = "+_prefix);
				_sharedFolder = _workDirectory+"/calshared/"+_prefix;
				if(!Directory.Exists(_sharedFolder))
					Directory.CreateDirectory(_sharedFolder);
			}
			else{
				Debug.Log("using custom shared folder: "+_sharedFolder);
				_sharedFolder+="/"+_prefix;
				if(!Directory.Exists(_sharedFolder))
					Directory.CreateDirectory(_sharedFolder);
			}
			//copy room configs to shared
			ShareFile("Catalog_ICU.ini");
			ShareFile("Catalog_OR.ini");
			ShareFile("Ecmo.json");


			//interactive elements - this section needs more definition
			_config.lookSensitivity = (float)ini.ReadValue("Interactive", "LookSensitivity", 0.5F);
			_config.moveSensitivity = (float)ini.ReadValue("Interactive", "MoveSensitivity", 0.5F);
			_config.joystickSensitivity=(float)ini.ReadValue("Interactive", "JoySensitivity", 0.5F);
		}
		ini.Close();
	}

	void LoadPubsAndSubs(){
		//load pubs and subs
		System.Type thisType = this.GetType();
		Dictionary<int,string> subs = IniHelper.GetStrings(_iniPath,"sub");
		Debug.Log("Loading pubs and subs from "+_topicTablePath);
		Dictionary<string,string> subActions = IniHelper.GetStringDict(_topicTablePath,"sub");

		//get subs by int - used for batch scalar processing
		_subs.Clear();
		foreach(int k in subs.Keys){
			_subs.Add(k,_prefix+subs[k]);
		}
		
		//load sub handlers
		_subActions.Clear();
		foreach(string sa in subActions.Keys){
			_subActions.Add(_prefix+sa, thisType.GetMethod(subActions[sa]));
		}

		//load pub aliases
		_pubs.Clear();
		Dictionary<string,string> pubs = IniHelper.GetStringDict(_topicTablePath,"pub");
		foreach(string t in pubs.Keys){
			if(pubs[t]!="Undefined"){
				_pubs.Add(pubs[t],_prefix+t);
			}
		}
	}

	void InitMqtt(){
		//fire up the mqtt client
		//Note that these events are received in a separate thread
		_client = new MqttClient(_brokerAddress,_brokerPort,false,null,null,MqttSslProtocols.None,null);
		try{
			//receiver handler
			_client.MqttMsgPublishReceived += MsgPublishReceived;
			//disconnect handler
			_client.ConnectionClosed += Disconnected;
			//try connecting - this will fail if broker is offline
			_client.Connect(_clientId);
			//subscriptions
			SendSubs();
			//send status
			SendPub("Status",1);
		}
		catch(System.Exception e){
			//report the error
			Debug.Log(e.Message);
			//start the reconnection routine
			Disconnected(null,null);
		}
	}

	//Need to refresh references on scene load, since this is a singleton
	void OnSceneLoaded(Scene scene, LoadSceneMode mode){
		CheckStatusLed();
		if(scene.buildIndex>1){
			GetSceneReferences();
		}
	}

	void GetSceneReferences(){
		_traces = FindObjectOfType<TraceManager>();
		_cannulas = FindObjectOfType<CannulaMenu>();
		_circuitMan = FindObjectOfType<CircuitManager>();
		_circuitMenu = FindObjectOfType<CircuitMenu>();
		_cardiohelp = FindObjectOfType<Cardiohelp>();
		_ecmo = FindObjectOfType<EcmoCart>();
		_drager = FindObjectOfType<DragerStats>();
		_airMixer = FindObjectOfType<AirMixer>();
		_nav = FindObjectOfType<NavPanel>();
		_room = FindObjectOfType<RoomConfig>();
		_quantum = FindObjectOfType<Quantum>();
		_equip = FindObjectOfType<EquipmentManager>();
		_cameraController = FindObjectOfType<TestCam>();
		_media = FindObjectsOfType<MediaMonitor>();
		_roles = transform.GetComponentInChildren<RoleManager>();
		_editor = FindObjectOfType<EditorMenu>();
		_terumo = FindObjectOfType<Terumo>();
		_ibga = FindObjectOfType<Ibga>();
		_urine = FindObjectOfType<Urometer>();
		_hemo = FindObjectOfType<Hemofilter>();
		_clip = FindObjectOfType<ClipboardHelper>();
		_ivMenu = FindObjectOfType<IVmenu>();
		_invos = FindObjectOfType<Invos>();
		_scenario = FindObjectOfType<ScenarioManager>();
		_clocks = FindObjectsOfType<Clock>();
		_bed = FindObjectOfType<BedManager>();
		_scene = FindObjectOfType<SceneSelector>();
		_alaris = FindObjectOfType<Alaris>();
		_voiceCom = FindObjectOfType<VoiceComs>();
	}

	//Todo fix this after fixing the Disconnection stall bug
	void CheckStatusLed(){
		//set up status indicator
		GameObject monitor = GameObject.Find("wallMonitor");
		if(monitor==null)
			return;
		_monitorMesh = monitor.GetComponent<MeshRenderer>();
		_monitorMesh.sharedMaterials[_ledIndex].SetColor("_EmissionColor",Color.green);
	}

	void OnDestroy(){
		if(_client==null)
			return;
		//receiver handler
		_client.MqttMsgPublishReceived -= MsgPublishReceived;

		//disconnect handler
		_client.ConnectionClosed -= Disconnected;
		_killTask=true;
	}

	//Subscribe to topics via broker
	void SendSubs(){
		
		int numSubs = _subs.Count;
		string [] subs = new string[numSubs];
		byte[] qos = new byte[numSubs];
		int index=0;
		foreach(string v in _subs.Values){
			subs[index]=v;
			qos[index]=_qos;
			index++;
		}

		_client.Subscribe(subs, qos);
	}

	//note this is kind of broken and causes main thread to stall
	async void Disconnected(object sender, System.EventArgs e){
		if(!_reconnecting){
			Debug.Log("Disconnected");
			await TryReconnect();
		}
	}

	async Task TryReconnect(){
		_reconnecting=true;
		bool connected = _client.IsConnected;
		while(!connected&&!_killTask){
			try{
				_client.Connect(_clientId);
			}
			catch{
				Debug.Log("No connection... reconnecting");
			}
			await Task.Delay(_reconnectDelay);
			//await Task.Wait(_reconnectDelay);
			//Thread.Sleep(_reconnectDelay);
			connected=_client.IsConnected;
		}	
		SendSubs();
		_reconnecting=false;
	}

	//main handler for receiving messages
	void MsgPublishReceived(object sender, MqttMsgPublishEventArgs e){
		if(_subActions.ContainsKey(e.Topic))
		{
			//wait for commands list to free from Update()
			while(_commandLock);
			StringByteArr sba;
			sba._str=e.Topic;
			sba._bytes=e.Message;
			_commands.Add(sba);
#if UNITY_EDITOR
			LoggerManager.SaveTopic(sender, StripPrefix(e.Topic), e.Message, MqttLogger.LogType.income);
#endif
		}
		else
		{
			Debug.Log("unknown sub received: "+e.Topic);
		}
	}

	//Publish topic to broker
	void Publish(string topic, byte[] data){
		_client.Publish(topic,data,_qos,false);
	}

	// Update is called once per frame
	void Update()
	{
		//Execute commands
		if(_commands.Count>0){
			_commandLock=true;
			object [] mParams = new object[2];
			//processing these commands one per frame because in some cases
			//race conditions may occur - like configuring all elements of the ecmo cart
			//in a single frame
			StringByteArr sba = _commands[0];
			mParams[0]=sba._str;
			mParams[1]=sba._bytes;
			//#todo better error handling
			//maybe improve this - hard to pin point error type and location
			try{
				_subActions[sba._str].Invoke(this,mParams);
			}
			catch(System.NullReferenceException e){
				Debug.Log(e.Message);
				//apparently anything we put in here doesn't get logged so that isn't good
			}
			_commands.RemoveAt(0);

			//scalar commands are processed in bulk because they are generally simple numerical topics
			foreach(StringByteArr sbaScalar in _scalarCommands)
			{
				mParams[0]=sbaScalar._str;
				mParams[1]=sbaScalar._bytes;
				try{
					_subActions[sbaScalar._str].Invoke(this,mParams);
				}
				catch(System.NullReferenceException e){
					Debug.Log(e.Message);
					//apparently anything we put in here doesn't get logged so that isn't good
				}
			}
			//_commands.Clear();
			_scalarCommands.Clear();
			_commandLock=false;
		}

		//#todo something with this maybe - some kind of status indicator
		//state machine
		/*
		switch(_cState){
			case CommStates.HANDSHAKE:
				break;
			case CommStates.STANDBY:
				UpdateStatusIndicator();
				break;
			case CommStates.RUN:
				UpdateStatusIndicator();
				break;
			case CommStates.EXIT:
				break;
			default:
				break;
		}
		*/
	}

	void UpdateStatusIndicator(){
		//update status indicator
		if(_monitorMesh!=null){
			if(_client.IsConnected&&!_greenLight){
				_monitorMesh.sharedMaterials[_ledIndex].SetColor("_EmissionColor",Color.green);
				_greenLight=true;
			}
			else if(!_client.IsConnected&&_greenLight){
				_monitorMesh.sharedMaterials[_ledIndex].SetColor("_EmissionColor",Color.red);
				_greenLight=false;
			}
		}
	}
	
	//sub handlers - seek to splt for end of subs and start of pubs
	
	public void HandshakeReceived(string topic, byte[] data){
		Debug.Log("Received handshake");
		float f = System.BitConverter.ToSingle(data,0);
		int status=Mathf.RoundToInt(f);
		if(status==1)
		{
			if(_cState == CommStates.HANDSHAKE){
				SendVersion(null,null);
				SendBuild(null,null);
				SendKeyRev(null,null);
				_cState = CommStates.STANDBY;
			}
			SendPub("Status",1);
		}
		else if(status==0)
			Debug.Log("Master Edge Offline");
		else
			Debug.Log("Master Edge Status cannot be determined");
	}

	//status request from masterEdge
	public void SendStatus(string topic, byte[] data){
		Debug.Log("Trying to send a status 1");
		SendPub("Status",1);
	}

	public void ExitApp(string topic, byte[] data){
		string reason = Encoding.UTF8.GetString(data);
		StartCoroutine(ExitRoutine(reason));
	}

	IEnumerator ExitRoutine(string r){
		bool immediate = r=="";
		Text cd = null;
		if(!immediate){
			Transform closeCanvas = transform.Find("ForceClose");
			closeCanvas.GetComponent<Canvas>().enabled=true;
			CanvasGroup panel =closeCanvas.GetComponent<CanvasGroup>();
			panel.alpha=1;
			cd = panel.transform.Find("cdbg").GetChild(0).GetComponent<Text>();
			Text reason = panel.transform.Find("Reason").GetComponent<Text>();
			reason.text=r;
		}

		float timer=immediate ? 0 : 5f;
		while(timer>0){
			timer-=Time.deltaTime;
			cd.text="Closing in "+Mathf.CeilToInt(timer).ToString();	
			yield return null;
		}
		yield return null;
		DisplayManager._forceClose=true;
#if UNITY_EDITOR
		UnityEditor.EditorApplication.isPlaying=false;
#else
		Application.Quit();
#endif
	}

	void OnApplicationQuit(){
		if(!DisplayManager._forceClose)
			return;
		if(_client!=null && _client.IsConnected){
			SendPub("Status",0);
		}
		//backup shared files
		string [] inis = Directory.GetFiles(_sharedFolder,"*.ini");
		Debug.Log("Tryin to delete backup shared files");
		foreach(string i in inis)
			BackupSharedFile(Path.GetFileName(i));
		//Destroy shared folder
		Debug.Log("Trying to delete shared folder");
		Directory.Delete(_sharedFolder,true);
	}

	public void UpdateDisplay(string topic, byte[] data){
		float f = System.BitConverter.ToSingle(data,0);
		int dispCode = Mathf.RoundToInt(f);
		//pass the display code to display manager
		int returnCode = _display.TrySetDisplay(dispCode);
		SendPub("Display", returnCode);
	}

	public void GetDisplay(string topic, byte[] data){
		int curCode = _display._monitorCache;
		SendPub("Display", curCode);
	}

	public void SetMinUpdateDelay(string topic, byte[] data){
		float f = System.BitConverter.ToSingle(data,0);
		_minUpdateDelay=f;
	}

	public void SetUpdateRate(string topic, byte[] data){
		float ur = System.BitConverter.ToSingle(data,0);
		_updateRate=ur;
	}

	public void SetEkg(string topic, byte[] data){
		string text = Encoding.UTF8.GetString(data);
		TraceManager.TraceData td = JsonUtility.FromJson<TraceManager.TraceData>(text);
		_traces.UpdateTraceData(0,td.value,td.display_rate,0,0,td.color,td.trace_color);
	}
	public void SetPulseOx(string topic, byte[] data){
		string text = Encoding.UTF8.GetString(data);
		TraceManager.TraceData td = JsonUtility.FromJson<TraceManager.TraceData>(text);
		_traces.UpdateTraceData(1,td.value,td.display_rate,0,0,td.color,td.trace_color);
	}
	public void SetAbp(string topic, byte[] data){
		string text = Encoding.UTF8.GetString(data);
		TraceManager.TraceData td = JsonUtility.FromJson<TraceManager.TraceData>(text);
		_traces.UpdateTraceData(2,td.mean,td.display_rate,td.diastole,td.systole,td.color,td.trace_color);
	}
	public void SetPap(string topic, byte[] data){
		string text = Encoding.UTF8.GetString(data);
		TraceManager.TraceData td = JsonUtility.FromJson<TraceManager.TraceData>(text);
		_traces.UpdateTraceData(3,td.mean,td.display_rate,td.diastole,td.systole,td.color,td.trace_color);
	}
	public void SetCvp(string topic, byte[] data){
		string text = Encoding.UTF8.GetString(data);
		TraceManager.TraceData td = JsonUtility.FromJson<TraceManager.TraceData>(text);
		_traces.UpdateTraceData(4,td.value,td.display_rate,0,0,td.color,td.trace_color);
	}
	public void SetEtco2(string topic, byte[] data){
		string text = Encoding.UTF8.GetString(data);
		TraceManager.TraceData td = JsonUtility.FromJson<TraceManager.TraceData>(text);
		_traces.UpdateTraceData(5,td.value,td.display_rate,0,0,td.color,td.trace_color);
	}

	//strip prefix from topic
	public void UpdateTrace(string topic,byte[] data){
		if(_traces==null){
			_traces = FindObjectOfType<TraceManager>();
			if(_traces==null)
				return;
		}
		string top=StripPrefix(topic);
		_traces.ConvertCycleData(top,data);
	}

	//...
	public void UpdateTraceColor(string topic, byte[] data){
		if(_traces==null){
			_traces = FindObjectOfType<TraceManager>();
			if(_traces==null)
				return;
		}
		string msg = Encoding.UTF8.GetString(data);
		string top=StripPrefix(topic);
		_traces.UpdateColor(top.Split(new string[] {"trace"},System.StringSplitOptions.None)[0],msg);
	}

	public void SetPumpVal(string topic, byte[] data){
		if(_ecmo==null)
			return;
		float f = System.BitConverter.ToSingle(data,0);
		string top=StripPrefix(topic);

		_ecmo.SetPumpVal(top,f);
	}

	public void UpdateMaxRpm(string topic, byte[] data){
		//#todo define this method
	}

	public void UpdatePatientBody(string topic, byte[] data){
		_patient = FindObjectOfType<Patient>();
		if(_patient==null)
			return;
		float val = System.BitConverter.ToSingle(data,0);
		string top=StripPrefix(topic);
		_patient.UpdateVal(top,val);
	}


	public void SendBuild(string topic, byte[] data){
		SendPub("Build",_build);
	}

	public void SendVersion(string topic, byte[] data){
		SendPub("Version",_version);
	}

	public void SendKeyRev(string topic, byte[] data){
		SendPub("Keyrev",_keyRev);
	}

	public void Undefined(string topic, byte[] data){
		Debug.Log("Sub handler "+topic+" not set up");
	}

	public void GotoRoom(string topic, byte[] data){
		Debug.Log("goto room");
		float f = System.BitConverter.ToSingle(data,0);
		int room = Mathf.RoundToInt(f);
		Debug.Log("going to room: "+room);
		//offset for custom splash screen
		room++;
		if(Application.CanStreamedLevelBeLoaded(room)&& _sceneCode!=room)
		{
			if(room==1)
			{
				_sceneCode=1;
				_scene.GoToScene(1);
			}
			else{
				SendPub("Room",room-1);
				_sceneCode=room;
				_prevSceneCode=1;
				if(_sceneCode>1)
					StartCoroutine(GotoRoomR(room,true));
				else
					SceneManager.LoadScene(_sceneCode);
			}
		}
		else
			Debug.Log("scene "+room+" is NOT valid");
	}

	//Called via room! when user clicks continue on sim side
	//room is build index of room scene
	//direct is directly from splash screen
	IEnumerator GotoRoomR(int room,bool direct=false){
		//load opening scene if in splash screen
		if(SceneManager.GetActiveScene().buildIndex==0)
			SceneManager.LoadScene(1);
		yield return null;
		AnimationPath[] animPaths = FindObjectsOfType<AnimationPath>();
		foreach (AnimationPath ap in animPaths)
		{
			if (ap._pathCode == room)
			{
				//play walk animation
				ap.PlayAnim(this);
			}
		}
		//SendPub("RoomStatus", 2);//2 = loading
		AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(room,LoadSceneMode.Additive);
		while(!asyncLoad.isDone){
			yield return null;
		}
		SendPub("RoomStatus", 1);
		if(_cameraController!=null){
			_cameraController.gameObject.tag="MainCamera";
			if(_cameraController!=null)
				_cameraController.GetComponent<AudioListener>().enabled=false;
		}
	}

	public void UnloadPreviousScene(){
		SceneManager.UnloadSceneAsync(_prevSceneCode);
		//set main camera
		if(_cameraController!=null){
			_cameraController.gameObject.tag="MainCamera";
			_cameraController.GetComponent<AudioListener>().enabled=true;
			_cameraController.GetComponent<BlendEffect>().Unblink();
		}
		StartCoroutine(FadeInEditorR());
	}

	IEnumerator FadeInEditorR(){
		Debug.Log("Activating ui");
		float timer = 0;
		CanvasGroup [] cans = _editor.transform.parent.GetComponentsInChildren<CanvasGroup>();
		List<CanvasGroup> tabs = new List<CanvasGroup>();
		foreach(CanvasGroup c in cans){
			if(c.ignoreParentGroups)
			{
				tabs.Add(c);
				Debug.Log("Got a tab: "+c.name);
			}
		}
		while(timer<1f){
			timer+=Time.deltaTime;
			_editor.transform.parent.GetComponent<CanvasGroup>().alpha=timer;
			foreach(CanvasGroup t in tabs){
				t.alpha=timer;
			}
			yield return null;
		}
		_editor.transform.parent.GetComponent<CanvasGroup>().alpha=1;
		foreach(CanvasGroup t in tabs){
			t.alpha=1;
		}
	}

	public void GetRoom(string topic, byte[] data){
		//offset for splash screen
		string curRoom = (SceneManager.GetActiveScene().buildIndex-1).ToString("0");
		SendPub("Room",curRoom);
	}

	[System.Serializable]
	struct MonitorAudio{
		public int audio_level;
		public float frequency;
	}
	public void SetMonitorAudio(string topic, byte[] data){
		//todo test monitor audio
		string text = Encoding.UTF8.GetString(data);
		MonitorAudio ma = JsonUtility.FromJson<MonitorAudio>(text);
		_traces.SetAudio(ma.audio_level/100f,Mathf.RoundToInt(ma.frequency));
	}

	public void PeekRoom(string topic, byte[] data){
		string status = Encoding.UTF8.GetString(data);
		switch(status){
			case "0":
			case "2":
			default:
				break;
			case "1":
				ShowLoadingScreen(false);
				break;
		}
	}

	public void UpdateSimStatus(string topic, byte[] data){
		float f = System.BitConverter.ToSingle(data,0);
		int status = Mathf.RoundToInt(f);
		Debug.Log("Got sim status: "+status);
		switch(status){
			case 1:
				if(_sceneCode>1){
					AnimationPath._simLoaded=true;
					Debug.Log("sim loaded is true");
				}
				break;
			case 2:
			default:
				break;
			case 0:
				_cState = CommStates.STANDBY;
				//1 is our opening scene
				SceneManager.LoadScene(1);
				SendPub("RoomStatus", 1);
				int returnCode = _display.TrySetDisplay(-1);
				SendPub("Display", returnCode);
				break;
		}
	}

	[System.Serializable]
	public struct CannulaData{
		public int cannula;
		public int cannulation_site;
		public float depth;
		public float rotation;
	}

	public void UpdateCannula(string topic, byte[] data){
		string text = Encoding.UTF8.GetString(data);
		CannulaData cd = JsonUtility.FromJson<CannulaData>(text);
		CircuitManager.CannulaN c = new CircuitManager.CannulaN();
		c.index=cd.cannula;
		c._depth=cd.depth*.01f;
		c._rotation=cd.rotation;
		c.length=_circuitMan._ecmoData.Cannulas[c.index].length;
		_cannulas.SetCannula(c, cd.cannulation_site,false,true,true);
	}

	/*
	public void UpdateFio2(string topic, byte[] data){
		float f = System.BitConverter.ToSingle(data,0);
		if(_airMixer!=null)
			_airMixer.SetFio2(f);
	}
	public void UpdateAirRate(string topic, byte[] data){
		float f = System.BitConverter.ToSingle(data,0);
		if(_airMixer!=null)
			_airMixer.SetAirRate(f);
	}
	*/

	public void SetGasBlenderValue(string topic, byte[] data){
		float f = System.BitConverter.ToSingle(data,0);
		string top=StripPrefix(topic);
		_ecmo.SetGasBlenderValue(top,f);
	}

	public void SetFloor(string topic, byte[] data){
		float f = System.BitConverter.ToSingle(data,0);
		int floor=Mathf.RoundToInt(f);
		MaterialManager matMan = FindObjectOfType<MaterialManager>();
		if(matMan==null)
		{
			Debug.Log("No material manager found in current scene: "+SceneManager.GetActiveScene().buildIndex);
			return;
		}
		//temp method to just set floor if exists
		matMan.SetFloor(floor);
	}

	public void GotoView(string topic, byte[] data){
		float f = System.BitConverter.ToSingle(data,0);
		int id = Mathf.RoundToInt(f);
		if(id>=0 && _nav!=null){
			_nav.NavigateByIndex(id);
		}
	}

	[System.Serializable]
	public struct AlarmData{
		public int parameter;
		public int type;
		public string message;
		public int volume;
	}

	public void SetPumpAlarm(string topic, byte[] data){
		string text = Encoding.UTF8.GetString(data);
		AlarmData ad = JsonUtility.FromJson<AlarmData>(text);
		float vol = (float)(ad.volume)*.01f;
		int type = ad.type;
		int index = ad.parameter;
		_ecmo.SetPumpAlarm(index,type,ad.message,vol);
		//_cardiohelp.SetAlarm(index,type,ad.message,vol);
	}

	public void ToggleGasDigi(string topic, byte[] data){
		float f = System.BitConverter.ToSingle(data,0);
		int type = Mathf.RoundToInt(f);
		Debug.Log("Toggling gas: "+type);
		//#todo rework this to go through ecmo cart
		if(_airMixer==null)
			_airMixer = FindObjectOfType<AirMixer>();
		_airMixer.ToggleDigi(type);
	}

	public void ScalarGroup(string topic, byte [] data){
		for(int i=0; i<=data.Length-8;i+=8){
			int id=0;
			float val=0;
			id = Mathf.RoundToInt(System.BitConverter.ToSingle(data,i));
			val = System.BitConverter.ToSingle(data,i+4);
			Debug.Log("Received ID: "+id+" - With value: "+val);
			//add command to command buffer
			//Add to secondary commands buffer scalarCommands so as to not modify
			//the main loop looping through commands
			if(_subs.ContainsKey(id)){
				StringByteArr sba;
				//assign topic
				sba._str=_subs[id];
				//assign byte[] payload
				sba._bytes=System.BitConverter.GetBytes(val);
				//add to secondary command buffer
				_scalarCommands.Add(sba);
			}

		}
	}

	public void SetVentAlarm(string topic, byte[] data){
		string text = Encoding.UTF8.GetString(data);
		AlarmData ad = JsonUtility.FromJson<AlarmData>(text);
		int param = ad.parameter;
		int type = ad.type;
		string msg = ad.message;
		float vol = (float)(ad.volume)*.01f;
		_drager.SetAlarm(param,type,msg,vol);
	}

	public void SetVentVisible(string topic, byte[] data){
		float f = System.BitConverter.ToSingle(data,0);
		int vis = Mathf.RoundToInt(f);
		_drager.SetVis(vis==1);
	}

	public void SetVentValue(string topic, byte[] data){
		float f = System.BitConverter.ToSingle(data,0);
		//testing out the prefix split
		string top=topic;
		if(_prefix!="")
			top = top.Split(new string[] {_prefix},System.StringSplitOptions.None)[1];
		_drager.SetValue(top,f);
	}

	public void SetVentMode(string topic, byte[] data){
		float f = System.BitConverter.ToSingle(data,0);
		int mode = Mathf.RoundToInt(f);
		_drager.SetMode(mode);
	}

	public void SetQuantumData(string topic, byte[] data){
		float f = System.BitConverter.ToSingle(data,0);
		string top=topic;
		if(_prefix!="")
			top = top.Split(new string[] {_prefix},System.StringSplitOptions.None)[1];
		_quantum.SetData(top,f);
	}

	public void SetHLM(string topic, byte[] data){
		float f = System.BitConverter.ToSingle(data,0);
		int type = Mathf.RoundToInt(f);
		_equip.SetHLM(type);
	}

	public void SetResVolume(string topic, byte[] data){
		float f = System.BitConverter.ToSingle(data,0);
		_quantum.SetResVolume(f);
	}

	public void SetHLMColor(string topic, byte[] data){
		string color = Encoding.UTF8.GetString(data);
		string top=topic;
		if(_prefix!="")
			top = top.Split(new string[] {_prefix},System.StringSplitOptions.None)[1];
		_quantum.SetColor(top,color);
	}

	[System.Serializable]
	public struct MediaData{
		public string filepath;
		public int display;
		public int playmode;
		public int volume;
		public int file_size;
	}

	public void LoadMedia(string topic, byte[] data){
		string raw = Encoding.UTF8.GetString(data);
		MediaData md = JsonUtility.FromJson<MediaData>(raw);
		string fullPath = _sharedFolder+"/"+md.filepath.Replace('\\','/');
		//check for url path
		bool isWeb=false;
		if(md.filepath.Length>=4){
			string firstFour = md.filepath.Substring(0,4);
			if(firstFour.Equals("http", System.StringComparison.OrdinalIgnoreCase)){
				fullPath=md.filepath;
				isWeb=true;
			}
		}
		string [] parts = md.filepath.Split('.');
		string extension = parts[parts.Length-1];
		if(fullPath.Contains("youtube")){
			foreach(MediaMonitor m in _media)
				m.LoadVideo(fullPath,md.playmode,(float)md.volume*.01f,isWeb,md.display,true);
		}
		else if(extension.Equals("png", System.StringComparison.OrdinalIgnoreCase) || extension.Equals("jpg", System.StringComparison.OrdinalIgnoreCase)){
			foreach(MediaMonitor m in _media)
				m.LoadImage(fullPath,md.display,md.playmode);
		}
		else if( extension.Equals("mp4", System.StringComparison.OrdinalIgnoreCase)||extension.Equals("wmv", System.StringComparison.OrdinalIgnoreCase)||extension.Equals("mov", System.StringComparison.OrdinalIgnoreCase)){
			foreach(MediaMonitor m in _media)
				m.LoadVideo(fullPath,md.playmode,(float)md.volume*.01f,isWeb,md.display);
		}
		else{
			foreach(MediaMonitor m in _media)
				m.FileNotFound(md.display);
		}
	}

	[System.Serializable]
	public struct RoleData{
		public int role;
		public string role_name;
		public int avatar_placement;
		public string tts;
		public int tts_mode;
		public int volume;
	}

	public void RoleAction(string topic, byte[] data){
		string raw = Encoding.UTF8.GetString(data);
		Debug.Log("Received role action: "+raw);
		RoleData ad = JsonUtility.FromJson<RoleData>(raw);
		if(_roles!=null)
			_roles.RoleAction(ad.role,ad.role_name,ad.avatar_placement,ad.tts,ad.tts_mode,ad.volume);
		else
			Debug.Log("Role manager null");
	}

	public void SetCardiohelpPanel(string topic, byte[] data){
		float f = System.BitConverter.ToSingle(data,0);
		//plus 1 because we know these indices start at 0 in the ini, but 1 in Cardiohelp
		//0 is reserved for test tube panel
		//#todo rework this stuff to go through ecmo cart
		if(_cardiohelp==null)
			_cardiohelp = FindObjectOfType<Cardiohelp>();
		if(_cardiohelp!=null)
			_cardiohelp.GotoScreen(Mathf.RoundToInt(f)+1);
	}

	public void SetEcmoCart(string topic, byte[] data){
		float f = System.BitConverter.ToSingle(data,0);
		int index = Mathf.RoundToInt(f);
		_ecmo.SetCartModel(index);
	}

	public void SetHeaterModel(string topic, byte[] data){
		float f = System.BitConverter.ToSingle(data,0);
		int index = Mathf.RoundToInt(f);
		_ecmo.SetHeaterCooler(index);
	}

	public void SetHeaterVis(string topic, byte[] data){
		float f = System.BitConverter.ToSingle(data,0);
		int index = Mathf.RoundToInt(f);
		_ecmo.SetHeaterCoolerVis(index==1);
	}
	
	public void SetEcmoPump(string topic, byte[] data){
		float f = System.BitConverter.ToSingle(data,0);
		int index = Mathf.RoundToInt(f);
		_ecmo.SetPumpModel(index);
	}

	public void SetEcmoPumpVis(string topic, byte[] data){
		float f = System.BitConverter.ToSingle(data,0);
		int index = Mathf.RoundToInt(f);
		_ecmo.SetPumpVisible(index==1);
	}

	public void SetOxygenator(string topic, byte[] data){
		float f = System.BitConverter.ToSingle(data,0);
		int index = Mathf.RoundToInt(f);
		_ecmo.SetOxygenatorModel(index);
	}

	public void SetOxygenatorVis(string topic, byte[] data){
		float f = System.BitConverter.ToSingle(data,0);
		int index = Mathf.RoundToInt(f);
		_ecmo.SetOxygenatorVis(index==1);
	}

	public void SetWallClockMode(string topic, byte[] data){
		float f = System.BitConverter.ToSingle(data,0);
		int index = Mathf.RoundToInt(f);
		foreach(Clock c in _clocks)
		{
			if(c!=null)
				c.SetSeconds(index==1);
		}
	}

	public void SetMode(string topic, byte[] data){
		float f = System.BitConverter.ToSingle(data,0);
		int index = Mathf.RoundToInt(f);
		_mode=index;
		_editor.SetMode(_mode==1);
		SendPub("Mode",_mode);
	}

	public void SetIbgaModel(string topic, byte[] data){
		float f = System.BitConverter.ToSingle(data,0);
		int index = Mathf.RoundToInt(f);
		_ecmo.SetIbga(index);
	}

	public void SetTerumaTempMode(string topic, byte[] data){
		float f = System.BitConverter.ToSingle(data,0);
		int index = Mathf.RoundToInt(f);
		//#todo I think this should go through ecmo cart otherwise temp mode
		//won't persist through a model change or cart change
		if(_terumo==null)
			_terumo = FindObjectOfType<Terumo>();
		_terumo.SetTempMode(index==1);
	}

	public void SetIbgaModules(string topic, byte[] data){
		float f = System.BitConverter.ToSingle(data,0);
		int index = Mathf.RoundToInt(f);
		if(_terumo==null)
			_terumo = FindObjectOfType<Terumo>();
		//#todo refactor ibga, so both of these go through ecmo cart
		_terumo.SetModules(index);
		_ecmo.SetIbgaModules(index);
	}

	public void SetIbgaFloat(string topic, byte[] data){
		float f = System.BitConverter.ToSingle(data,0);
		string top=StripPrefix(topic);
		_ecmo.SetIbgaFloat(top,f);
	}

	public void SetHcVal(string topic, byte[] data){
		float f = System.BitConverter.ToSingle(data,0);
		string top=StripPrefix(topic);
		_ecmo.SetHeaterValue(top,f);
	}

	[System.Serializable]
	public struct UrineData{
		public float volume;
		public string color;
	}
	public void SetUrineContent(string topic, byte[] data){
		string raw = Encoding.UTF8.GetString(data);
		UrineData ud = JsonUtility.FromJson<UrineData>(raw);
		//send to urine manager
		Debug.Log("Setting urine data");
		if(_urine!=null)
			_urine.SetData(ud.volume,ud.color);
	}

	public void DrainUrine(string topic, byte[] data){
		if(_urine!=null)
			_urine.DrainUrine();
	}

	[System.Serializable]
	public struct FormData
	{
		public string filepath;
		public int tab;
		public string tab_label;
		public string tab_color;
		public int status;
		public int tab_change;
	}
	
	/// <summary>
	/// Sets the patient form using JObject library.
	/// </summary>
	/// <see cref="http://www.newtonsoft.com/json/help/html/T_Newtonsoft_Json_Linq_JObject.htm"/>
	/// <param name="jsonObject">The json object.</param>
	public void SetPatientForm(string topic, byte[] data)
    {
		string text = Encoding.UTF8.GetString(data);
		Debug.Log("Received form: "+text);
		FormData form = JsonUtility.FromJson<FormData>(text);
		if(_clip==null)
			return;
		_clip.UpdateForm(form.filepath,form.tab,form.tab_label,
				form.tab_color,form.status,form.tab_change==1,this);
    }

	[System.Serializable]
	public struct CatData{
		public long file_timestamp;
		public int cat_file_flag;
		public string section;
	}

	//Reload config actually sends over a time stamp
	//This way we know when we have received the updated form
	//We are only doing this for config because in most cases we can rely on a name change
	//Note also that forms can have this kind of issue, but those are handled in the ClipboardHelper
	public void ReloadCat(string topic, byte[] data){
		string text = Encoding.UTF8.GetString(data);
		Debug.Log("Catalog payload: "+text);
		CatData cd = JsonUtility.FromJson<CatData>(text);
		//todo send the CatData to RoomConfig and distribute from there based on section
		_ivMenu.ReloadFluids(cd.file_timestamp);
	}

	public void SetInvosVal(string topic, byte[] data){
		float f = System.BitConverter.ToSingle(data,0);
		string top=StripPrefix(topic);
		_invos.SetValue(top,f);
	}

	public void ConfigureCircuit(string topic, byte[] data){
		string text = Encoding.UTF8.GetString(data);
		Debug.Log("Circuit config: "+text);
		Tube.CircuitData cd = JsonUtility.FromJson<Tube.CircuitData>(text);
		_circuitMan.ConfigureTube(cd);
	}

	public void SetCircuit(string topic, byte[] data){
		float f = System.BitConverter.ToSingle(data,0);
		int t = Mathf.RoundToInt(f);
		_circuitMan.SetCurrentCircuit(t);
		_circuitMenu.SetSelectedCircuit(t);
		_circuitMenu.SetCircuitLabel(t);
	}

	public void PowerInvos(string topic, byte[] data){
		float f = System.BitConverter.ToSingle(data,0);
		int t = Mathf.RoundToInt(f);
		_invos.PowerAndHome(t==1);
	}

	public void MasterSetInvosSensors(string topic, byte[] data){
		float f = System.BitConverter.ToSingle(data,0);
		int t = Mathf.RoundToInt(f);
		Debug.Log("Todo: support multiple sensor types: "+t);
		_invos.ConnectAmplifier(0,0,false);
		_invos.ConnectAmplifier(0,1,false);
	}

	public void SetInvosSite(string topic, byte[] data){
		float f = System.BitConverter.ToSingle(data,0);
		int site = Mathf.RoundToInt(f);
		string top = StripPrefix(topic);
		string[] parts = top.Split('/');
		int channel = int.Parse(""+parts[2][2]);
		channel--;
		_invos.CommandPlaceSensor(channel, site);
	}

	public struct ScenarioFile{
		public string filepath;
		public int step;
		public string cat_path;
	}
	public void UpdateScenarioFile(string topic, byte[] data){
		string text = Encoding.UTF8.GetString(data);
		ScenarioFile sf = JsonUtility.FromJson<ScenarioFile>(text);
		Debug.Log($"Got filename: {sf.filepath} and step: {sf.step}");
		_scenario.LoadScenario(sf.filepath.Replace("\\","/"),sf.step,sf.cat_path.Replace("\\","/"));
	}

	public struct ScenarioState{
		public int step;
		public int mode;//0 = pause, 1 = play, 2=reset
	}
	public void PlayScenario(string topic, byte[] data){
		string text = Encoding.UTF8.GetString(data);
		Debug.Log("Received scenario play: "+text);
		ScenarioState sf = JsonUtility.FromJson<ScenarioState>(text);
		if(_scenario==null)
			Debug.Log("Scenario null");
		_scenario._curStep=sf.step-1;
		_scenario.Play(sf.mode,false);
	}

	public void DisconnectGas(string topic, byte[] data){
		//_airMixer.DisconnectOutflow();
		_ecmo.ConnectGas(false);
	}

	public void ConnectGas(string topic, byte[] data){
		//_airMixer.ConnectOutflow();
		_ecmo.ConnectGas(true);
	}

	public void SetSimulationType(string topic, byte[] data){
		float f = System.BitConverter.ToSingle(data, 0);
		int t = Mathf.RoundToInt(f);
		Debug.Log("got sim type w scene code: "+_sceneCode);
		if(_ecmo!=null&&_sceneCode>1)
		{
			Debug.Log("Pumps loaded");
			_hardware=t % 2 == 0 && t != 0;
			_ecmo.SetHardwareMode(_hardware);
		}
		else{
			Debug.Log("Pumps not loaded");
		}
	}

	public void SetPatientId(string topic, byte[] data){
		string text = Encoding.UTF8.GetString(data);
		_patientId = text;
	}


	public void ConfigureClamps(string topic, byte[] data){
		string text = Encoding.UTF8.GetString(data);
		_circuitMan.ConfigureClamps(text);
	}

	public void SetHoffmanVis(string topic, byte[] data){
		float f = System.BitConverter.ToSingle(data, 0);
		int vis = Mathf.RoundToInt(f);
		_circuitMan.EnableHoffman(vis==1);

	}

	public void SetTimeFactor(string topic, byte[] data){
		float f = System.BitConverter.ToSingle(data, 0);
		foreach(Clock c in _clocks){
			if(c!=null)
				c.SetTimeFactor(f);
		}
	}

	//in minutes
	public void AdvanceTime(string topic, byte[] data){
		float f = System.BitConverter.ToSingle(data, 0);
		foreach(Clock c in _clocks){
			if(c!=null)
				c.AdvanceTime(f);
		}
	}

	public void SetBed(string topic, byte[] data){
		float f = System.BitConverter.ToSingle(data, 0);
		int bed = Mathf.RoundToInt(f);
		_bed.SetBed(bed);
	}

	public void SetLanguage(string topic, byte[] data){
		float f = System.BitConverter.ToSingle(data, 0);
		int lang = Mathf.RoundToInt(f);
		_room.ChangeLanguage(lang);
	}

	[System.Serializable]
	public struct HemoVol{
		public float volume;
		public string color;
	}

	public void SetHemoVol(string topic, byte[] data){
		string text = Encoding.UTF8.GetString(data);
		HemoVol hv = JsonUtility.FromJson<HemoVol>(text);
		//#todo rework
		if(_hemo==null)
			_hemo = FindObjectOfType<Hemofilter>();

		_hemo.Update(hv.volume,hv.color);
	}

	[System.Serializable]
	public struct IvModule{
		public int module;
		public int power;
		public string fluid;
		public float rate;
		public float vtbi;
		public int state;
	}
	public void SetIvModule(string topic, byte[] data){
		string text = Encoding.UTF8.GetString(data);
		IvModule im = JsonUtility.FromJson<IvModule>(text);
		if(im.module==0 && im.power==0)
			_alaris.PowerOff();
	}

	public void SetVoiceMode(string topic, byte[] data){
		float f = System.BitConverter.ToSingle(data, 0);
		int v = Mathf.RoundToInt(f);
		_scenario.ttsMode=v;
	}

	public void ConnectHemo(string topic, byte[] data){
		float f = System.BitConverter.ToSingle(data, 0);
		int v = Mathf.RoundToInt(f);
		_hemo.Connect(v);
	}

	public void DisconnectHemo(string topic, byte[] data){
		float f = System.BitConverter.ToSingle(data, 0);
		int v = Mathf.RoundToInt(f);
		_hemo.Disconnect(v);
	}

	public void SetGasMixerVis(string topic, byte[] data){
		float f = System.BitConverter.ToSingle(data,0);
		int index = Mathf.RoundToInt(f);
		_ecmo.SetBlenderVis(index==1);
	}

	public void SetHemofilterVis(string topic, byte[] data){
		float f = System.BitConverter.ToSingle(data,0);
		int index = Mathf.RoundToInt(f);
		_ecmo.SetHemofilterVis(index==1);
	}

	public void SetInfusionPumpVis(string topic, byte[] data){
		float f = System.BitConverter.ToSingle(data,0);
		int index = Mathf.RoundToInt(f);
		Debug.Log("alaris vis "+index);
		_alaris.ToggleVis(index==1);
	}

	public void SetIbgaVis(string topic, byte[] data){
		float f = System.BitConverter.ToSingle(data,0);
		int index = Mathf.RoundToInt(f);
		_ecmo.SetIbgaVis(index==1);
	}

	public void SetNirsVis(string topic, byte[] data){
		float f = System.BitConverter.ToSingle(data,0);
		int index = Mathf.RoundToInt(f);
		Debug.Log("nirs vis "+index);
		_invos.SetVis(index==1);
	}

	public void SetPumpValid(string topic, byte[] data){
		float f = System.BitConverter.ToSingle(data,0);
		int index = Mathf.RoundToInt(f);
		_ecmo.SetPumpValid(index==1);
	}

	public void SetIbgaValid(string topic, byte[] data){
		float f = System.BitConverter.ToSingle(data,0);
		int index = Mathf.RoundToInt(f);
		if(_terumo==null)
			_terumo = FindObjectOfType<Terumo>();
		_terumo.SetValid(index==1);
	}

	public void PlayMic(string topic, byte[] data){
		_voiceCom.PlayClip(data);
	}

//splt
	public void SendKnobVal(Knob knob,Knob secondary,string pubAlias){
		System.DateTime cur = System.DateTime.Now;
		System.TimeSpan interval = cur-_lastUpdate;
		float updateDelay = _minUpdateDelay;
		if(knob._pubAlias=="RPM")
		{
			updateDelay = _rpmUpdatePeriod;
		}
		//somewhere in here we otta be able to bypass this for specific knobs
		if(interval.Seconds>updateDelay)
		{
			float val = knob._val;
			if(secondary!=null)
				val+=secondary._val;
			SendPub(pubAlias,val);
			_lastUpdate=cur;
		}
	}

	public void ForceSendKnobVal(Knob knob,Knob secondary,string pubAlias){
		float val = knob._val;
		if(secondary!=null)
			val+=secondary._val;
		SendPub(pubAlias,val);
	}

	public void ForceCannula(int site, CircuitManager.CannulaN can){
		Debug.Log("Forcing cannula "+site);
		CannulaData cd;
		cd.cannula=can.index;
		cd.cannulation_site=site;
		cd.depth=can._depth*100f;
		cd.rotation=can._rotation;
		string msg = JsonUtility.ToJson(cd);
		SendPub("Cannula",msg);
	}

	public void ForceVentValue(string alias, float f){
		SendPub(alias,f);
	}

	public void SendNav(int navIndex){
		SendPub("Nav",navIndex);
	}

	public void ShowLoadingScreen(bool show){
		_loadCanvas.enabled=show;
		StartCoroutine(EnableInput(!show));
	}

	IEnumerator EnableInput(bool enable){
		yield return null;
		//this stuff really needs to be optimized so we aren't searching for these components every time
		//References to the components should be stored globally and calculated onsceneload
		//We should also do null checks in here I think to be safe
		if(_cameraController!=null && _cameraController.transform.Find("Blocker")!=null){
			_cameraController.transform.Find("Blocker").GetComponent<BoxCollider>().enabled=!enable;
			_cameraController.enabled=enable;
			//_control.isEnabled = enable;
		}
	}

	//this tells the server that there's been a change to a room config
	public void ConfigUpdate(){
		//only send update if running to get around the bug where these things send during setup
		if(_client!=null)
			SendPub("ConfigChange", 0f);
	}
	//
	//this tells the server that there's been a change to a room config
	public void NavChange(long timestamp, bool fromScenario){
		//only send update if running to get around the bug where these things send during setup
		//this uses a different topic now !
		//if(_client!=null)
		//	SendPub("NavChange", 0f);
		CatData cd = new CatData();
		cd.file_timestamp = timestamp;
		cd.cat_file_flag = fromScenario ? 1 : 0;
		cd.section = "View";
		string s = JsonUtility.ToJson(cd);
		Debug.Log("Sending cat change: "+s);
		SendPub("CatChange",s);
	}

	public void PanelUpdate(int i){
		SendPub("CardiohelpPanel",i);
	}

	public void UpdateIbgaTempMode(int i){
		SendPub("IbgaTempMode", i);
	}

	public void SendSetTemp(float f){
		SendPub("SetSetpoint",f);
	}

	public void ForceDrain(){
		SendPub("UrineDrain",0f);
	}

	[System.Serializable]
	struct FluidInput {
		public string fluid;
		public float volume;
		public int delivery_means;
	}
	public void Infuse(string fl, float amt){
		FluidInput fi;
		fi.fluid=fl;
		fi.volume=amt;
		fi.delivery_means=0;
		string fis = JsonUtility.ToJson(fi);
		SendPub("FluidInput",fis);
	}

	public void PlaceSensor(int channel, int site){
		if(channel==0)
			SendPub("Invos1",site);
		else if(channel==1)
			SendPub("Invos2",site);
		else if(channel==2)
			SendPub("Invos3",site);
		else if(channel==3)
			SendPub("Invos4",site);
	}

	public void SetInvosSensors(int sensors){
		SendPub("InvosSensor",sensors);
	}

	public void SetInvosPower(bool on){
		SendPub("InvosPower",on);
	}

	public void ForceCircuitConfig(Tube.CircuitData cd){
		string text = JsonUtility.ToJson(cd);
		SendPub("CircuitConfig",text);
	}

	public void ForceCircuitType(int i){
		SendPub("ForceCircuit",i);
	}

	public void Play(int mode, int step){
		ScenarioState s = new ScenarioState();
		s.mode=mode;
		s.step=step;
		string str = JsonUtility.ToJson(s);
		SendPub("Play",str);
	}
	public void Pause(){
		SendPub("Pause");
	}

	public void ReconnectGasOutflow(float sweep){
		SendPub("ReconnectGas");
		SendPub("AirRate", sweep);
	}

	public void RequestGem3000(int index){
		SendPub("Gem",index);
	}

	public void RequestACT(){
		SendPub("ACT");
	}

	//todo replace this with a generic clamp handler 
	public void ForceClamps(string json){
		SendPub("ClampConfig",json);
	}

	public void SendPrimer(float val){
		SendPub("ECMOPrimer",val);
	}

	public void ReconnectHemo(int port){
		SendPub("ReconnectHemo", port);
	}

	public void SendClip(byte[] data){
		SendPub("Mic", data);
	}

	//end splt

	//string handler
	public void SendPub(string alias, string msg){
		if(_pubs.ContainsKey(alias)){
			Publish(_pubs[alias],Encoding.UTF8.GetBytes(msg));
#if UNITY_EDITOR
			LoggerManager.SaveTopic(_prefix, StripPrefix(_pubs[alias]), msg, MqttLogger.LogType.outgoing);
#endif
			Debug.Log("Sending pub: " + alias + " message: " + msg);
		}
	}
	//numeric (float) handler
	public void SendPub(string alias, float msg){
		if(_pubs.ContainsKey(alias))
		{
			Publish(_pubs[alias],System.BitConverter.GetBytes(msg));
#if UNITY_EDITOR
			LoggerManager.SaveTopic(_prefix, StripPrefix(_pubs[alias]), msg.ToString(), MqttLogger.LogType.outgoing);
#endif
		}
	}
	//numeric (int) handler
	public void SendPub(string alias, int msg){
		//#hack
		if(alias=="Status" && _arBuild)
			alias="ARStatus";
		if(_pubs.ContainsKey(alias))
		{
			float f = (float)msg;
			Publish(_pubs[alias],System.BitConverter.GetBytes(f));
#if UNITY_EDITOR
			LoggerManager.SaveTopic(_prefix, StripPrefix(_pubs[alias]), msg.ToString(), MqttLogger.LogType.outgoing);
#endif
		}
	}
	//raw byte [] handler
	public void SendPub(string alias, byte[] msg){
		if(_pubs.ContainsKey(alias))
		{
			Publish(_pubs[alias],msg);
#if UNITY_EDITOR
			LoggerManager.SaveTopic(_prefix, StripPrefix(_pubs[alias]), "byte[]", MqttLogger.LogType.outgoing);
#endif
		}
	}
	//bool handler
	public void SendPub(string alias, bool b){
		if(_pubs.ContainsKey(alias))
		{
			float f = b ? 1 : 0;
			Publish(_pubs[alias],System.BitConverter.GetBytes(f));
#if UNITY_EDITOR
			LoggerManager.SaveTopic(_prefix, StripPrefix(_pubs[alias]), b.ToString(), MqttLogger.LogType.outgoing);
#endif
		}
	}
	//dont care handler
	public void SendPub(string alias){
		SendPub(alias,0);
	}

	IEnumerator CopyToWorkDirR(string fileName, int flagId){
		string workingPath = _workDirectory+"/"+fileName;
		string tmpPath = _workDirectory+"/tmp_"+fileName;
		string backupPath = Application.streamingAssetsPath+"/"+fileName;
		UnityWebRequest www = UnityWebRequest.Get(backupPath);
		yield return www.SendWebRequest();
		if (www.isNetworkError || www.isHttpError)
		{
			StopAllCoroutines();
			StartCoroutine(ExitRoutine(_workDirectory+" "+fileName+" missing!"));
			Debug.Log(www.error);
		}
		else
		{
			//if don't already have working copy, just overwrite
			if(!File.Exists(workingPath))
			{
				File.WriteAllText(workingPath,www.downloadHandler.text);
				_flags[flagId]=true;
			}
			//otherwise, compare fileIds
			else
			{
				File.WriteAllText(tmpPath,www.downloadHandler.text);
				string newFileId=IniHelper.GetValue(tmpPath,"Base","FileId");
				string oldFileId=IniHelper.GetValue(workingPath,"Base","FileId");
				//if mismatch, overwrite working path
				if(newFileId!=oldFileId)
				{
					File.WriteAllText(workingPath,www.downloadHandler.text);
					_flags[flagId]=true;
				}
				_flags[flagId]=true;
			}
		}
	}

	//this gets work path and also copies from streamingAssets if work path does not exist
	public string GetWorkPath(string fileName){
		//hardcode working path to localappdata
		string workingPath = _workDirectory+"/"+fileName;
		//backup is streaming assets
		//note this should only be used in editor
		//in integration, Cal3D.exe is responsible for doing these copies
		string backupPath = Application.streamingAssetsPath+"/"+fileName;
		if(!File.Exists(workingPath)){
			//if the backup does not exist, close, because there's no data anywhere!
			if(!File.Exists(backupPath)){
				StartCoroutine(ExitRoutine(_workDirectory+" "+fileName+" missing!"));
				return "nope";
			}
			//copy ini from backup
			try{
			File.Copy(backupPath,workingPath);
			}
			catch(System.Exception e){
				Debug.Log("Oops: "+e.Message);
			}
		}
		else{
			//file does exist in working path but we want to check the two dates
			//edgeCalifia3D is NOT copied from backup. That is up to cal3D.exe
			if(File.GetLastWriteTime(workingPath)>=File.GetLastWriteTime(backupPath) || fileName=="EdgeCalifia3D.ini")
			{
				return workingPath;
			}
			else
			{
				File.Copy(backupPath,workingPath,true);
			}
		}
		return workingPath;
	}

	public void ShareFile(string fileName){
		string qualified = _workDirectory+"/"+fileName;
		if(!File.Exists(qualified))
		{
			Debug.Log("Unable to share file - 404 file not found: "+qualified);
			return;
		}
		File.Copy(qualified,_sharedFolder+"/"+fileName,true);
	}

	void BackupSharedFile(string fileName){
		string qualified = _sharedFolder+"/"+fileName;
		if(!File.Exists(qualified)){
			Debug.Log("Oops could not find file in shared: "+qualified);
		}
		File.Copy(qualified,_workDirectory+"/"+fileName,true);
	}

	public string StripPrefix(string topic){
		string [] parts = topic.Split(new string[] {_prefix},System.StringSplitOptions.None);
		if(parts.Length==2){
			if(parts[0]=="")
				return parts[1];
		}
		return topic;
	}

	public void ChangePrefix(string newPrefix){
		INIParser ini = new INIParser();
		ini.Open(_iniPath);
		
		//basic
		ini.WriteValue("Base","Prefix",newPrefix);
		_prefix=newPrefix;
		//_prefix = ini.ReadValue("Base","Prefix","");
		ini.Close();
	}

	public void ChangeBrokerAddress(string newAddress){
		INIParser ini = new INIParser();
		ini.Open(_iniPath);
		
		//basic
		ini.WriteValue("Base","Server",newAddress);
		_brokerAddress=newAddress;
		ini.Close();
	}
}
