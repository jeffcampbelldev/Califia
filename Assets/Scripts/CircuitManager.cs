//CircuitManager.cs
//
//Description: to-do write
//

//

using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using UnityEngine;

public class CircuitManager : MonoBehaviour
{
	[System.Serializable]
	public class EcmoData{
		public Type[] CircuitTypes;
		public CannulaN[] Cannulas;
		public Site[] Sites;
	}
	[HideInInspector]
	public EcmoData _ecmoData;

	[System.Serializable]
	public class Type{
		public string name;
		public int index;
		public int[] sites;
	}

	[System.Serializable]
	//#todo I think we can change CannulaN back to Cannula at some point
	public class CannulaN{
		public string name;
		public int index;
		public float length;
		public float _depth;
		public float _rotation;
		//root is an empty transform that tracks the patient mesh at specific points
		public Transform _root;

		public void Init(){
			length*=0.01f;
			_depth=length;
		}

		public CannulaN Copy(){
			CannulaN c = new CannulaN();
			c.index=index;
			c.name=name;
			c.length=length;
			c._depth=_depth;
			c._rotation=_rotation;
			c._root=_root;
			return c;
		}

		public void CopyVals(CannulaN other){
			index=other.index;
			name=other.name;
			length=other.length;
		}

		public void CopyDepthAndRotation(CannulaN other){
			if(Mathf.Abs(other._depth-999.99f)>0.1f)
				_depth=other._depth;
			if(Mathf.Abs(other._rotation-99999f)>0.1f)
				_rotation=other._rotation;
			//happens when swapping between cannulas of unequal length
			//ideally depth is preserved during swaps, but sometimes that make no sense
			if(_depth>length)
				_depth=length;
		}
	}

	[System.Serializable]
	public class Site{
		public string name;
		public int index;
		public int def;
		public int defDl;
	}

	[System.Serializable]
	public class TubeClamps{
		public int segment;
		public ClampData [] clamps;
	}

	[System.Serializable]
	public class ClampData{
		public int type;
		public int value;
		public float position;

		public ClampData(){
			type=0;
			value=0;
			position=0;
		}

		public ClampData(int t, int v, float p){
			type=t;
			value=v;
			position=p;
		}
	}

	public Dictionary<int,TubeClamps> _clampConfig = new Dictionary<int, TubeClamps>();
	[HideInInspector]
	public MyMQTT _mqtt;
	Tube [] _tubing;
	List<Tube> _activeTubing = new List<Tube>();
	CannulaMenu _canMen;
	int _curType;
	Inventory _inventory;
	ItemStand _flowSensorStand;
	[HideInInspector]
	public Camera _main;
	[HideInInspector]
	public Camera _hoffmanAlt;
	public float[] _defaultSegmentOffsets;
	ItemStand _clampStand;
	Patient _patient;
	TubeManager _tubeMan;
    // Start is called before the first frame update
    void Start()
    {
		//get camera
		_main = Camera.main;
		//get patient
		_patient = FindObjectOfType<Patient>();
		//get mqtt
		MyMQTT [] qts = FindObjectsOfType<MyMQTT>();
		foreach(MyMQTT qt in qts){
			if(qt.gameObject.tag=="GameController")
				_mqtt=qt;
		}
		//get inventory
		_inventory = FindObjectOfType<Inventory>();
		//get tubes
		GameObject[] tubes = GameObject.FindGameObjectsWithTag("Circuit");
		_tubing = new Tube[tubes.Length];
		for(int i=0;i<tubes.Length; i++)
			_tubing[i]=tubes[i].GetComponent<Tube>();

		_tubeMan = FindObjectOfType<TubeManager>();
		//get cannulas
		_canMen = FindObjectOfType<CannulaMenu>();

		//initialize clamps
		for(int i=0; i<6; i++){
			_clampConfig.Add(i, new TubeClamps());
			_clampConfig[i].segment=i;
		}

		//read ecmo data from json
		string path = Application.streamingAssetsPath+"/Ecmo.json";
		string content = File.ReadAllText(path);
		_ecmoData = JsonUtility.FromJson<EcmoData>(content);
		//init cannulas
		foreach(CannulaN c in _ecmoData.Cannulas)
			c.Init();
    }

	public void ConfigureTube(Tube.CircuitData cd)
	{
		//#question - do we even need this default _tubing thing? especially since most tubes are created otf...
		
		//get tube manager tubes
		Tube [] tubes = _tubeMan.transform.GetComponentsInChildren<Tube>();
		foreach(Tube.TubeData td in cd.Circuit_Data){
			foreach(Tube t in _tubing){
				if(t._data.segment==td.segment)
					t.ConfigureTube(td);
			}
			foreach(Tube t in tubes){
				if(t.transform.tag=="Circuit"){
					if(t._data.segment==td.segment)
						t.ConfigureTube(td);
				}
			}
		}

	}

	public void ClearClamps(){
		//reset internal data
		foreach(int segment in _clampConfig.Keys){
			_clampConfig[segment].clamps=null;
		}

		//send config to tubes
		foreach(Tube t in _tubing){
			t.ConfigureClamps(_clampConfig[t._data.segment].clamps);
		}
	}

