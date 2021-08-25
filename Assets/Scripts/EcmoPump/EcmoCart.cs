//EcmoCart.cs
//
//Description: Manages the many dynamic elements of the ecmo cart
//Manages hardware model swaps, visibility swaps
//Relays data from mqtt to the pumps via the PumpData struct
//

//

using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class EcmoCart : MonoBehaviour
{
	//slot transforms
	Transform _cartSlot;
	Transform _controllerSlot;
	Transform _pumpHeadSlot;
	Transform _oxygenatorSlot;
	Transform _flowSensorSlot;
	Transform _flowSensorDisplaySlot;
	Transform _heaterSlot;
	Transform _blenderSlot;
	Transform _handCrankSlot;
	Transform _ibgaSlot;
	Transform _hemofilterSlot;

	//prefabs
	[Header("Prefabs")]
	public Transform [] _carts;
	public Transform [] _pumps;
	public Transform [] _oxygenators;
	public Transform [] _flowSensorDisplays;
	public Transform [] _heaters;
	public Transform [] _gasBlenders;
	public Transform [] _ibgas;
	public Transform [] _hemofilters;

	//obj references
	[HideInInspector]
	public EcmoPump _pump;//this is public for strange reasons - see navpanel
	Transform _pumpHead;
	Transform _cart;
	[HideInInspector]
	public Transform _oxygenator;
	Transform _flowSensorDisplay;
	Transform _flowSensor;
	Transform _heater;//#todo maybe we refactor the heater cooler class and make this a monobehaviour
	//#temp until we refactor the heaterCooler / getinge HU classes
	GetingeHu _getinge;
	[HideInInspector]//public for navPanel
	public Transform _gasBlender;
	//#temp until we refactor gas blender
	AirMixer _air;
	Transform _handCrank;
	Transform _ibga;
	//#temp until we refactor the ibga and the terumo class
	Terumo _terumo;
	Transform _hemofilter;
	Transform _inflow;
	Transform _hsCuvette;

	//indices for equipment - should match catalog ini 
	int _activeCart=0;
	int _activePump=0;
	int _activeOxygenator=0;
	int _activeFlowSensorDisplay=0;
	int _activeHeater;
	int _activeBlender=0;
	int _activeHemofilter=0;
	int _activeIbga=0;

	//tubing
	[Header("Tubing")]
	//new stuff
	CircuitManager _circuit;
	TubeManager _tubeMan;
	Tube _waterOut;//from perspective of heater cooler
	Tube _waterIn;
	Tube _oxygenIn;
	Tube _airIn;
	Tube _mixedAir;
	Tube _hemoIn;
	Tube _hemoOut;
	Tube _hemoDrain;
	Tube _ibgaVenIn;
	Tube _ibgaVenOut;
	Tube _ibgaArtIn;
	Tube _ibgaArtOut;
	Tube _pumpToOxy;
	Tube _mainInflow;
	Tube _mainOutflow;
	Tube _shuntCable;

	//hardware mode
	public static bool _hardwareMode;

	//data
	public class PumpData{
		public float _rpm;
		public float _flowRate;
		public float _pressureVen;
		public float _pressureArt;
		public float _pressureInt;
		public float _tempVen;
		public float _tempArt;
		public float _pressureDelta;
		public float _svo2;
		public float _hb;
		public float _hct;
		public PumpData(){
		}
	}
	public PumpData _pd;

	public class IbgaData{
		public float _postPh;
		public float _postPco2;
		public float _postPo2;
		public float _tempArt;
		public float _tempVen;
		public float _postSo2;
		public float _hco3;
		public float _be;
		public float _kPlus;
		public float _vo2;
		public float _prePh;
		public bool _tempMode;
		public float _prePco2;
		public float _prePo2;
		public float _do2;
		public float _hct;
		public float _preSo2;
		public float _hgb;
		public float _flowRate;
		public IbgaData(){}
	}
	public IbgaData _ibgaData;
	public class HeaterData{
		public float _level;
		public float _setPoint;
		public float _actual;
		public HeaterData(){}
	}
	public HeaterData _heaterData;
	public class AirData{
		public float _rate;
		public float _fio2;
		public AirData(){}
	}
	public AirData _airData;

	[Header("Tube settings (temp)")]
	public float _mixedAirGravCardiohelp;

    // Start is called before the first frame update
    void Start()
    {
		//get circuit manager to help with connections
		_circuit = FindObjectOfType<CircuitManager>();

		//get reference to tube manager
		_tubeMan = FindObjectOfType<TubeManager>();

		//cart setup
		_cartSlot = transform.Find("CartSlot");
		//setup cart
		SetCartModel();
		_pd = new PumpData();
		_ibgaData = new IbgaData();
		_heaterData = new HeaterData();
		_airData = new AirData();
		//_yJoint = GameObject.Find("yJoint");
		//_inflowTube3 = GameObject.Find("InflowTube3");
    }

#if UNITY_EDITOR
	void Update(){
		if(Input.GetKeyDown(KeyCode.F1)){
			Transform t = FindRecursive(transform,"IbgaSlot");
			if(t==null)
				Debug.Log("not found");
			else
				Debug.Log("found: "+t.name);
		}
	}
#endif

	//model methods - used for swapping out hardware***************************************
	//because cart can change root transforms for all equipments, we must go through
	//all equipment and reposition - not neccessarily re-instantiate, but that was
	//the quickest thing to do at the time so maybe this can be optimized
	public void SetCartModel(int index=-1){
		if(index!=-1)
			_activeCart=index;
		if(_activeCart>=_carts.Length)
			return;
		if(_cart!=null){
			//destroy previous cart
			Destroy(_cart.gameObject);
		}
		_cart = Instantiate(_carts[_activeCart],_cartSlot);
		//handcrank
		_handCrankSlot = _cart.Find("HandCrankSlot");
		//pump
		_controllerSlot = _cart.Find("ControllerSlot");
		SetPumpModel();
		//flow sensor
		_flowSensorDisplaySlot = _cart.Find("FlowSensorDisplaySlot");
		_flowSensorSlot = _cart.Find("FlowSensorSlot");
		SetFlowSensorDisplay();
		//heater cooler
		_heaterSlot = _cart.Find("HeaterCoolerSlot");
		SetHeaterCooler();
		//gas blender
		_blenderSlot = _cart.Find("GasBlenderSlot");
		SetGasBlender();
		//ibga
		_ibgaSlot = _cart.Find("IbgaSlot");
		SetIbga();
		//hemofilter
		_hemofilterSlot = _cart.Find("HemofilterSlot");
		SetHemofilter();

		//tubing
		ConnectWaterHoses();
		ConnectGasTubes();
		ConnectHemofilter();
	}

	public void SetPumpModel(int index=-1){
		if(index==-1)
			index=_activePump;
		else
			_activePump=index;
		if(_controllerSlot==null)
		{
			Debug.Log("Oops, no pump slot found on cart");
			return;
		}
		if(_pump!=null){
			//destroy previous pump
			Destroy(_pump.gameObject);
		}
		Transform pump = Instantiate(_pumps[_activePump],_controllerSlot);
		_pump = pump.GetComponent<EcmoPump>();
		
		//pump head
		_pumpHeadSlot = pump.Find("PumpSlot");
		if(_pumpHeadSlot==null)
			_pumpHeadSlot = _cart.Find("PumpSlot");
		if(_pumpHead!=null)
			Destroy(_pumpHead.gameObject);
		_pumpHead = pump.Find("PumpHead");
		//_pumpHead = FindRecursive(pump,"PumpHead");
		//pump head is sometimes null like in the cardiohelp case
		if(_pumpHead!=null){
			_pumpHead.SetParent(_pumpHeadSlot);
			_pumpHead.localPosition = Vector3.zero;
			_pumpHead.localEulerAngles = Vector3.zero;
			//_pumpHead.localScale = Vector3.one;
			//_inflow = _pumpHead.Find("Inflow");
			_inflow = FindRecursive(_pumpHead,"Inflow");
		}
		else
			_inflow = pump.Find("Inflow");

		//oxygenator
		_oxygenatorSlot = _pump.transform.Find("OxygenatorSlot");
		//some pumps have special slots, otherwise it gets mounted to the cart
		if(_oxygenatorSlot==null)
			_oxygenatorSlot = _cart.Find("OxygenatorSlot");
		//move oxygenator if there previously was one
		if(_oxygenator!=null){
			_oxygenator.SetParent(_oxygenatorSlot);
			_oxygenator.localPosition = Vector3.zero;
			_oxygenator.localEulerAngles = Vector3.zero;
			_oxygenator.localScale = Vector3.one;
		}
		else
			SetOxygenatorModel();

		//handcrank
		if(_handCrank!=null)
			Destroy(_handCrank.gameObject);
		_handCrank = pump.Find("HandCrank");
		if(_handCrank!=null){
			_handCrank.SetParent(_handCrankSlot);
			_handCrank.localPosition = Vector3.zero;
			_handCrank.localEulerAngles = Vector3.zero;
			_handCrank.localScale = Vector3.one;
		}

		//can't and don't need to connect on init before other stuff has been instanced
		if(_heater!=null)
			ConnectWaterHoses();
		if(_gasBlender!=null&&_air!=null)
			ConnectMixedAirTube();
		if(_hemofilter!=null)
			ConnectHemofilter();

		ConnectPumpHeadToOxy();
		ConnectCircuitToPatient();

		if(_ibga!=null)
			ConnectIBGA();
		_circuit.ApplyClamps();
	}

	public void SetFlowSensorDisplay(int index=-1){
		if(index==-1)
			index=_activeFlowSensorDisplay;
		else
			_activeFlowSensorDisplay=index;
		if(_flowSensorDisplaySlot==null)
		{
			Debug.Log("Oops, no flow sensor display slot found on cart");
			return;
		}
		//flow sensor display
		if(_flowSensorDisplay!=null){
			Destroy(_flowSensorDisplay.gameObject);
		}
		_flowSensorDisplay = Instantiate(_flowSensorDisplays[_activeFlowSensorDisplay],_flowSensorDisplaySlot);

		//flow sensor
		_flowSensor = _flowSensorDisplay.Find("FlowSensor");
		if(_flowSensor!=null){
			_flowSensor.SetParent(_flowSensorSlot);
			_flowSensor.localPosition = Vector3.zero;
			_flowSensor.localEulerAngles = Vector3.zero;
			_flowSensor.localScale = Vector3.one;
		}
	}

	public void SetOxygenatorModel(int index=-1){
		bool gen=index!=-1;
		if(index==-1)
			index=_activeOxygenator;
		else
			_activeOxygenator=index;
		if(_oxygenatorSlot==null)
		{
			Debug.Log("Oops, no oxygenator slot found on pump");
			return;
		}
		if(_oxygenator!=null){
			//destroy previous pump
			Destroy(_oxygenator.gameObject);
		}
		_oxygenator = Instantiate(_oxygenators[_activeOxygenator],_oxygenatorSlot);
		_oxygenator.localPosition = Vector3.zero;
		_oxygenator.localEulerAngles = Vector3.zero;

		if(gen)
		{
			ConnectWaterHoses();
			ConnectMixedAirTube();
			ConnectHemofilter();
		}
		
		ConnectPumpHeadToOxy();
		ConnectCircuitToPatient();
		_circuit.ApplyClamps();
	}

	public void SetHeaterCooler(int index=-1){
		bool gen=index!=-1;
		if(index==-1)
			index=_activeHeater;
		else
			_activeHeater=index;
		if(_heaterSlot==null)
		{
			Debug.Log("Oops, no heater cooler slot found on cart");
			return;
		}
		if(_heater!=null){
			Destroy(_heater.gameObject);
		}
		_heater = Instantiate(_heaters[_activeHeater],_heaterSlot);
		_getinge=_heater.GetComponentInChildren<GetingeHu>();
		if(gen)
			ConnectWaterHoses();
	}

	public void SetGasBlender(int index=-1){
		bool gen=index!=-1;
		if(index==-1)
			index=_activeBlender;
		else
			_activeBlender=index;
		if(_blenderSlot==null)
		{
			Debug.Log("Oops, no gas blender slot found on cart");
			return;
		}
		if(_gasBlender!=null){
			Destroy(_gasBlender.gameObject);
		}
		_gasBlender = Instantiate(_gasBlenders[_activeBlender],_blenderSlot);
		_air = _gasBlender.GetComponentInChildren<AirMixer>();

		if(gen)
			ConnectGasTubes();
		//tubeMan.Connect(_wallAirPort,_gasBlenderAirPort,TubeMan.TubeTypes.OXYGEN)
	}

	public void SetIbga(int index=-1){
		if(index==-1)
			index=_activeIbga;
		else
			_activeIbga=index;
		if(_ibgaSlot==null)
		{
			Debug.Log("Oops, no ibga slot found on cart");
			return;
		}
		if(_ibga!=null){
			Destroy(_ibga.gameObject);
		}
		_ibga = Instantiate(_ibgas[_activeIbga],_ibgaSlot);
		_terumo = _ibga.GetComponentInChildren<Terumo>();
		_hsCuvette = _ibga.Find("hsCuvette");
		ConnectIBGA();
	}

	public void SetHemofilter(int index=-1){
		if(index==-1)
			index=_activeHemofilter;
		else
			_activeHemofilter=index;
		if(_hemofilterSlot==null)
		{
			Debug.Log("Oops, no hemofilter slot found on cart");
			return;
		}
		if(_hemofilter!=null){
			Destroy(_hemofilter.gameObject);
		}
		_hemofilter = Instantiate(_hemofilters[_activeHemofilter],_hemofilterSlot);

		ConnectHemofilter();
	}

	//end of model methods*******************************************************************


	//visible methods - toggling visibility of hardware**************************************
	public void SetHeaterCoolerVis(bool vis){
		_heater.gameObject.SetActive(vis);
		ConnectWaterHoses();
		CheckCartVis();
		if(vis)
			_getinge.SyncVals(_heaterData);
	}

	public void SetPumpVisible(bool vis){
		if(_pump==null)
			Debug.Log("oops pump is null");
		_pump.gameObject.SetActive(vis);
		if(_handCrank!=null)
			_handCrank.gameObject.SetActive(vis);
		if(_pumpHead!=null)
			_pumpHead.gameObject.SetActive(vis);
		//pump visibility controls oxygenator vis
		Debug.Log("setting oxy vis");
		SetOxygenatorVis(vis);
		//transfer screen values
		if(vis)
			_pump.SyncVals(_pd);
		CheckCartVis();
		ConnectPumpHeadToOxy();
	}

	public void SetBlenderVis(bool vis){
		_gasBlender.gameObject.SetActive(vis);
		//note this will simply remove old tubes if current hardware is inactive
		ConnectGasTubes();
		if(vis)
			_air.SyncVals(_airData);
		CheckCartVis();
	}

	public void SetOxygenatorVis(bool vis){
		_oxygenator.gameObject.SetActive(vis);
		//this will remove tube if hardware is inactive
		ConnectMixedAirTube();
		ConnectWaterHoses();
		ConnectHemofilter();
		ConnectIBGA();
		ConnectPumpHeadToOxy();
		CheckCartVis();
	}

	public void SetHemofilterVis(bool vis){
		_hemofilter.gameObject.SetActive(vis);
		ConnectHemofilter();
		CheckCartVis();
	}

	public void SetIbgaVis(bool vis){
		_ibga.gameObject.SetActive(vis);
		ConnectIBGA();
		CheckCartVis();
		if(vis)
			_terumo.SyncVals(_ibgaData);
	}

	public void CheckCartVis(){
		bool cartVis=_ibga.gameObject.activeSelf ||
				_hemofilter.gameObject.activeSelf ||
				_gasBlender.gameObject.activeSelf ||
				_pump.gameObject.activeSelf ||
				_heater.gameObject.activeSelf;
		_cart.gameObject.SetActive(cartVis);
	}
	//end of visibility methods******************************************************
	
	//Data methods - assigning values to hardware************************************
	public void SetPumpVal(string topic, float val){
		switch(topic){
			case "ECMO_Pump/centrifugal/flow_rate":
				_pd._flowRate=val;
				break;
			case "ECMO_Pump/pressure_ven":
				_pd._pressureVen=val;
				break;
			case "ECMO_Pump/pressure_art":
				_pd._pressureArt=val;
				break;
			case "ECMO_Pump/temperature_ven":
				_pd._tempVen=val;
				break;
			case "ECMO_Pump/temperature_art":
				_pd._tempArt=val;
				break;
			case "ECMO_Pump/pressure_dp":
				_pd._pressureDelta=val;
				break;
			case "ECMO_Pump/pressure_int":
				_pd._pressureInt=val;
				break;
			case "ECMO_Pump/pressure_aux":
				break;
			case "ECMO_Pump/svo2":
				_pd._svo2=val;
				break;
			case "ECMO_Pump/hb":
				_pd._hb=val;
				break;
			case "ECMO_Pump/hct":
				_pd._hct=val;
				break;
			case "ECMO_Pump/centrifugal/rpm":
				_pd._rpm=val;
				break;
		}
		if(_pump!=null)
			_pump.SyncVals(_pd);
	}

	public void SetIbgaFloat(string topic, float f){
		//for now assume all values go to terumo
		//once we get more ibga's we may send these to a different unit
		switch(topic){
			case "IBGA/post_ox/ph":
				_ibgaData._postPh=f;
				break;
			case "IBGA/post_ox/pco2":
				_ibgaData._postPco2=f;
				break;
			case "IBGA/post_ox/po2":
				_ibgaData._postPo2=f;
				break;
			case "IBGA/temperature_art":
				_ibgaData._tempArt=f;
				break;
			case "IBGA/temperature_ven":
				_ibgaData._tempVen=f;
				break;
			case "IBGA/post_ox/so2":
				_ibgaData._postSo2=f;
				break;
			case "IBGA/hco3":
				_ibgaData._hco3=f;
				break;
			case "IBGA/be":
				_ibgaData._be=f;
				break;
			case "IBGA/k_plus":
				_ibgaData._kPlus=f;
				break;
			case "IBGA/vo2":
				_ibgaData._vo2=f;
				break;
			case "IBGA/pre_ox/ph":
				_ibgaData._prePh=f;
				break;
			case "IBGA/pre_ox/pco2":
				_ibgaData._prePco2=f;
				break;
			case "IBGA/pre_ox/po2":
				_ibgaData._prePo2=f;
				break;
			case "IBGA/do2":
				_ibgaData._do2=f;
				break;
			case "IBGA/hct":
				_ibgaData._hct=f;
				break;
			case "IBGA/pre_ox/so2":
				_ibgaData._preSo2=f;
				break;
			case "IBGA/hgb":
				_ibgaData._hgb=f;
				break;
			case "IBGA/flow_rate":
				_ibgaData._flowRate=f;
				break;
		}
		if(_terumo!=null)
			_terumo.SyncVals(_ibgaData);
	}

	public void SetHeaterValue(string topic, float f){
		switch(topic){
			case "HeaterCooler/water/level":
				_heaterData._level=f;
				break;
			case "HeaterCooler/water/temperature/setpoint":
				_heaterData._setPoint=f;
				break;
			case "HeaterCooler/water/temperature/actual":
				_heaterData._actual=f;
				break;
		}
		if(_getinge!=null)
			_getinge.SyncVals(_heaterData);
	}

	public void SetGasBlenderValue(string topic, float f){
		switch(topic){
			case "Gas_Mixer/rate":
				_airData._rate=f;
				break;
			case "Gas_Mixer/fio2":
				_airData._fio2=f;
				break;
		}
		if(_air!=null)
			_air.SyncVals(_airData);
	}

	//simple relay - not really worried about switching pump models mid-alarm yet...
	public void SetPumpAlarm(int code, int type, string msg, float vol){
		_pump.SetAlarm(code,type,msg,vol);
	}

	//Other methods - that don't fit into above categories****************************
	
	public void SetIbgaModules(int index){
		//#todo move code from mqtt to here to better manage ibgas
		//ibga set modules
		//reset tube stuff
		ConnectIBGA(index==1);
	}	

	public void SetPumpValid(bool v){
		_pump.SetValid(v);
	}

	public bool IsFlowing(){
		return _pd._flowRate!=0;
	}

	public void SetHardwareMode(bool hard){
		SetPumpVisible(!hard);
		_hardwareMode=hard;
		//disable clamps
		GameObject [] hoffs = GameObject.FindGameObjectsWithTag("Hoffman");
		foreach(GameObject h in hoffs)
		{
			h.SetActive(!hard);
		}
		_circuit.EnableClamps(!hard);
	}

	public void ConnectGas(bool connected){
		if(_air==null)
			return;
		if(connected)
			_air.ConnectOutflow();
		else
			_air.DisconnectOutflow();
	}
	//end of Other methods*****************************************************************************************


	//tubing connection methods ***********************************************************************************
	void ConnectWaterHoses(){
		_tubeMan.Destroy(_waterOut);
		_tubeMan.Destroy(_waterIn);
		float grav=0.01f;
		//water hoses require heater cooler and oxy
		if(_heater.gameObject.activeInHierarchy && _oxygenator.gameObject.activeInHierarchy){
			//_waterOut = _tubeMan.Connect(_heater.Find("WaterOut"),_oxygenator.Find("WaterIn"),TubeManager.TubeTypes.WATER,"WaterA",-1,0);
			//_waterIn = _tubeMan.Connect(_heater.Find("WaterIn"),_oxygenator.Find("WaterOut"),TubeManager.TubeTypes.WATER,"WaterB",-1,grav);
			_waterOut = _tubeMan.Connect(_heater.Find("WaterOut"),FindRecursive(_oxygenator,"WaterIn"),TubeManager.TubeTypes.WATER,"WaterA",-1,0);
			_waterIn = _tubeMan.Connect(_heater.Find("WaterIn"),FindRecursive(_oxygenator,"WaterOut"),TubeManager.TubeTypes.WATER,"WaterB",-1,grav);
		}
	}

	void ConnectGasTubes(){
		_tubeMan.Destroy(_oxygenIn);
		_tubeMan.Destroy(_airIn);
		//gas tubes require gas blender
		if(_gasBlender.gameObject.activeInHierarchy)
		{
			_oxygenIn = _tubeMan.Connect(GameObject.Find("OxygenWallPort").transform,
					_gasBlender.Find("OxygenIn"),TubeManager.TubeTypes.OXYGEN,"oxygenIn", -1, 1);
			_airIn = _tubeMan.Connect(GameObject.Find("AirWallPort").transform,
					_gasBlender.Find("AirIn"), TubeManager.TubeTypes.AIR,"airIn", -1, 1);
		}
		ConnectMixedAirTube();
	}

	void ConnectMixedAirTube(){
		_tubeMan.Destroy(_mixedAir);
		//mixed air tube requires gas blender and oxy
		if(_gasBlender.gameObject.activeInHierarchy && _oxygenator.gameObject.activeInHierarchy){
			//#hack
			float grav = _activePump==1? _mixedAirGravCardiohelp : 0.7f;
			/*
			_mixedAir = _tubeMan.Connect(_gasBlender.Find("AirOut"),
					_oxygenator.Find("AirIn"),TubeManager.TubeTypes.MIXED_AIR,"mixedAir",-1,grav);
					*/
			_mixedAir = _tubeMan.Connect(_gasBlender.Find("AirOut"),
					FindRecursive(_oxygenator,"AirIn"),TubeManager.TubeTypes.MIXED_AIR,"mixedAir",-1,grav);
			//add mesh collider
			_mixedAir.gameObject.AddComponent<MeshCollider>();
			//add click detection
			ClickDetection cd = _mixedAir.gameObject.AddComponent<ClickDetection>();
			cd._outlineThickness=2f;
			//for some reason the unity events are null when instanced at runtime, and this default constructor solves that...
			if(cd._onClick==null)
				cd._onClick = new UnityEvent();
			cd._onClick.AddListener(delegate{_air.ManualConnectOutflow();});
			cd.enabled=false;
			_air._airClick=cd;
		}
	}
	
	void ConnectHemofilter(){
		Tube.TubeData td1 = _tubeMan.Destroy(_hemoIn);
		Tube.TubeData td2 = _tubeMan.Destroy(_hemoOut);
		Tube.TubeData td3 = _tubeMan.Destroy(_hemoDrain);

		float tubeGrav=0.1f;
		//hemofilter cables require hemofilter and oxy
		if(_oxygenator.gameObject.activeInHierarchy && _hemofilter.gameObject.activeInHierarchy){
			//_hemoIn = _tubeMan.Connect(_oxygenator.Find("Outflow"),_hemofilter.Find("Inflow"),TubeManager.TubeTypes.DELIVERY,"HemoIn",0.004f,tubeGrav);
			_hemoIn = _tubeMan.Connect(FindRecursive(_oxygenator,"Outflow"),_hemofilter.Find("Inflow"),TubeManager.TubeTypes.DELIVERY,"HemoIn",0.004f,tubeGrav);
			if(td1.segment!=-1)
				_hemoIn._data=td1;
			_hemoOut = _tubeMan.Connect(_hemofilter.Find("Outflow"),_inflow,TubeManager.TubeTypes.DELIVERY,"hemoOut",0.004f,tubeGrav);
			if(td2.segment!=-1)
				_hemoOut._data=td2;
			_hemoDrain = _tubeMan.Connect(_hemofilter.Find("HemoDrain"), _hemofilter.Find("hemo_can").Find("HemoCatch"), TubeManager.TubeTypes.DRAINAGE,"hemoDrain", 0.004f, tubeGrav);
			if(td3.segment!=-1)
				_hemoDrain._data=td3;

			_hemoIn._data.segment = 4;
			_hemoOut._data.segment = 4;
			_hemoDrain._data.segment = 5;

			//hook up flow
			_pumpToOxy.AddNextTube(_hemoIn);
			_hemoIn.AddNextTube(_hemoOut);
			_hemoIn.AddNextTube(_hemoDrain);

			//allow drain color to be set regardless of flow
			_hemoDrain._canSetBloodDirect=true;
		}
	}

	void ConnectIBGA(bool bothModules=true){
		Tube.TubeData td1 = _tubeMan.Destroy(_ibgaVenIn);
		Tube.TubeData td2 = _tubeMan.Destroy(_ibgaVenOut);
		Tube.TubeData td3 = _tubeMan.Destroy(_ibgaArtIn);
		Tube.TubeData td4 = _tubeMan.Destroy(_ibgaArtOut);
		_tubeMan.Destroy(_shuntCable);
		float tubeGrav=0.10f;

		//ibga cables require oxy and ibga
		if(_oxygenator.gameObject.activeInHierarchy && _ibga.gameObject.activeInHierarchy){
			if(bothModules){
				//create tubes
				_ibgaVenIn = _tubeMan.Connect(_ibga.Find("VenIn"),_inflow,TubeManager.TubeTypes.DRAINAGE,"ibgaVenIn",0.004f,tubeGrav,true);
				if(td1.segment!=-1)
					_ibgaVenIn._data=td1;
				_ibgaVenOut = _tubeMan.Connect(_mainInflow.transform.GetChild(0),_ibga.Find("VenOut"),TubeManager.TubeTypes.DRAINAGE,"ibgaVenOut",0.004f,0,true);
				if(td2.segment!=-1)
					_ibgaVenOut._data=td2;
				//flow animation
				_mainInflow.AddNextTube(_ibgaVenIn);
				_ibgaVenIn.AddNextTube(_ibgaVenOut);
			}
			//_ibgaArtIn = _tubeMan.Connect(_oxygenator.Find("Outflow"),_ibga.Find("ArtIn"),TubeManager.TubeTypes.DELIVERY,"ibgaArtIn",0.004f,tubeGrav,true);
			_ibgaArtIn = _tubeMan.Connect(FindRecursive(_oxygenator,"Outflow"),_ibga.Find("ArtIn"),TubeManager.TubeTypes.DELIVERY,"ibgaArtIn",0.004f,tubeGrav,true);
			if(td3.segment!=-1)
				_ibgaArtIn._data=td3;
			_ibgaArtOut = _tubeMan.Connect(_ibga.Find("ArtOut"),_mainOutflow.transform.GetChild(0),TubeManager.TubeTypes.DELIVERY,"ibgaArtOut",0.004f,0,true);
			if(td4.segment!=-1)
				_ibgaArtOut._data=td4;

			//arterial side flow anim
			_pumpToOxy.AddNextTube(_ibgaArtIn);
			_ibgaArtIn.AddNextTube(_ibgaArtOut);

			//position h/s cuvette
			StartCoroutine(PositionHsShuntR());
		}
	}

	IEnumerator PositionHsShuntR(){
		//position after delay for reasons beyond me
		//#todo determine reasons - something to do with the order in which things get connected maybe...
		yield return new WaitForSeconds(0.1f);
		Vector3 pos = _mainInflow.GetWorldPosByDist(0.2f);
		Vector3 forward = _mainInflow.GetWorldForwardByDist(0.2f);
		_hsCuvette.position=pos;
		_hsCuvette.up=forward;

		//connect shunt
		_shuntCable = _tubeMan.Connect(_hsCuvette,_ibga.GetChild(0).GetChild(0).Find("ShuntPort"),
				TubeManager.TubeTypes.DEFAULT,"hsShuntCable",0.0025f,0,true);
	}

	void ConnectCircuitToPatient(){
		_circuit.SetInflowPort(_inflow);
		//_circuit.SetOutflowPort(_oxygenator.Find("Outflow"));
		_circuit.SetOutflowPort(FindRecursive(_oxygenator,"Outflow"));
	}

	void ConnectPumpHeadToOxy(){
		_mainInflow = _circuit.GetInflowTube();
		_mainOutflow = _circuit.GetOutflowTube();
		_tubeMan.Destroy(_pumpToOxy);
		Tube.TubeData td1 = _tubeMan.Destroy(_pumpToOxy);
		if(_pumpHead!=null)
		{
			//_pumpToOxy = _tubeMan.Connect(_oxygenator.Find("Inflow"),_pumpHead.Find("Outflow"),TubeManager.TubeTypes.DRAINAGE,"pumpToOxy",0.00625f);
			_pumpToOxy = _tubeMan.Connect(FindRecursive(_oxygenator,"Inflow"),FindRecursive(_pumpHead,"Outflow"),TubeManager.TubeTypes.DRAINAGE,"pumpToOxy",0.00625f);
			_mainInflow.AddNextTube(_pumpToOxy);
			_pumpToOxy.AddNextTube(_mainOutflow);
		}
		else{//cardiohelp doesn't have seperate pump head
			//_pumpToOxy = _tubeMan.Connect(_oxygenator.Find("Inflow"),_pump.transform.Find("Outflow"),TubeManager.TubeTypes.DRAINAGE,"pumpToOxy",0.00625f);
			_pumpToOxy = _tubeMan.Connect(FindRecursive(_oxygenator,"Inflow"),_pump.transform.Find("Outflow"),TubeManager.TubeTypes.DRAINAGE,"pumpToOxy",0.00625f);
			_mainInflow.AddNextTube(_pumpToOxy);
			_pumpToOxy.AddNextTube(_mainOutflow);
		}
		if(td1.segment!=-1)
			_pumpToOxy._data=td1;

		_pumpToOxy.AddNextTube(_ibgaArtIn);
	}

	//random utility - maybe this gets its own class at some point
	public static Transform FindRecursive(Transform cur, string name){
		Transform t=null;
		foreach(Transform child in cur){
			if(child.name==name)
			{
				//match found
				return child;
			}
			else
			{
				//recursive search
				t=FindRecursive(child,name);
				if(t!=null)
					return t;
			}
		}
		return null;
	}

	//editor helpers ******************************************************
	[ContextMenu("Update ICU catalog")]
	public void UpdateIcuCat(){
		string catPath = Application.streamingAssetsPath+"/Catalog_ICU.ini";
		string body = File.ReadAllText(catPath);
		body = ReplaceSection(body,_carts,"[ECMO_Cart]");
		body = ReplaceSection(body,_pumps,"[ECMO_Pump]");
		body = ReplaceSection(body,_gasBlenders,"[Gas_Blender]");
		body = ReplaceSection(body,_hemofilters,"[Hemofilter]");
		body = ReplaceSection(body,_oxygenators,"[Oxygenator]");
		body = ReplaceSection(body,_ibgas,"[IBGA]");
		body = ReplaceSection(body,_heaters,"[Heater_Cooler]");
		body = ReplaceSection(body,_flowSensorDisplays,"[Flow_Meter]");

		File.WriteAllText(catPath, body);
	}

	string ReplaceSection(string body, Transform[] prefabs, string header){
		string newBody="";
		for(int i=0; i<prefabs.Length; i++)
			newBody+=i+" = "+prefabs[i].name+System.Environment.NewLine;
		newBody+=System.Environment.NewLine;
		return IniHelper.ReplaceSection(body,header,newBody);
	}

	void OnDrawGizmos(){
		Gizmos.color = Color.red;
		Matrix4x4 rotationMatrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
		Gizmos.matrix = rotationMatrix;
		Gizmos.DrawWireCube(Vector3.up*0.5f,new Vector3(0.5f,1f,0.5f));
	}
}