	//used when changing circuit type
	public void ResetClamps(){
		//return of circuitManager not set up
		if(_defaultSegmentOffsets.Length==0)
			return;
		//reset internal data
		foreach(int segment in _clampConfig.Keys){
			switch(segment){
				case 1:
					if(!_mqtt._hardware)
					{
						_clampConfig[segment].clamps=new ClampData[1];
						ClampData clamp = new ClampData(0,0,_defaultSegmentOffsets[1]);
						_clampConfig[segment].clamps[0]=clamp;
					}
					break;
				case 3:
					_clampConfig[segment].clamps=new ClampData[1];
					ClampData clamp2 = new ClampData(1,1,_defaultSegmentOffsets[3]);
					_clampConfig[segment].clamps[0]=clamp2;
					break;
				case 4:
					_clampConfig[segment].clamps=new ClampData[1];
					ClampData clamp3 = new ClampData(0,0,_defaultSegmentOffsets[4]);
					_clampConfig[segment].clamps[0]=clamp3;
					break;
				default:
					break;
			}
		}

		//send config to tubes
		ApplyClamps();

		//check clamps on stand
		CheckClampStand();
	}

	//used by resetClamps
	public void ApplyClamps(){
		//find the active tubes
		foreach(Tube t in _tubing){
			if(t.gameObject.activeInHierarchy && t._meshR.enabled && t._clampable)
				t.ConfigureClamps(_clampConfig[t._data.segment].clamps);
		}
	}

	public ClampData[] GetClamps(int segment){
		return _clampConfig[segment].clamps;
	}

	//called via mqtt
	public void ConfigureClamps(string txt){
		TubeClamps tc = JsonUtility.FromJson<TubeClamps>(txt);
		_clampConfig[tc.segment]=tc;
		foreach(Tube t in _tubing){
			if(t._data.segment==tc.segment)
				t.ConfigureClamps(tc.clamps);
		}

		//set amount of clamps on stand based on tube clamps currently in use
		CheckClampStand();
	}

	public void CheckClampStand(){
		int clamps=0;
		foreach(Tube t in _activeTubing){
			clamps += t.GetNumClamps();
		}
		//currently no clamp stand in or
		if(_clampStand==null)
			return;
		_clampStand.SetNumItemsRemoved(clamps);
	}

	public void UpdateClampConfig(int segment, ClampData[] clamps){
		//save clamp data
		_clampConfig[segment].clamps=clamps;
		string data = JsonUtility.ToJson(_clampConfig[segment]);
		//send over mqtt
		_mqtt.ForceClamps(data);
	}

	public void SetCurrentCircuit(int index,bool init=false){
		bool pub=false;
		if(_curType!=index && !init)
			pub=true;
		_curType=index;

		//clear clamps
		ClearClamps();
		
		//magical offset numbers
		bool child = _patient._myAge<13 && _patient._myAge>0;
		int childSub = child ? 3 : 0;
		int dlChildAdd = child ? 1 : 0;
		bool dlCircuit=_curType==3||_curType==5;

		//set default cannulas
		List<int> sites = new List<int>();
		foreach(int site in _ecmoData.CircuitTypes[_curType].sites){
			CannulaN def;
			//default for delivery sites
			if(site==7&&dlCircuit)
				def = _ecmoData.Cannulas[_ecmoData.Sites[site].defDl+dlChildAdd];
			//default for drainage sites
			else
				def = _ecmoData.Cannulas[_ecmoData.Sites[site].def-childSub];
			//if no cannula t site use default
			if(!_canMen.HasCannulaAtSite(site))
			{
				_canMen.SetCannula(def, site, true, false);
			}
			//if there is a cannula at the site, we need to check special cases where we need to forcefully
			//change the cannula i.e. the circuit and site want a DL cannula, but a SL cannula is in place 
			//and vica versa
			else if(site==7){//kinda ugly but right now the only site that could support dl is 7 / internal jugular
				//if it wants a DL
				if(dlCircuit){
					//and cannula is already a dl
					if(_ecmoData.Cannulas[_canMen.GetCannulaAtSite(site)].name.Contains("Dual_Lumen"))
					{/*fine*/}
					else
						_canMen.SetCannula(def, site, true, true);//pub is true
				}
				//circuit is NOT dl
				else{
					//dl is already in
					if(_ecmoData.Cannulas[_canMen.GetCannulaAtSite(site)].name.Contains("Dual_Lumen"))
						//set default
						_canMen.SetCannula(def, site, true, true);//pub is true
					else
					{/*fine*/}
				}
			}
			sites.Add(site);
		}
		//place empties on all sites that don't contain a cannula - I think...
		for(int i=0; i<10; i++){
			if(!sites.Contains(i))
				_canMen.SetCannula(_ecmoData.Cannulas[0],i,true,false);
		}

		//pub sites 0, 3, and 7
		if(pub){
			_canMen.PubCan(0);
			_canMen.PubCan(3);
			_canMen.PubCan(7);
		}
		
		//set circuit
		switch(index){
			case 0:
				//No circuit
				foreach(Tube t in _tubing){
					t.EnableTube(t._ibga);
				}
				break;
			case 1:
				//VA 
				foreach(Tube t in _tubing){
					t.EnableTube((t._data.segment==0 && t._fem==true) ||
							(t._data.segment==1 && t._type=="VA") ||
							t._ibga);
				}
				break;
			case 2:
				//VAV fem-fem-ij
				foreach(Tube t in _tubing){
					t.EnableTube((t._data.segment==0 && t._fem==true) ||
							(t._data.segment==1 && t._type=="VAV") ||
							(t._data.segment==2 || t._data.segment==3) ||
							t._ibga);
					t.SetFlowSensor(t._data.segment==3);
				}
				break;
			case 3:
				//VAV fem-ij-dl
				foreach(Tube t in _tubing){
					t.EnableTube((t._data.segment==0 && t._fem==false) ||
							(t._data.segment==1 && t._type=="VAV") ||
							(t._data.segment==2 || t._data.segment==3) ||
							t._ibga);
					t.SetFlowSensor(t._data.segment==3);
				}
				break;
			case 4:
				//VV fem-ij
				foreach(Tube t in _tubing){
					t.EnableTube((t._data.segment==0 && t._fem==true) ||
							(t._data.segment==1 && t._type=="VV") ||
							t._ibga); 
				}
				break;
			case 5:
				//VV ij-dl
				foreach(Tube t in _tubing){
					t.EnableTube((t._data.segment==0 && t._fem==false) ||
							(t._data.segment==1 && t._type=="VV") ||
							t._ibga); 
				}
				break;
			default:
				break;
		}

		//check flow sensor
		bool sensorInCircuit=false;
		foreach(Tube t in _tubing){
			if(t._sensingFlow && t._meshR.enabled)
			{
				sensorInCircuit=true;
			}
		}
		if(!sensorInCircuit && !_inventory.HasItem("FlowSensor"))
		{
			if(_flowSensorStand!=null)
				_flowSensorStand.ResetItems();
		}

		//get active tubing
		_activeTubing.Clear();
		foreach(Tube t in _tubing)
		{
			if(t._meshR.enabled)
				_activeTubing.Add(t);
		}
		
		//reset clamps
		ResetClamps();
	}

	public void RefreshCircuit(){
		SetCurrentCircuit(_curType);
	}

	//called via mqtt on set hoffman vis
	public void ConfigureHoffman(int segment, int val){
		foreach(Tube t in _tubing){
			if(t._data.segment==segment){
				t.ConfigureHoffman(val);
			}
		}
	}
	public void EnableHoffman(bool enabled){
		foreach(Tube t in _tubing){
			ConfigureHoffman(3,100);
			t.EnableHoffman(enabled);
		}
	}

	//todo: replace mega-hacky code with more robust data model
	public List<int> GetAvailableCannulas(int site){
		List<int> cans = new List<int>();
		foreach(CannulaN c in _ecmoData.Cannulas){
			if(site==0 && c.name.Contains("Delivery"))
				cans.Add(c.index);
			else if(site==3 && c.name.Contains("Drainage"))
				cans.Add(c.index);
			else if(site==7){
				if(_curType==3||_curType==5){
					if(c.name.Contains("Lumen"))
						cans.Add(c.index);
				}
				else if(c.name.Contains("Delivery"))
					cans.Add(c.index);
			}
		}
		return cans;
	}

	//todo show active cannulas not just defaults
	public string GetDetails(int circuit){
		Type t = _ecmoData.CircuitTypes[circuit];
		string dets="";
		bool active=_curType==circuit;
		//if circuit is active, dets = active cannulas
		//else, dets = default cannulas
		if(active)
			dets+="Active Cannulas: \n\n";
		else
			dets+="Default Cannulas: \n\n";

		//string dets="Defaults: \n\n";
		foreach(int s in t.sites){
			Site si = _ecmoData.Sites[s];
			dets+="<b>"+si.name+"</b>"+":\n";
			if(!active)
				dets+=_ecmoData.Cannulas[si.def].name+"\n";
			else
				dets+=_ecmoData.Cannulas[_canMen.GetCannulaAtSite(s)].name+"\n";
		}
		return dets;
	}

	public void SetInflowPort(Transform root){
		GetInflowTube()._root=root;
	}

	public void SetOutflowPort(Transform root){
		GetOutflowTube()._root=root;
	}

	public Tube GetInflowTube(){
		foreach(Tube t in _tubing){
			if(t._data.segment==0&&!t._ibga&&t._meshR.enabled)
			{
				return t;
			}
		}
		return null;
	}

	public Tube GetOutflowTube(){
		foreach(Tube t in _tubing){
			if(t._data.segment==1&&!t._ibga&&t._meshR.enabled)
			{
				return t;
			}
		}
		return null;
	}

	public void EnableClamps(bool canClamp){
		foreach(Tube t in _tubing){
			if(!t._ibga)
			{
				t.EnableClamps(canClamp);
			}
		}

	}
}
