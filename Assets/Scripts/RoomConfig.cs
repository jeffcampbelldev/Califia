//RoomConfig.cs
//
//Description: Helper to set up simulation room
//Responsible for reading and writing 
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RoomConfig : MonoBehaviour
{
	NavPanel _nav;
	AvatarPlacement _ap;
	RoleManager _roles;
	[HideInInspector]
	public string _catalogPath;
	[HideInInspector]
	public string _catalogFile;
	MyMQTT _mqtt;
	System.DateTime _origin;
	public string _language="USA";

    // Start is called before the first frame update
    void Start()
    {
		_origin = new System.DateTime(1904, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
		MyMQTT [] qts = FindObjectsOfType<MyMQTT>();
		foreach(MyMQTT qt in qts){
			if(qt.gameObject.tag=="GameController")
				_mqtt=qt;
		}
		//the scene code is 0 if we start up a room right from in editor
		if(_mqtt._sceneCode==0)
		{
			_mqtt._sceneCode=SceneManager.GetActiveScene().buildIndex;
			//If starting right from editor - activate ui
			_mqtt._editor.transform.parent.GetComponent<CanvasGroup>().alpha=1f;
		}
		_catalogFile ="Catalog_"+SceneManager.GetSceneByBuildIndex(_mqtt._sceneCode).name+".ini";
		//_catalogPath = _mqtt.GetWorkPath(_catalogFile);
		_catalogPath=_mqtt._sharedFolder+'/'+_catalogFile;
		if(!File.Exists(_catalogPath)){
			Debug.Log("Failed to configure room. Unable to locate catalog @ "+_catalogPath);
			return;
		}

		//get reference to nav panel
		_nav = FindObjectOfType<NavPanel>();
		//null for vr testing
		if(_nav==null)
			return;

		_nav.OnNavsChanged+=SaveViews;

		//Load views
		LoadViews(_catalogPath,false);

		//get Avatar Placement
		_ap = FindObjectOfType<AvatarPlacement>();
		_ap.OnPlacementChanged+=SavePlacements;
		string ini = File.ReadAllText(_catalogPath);
		string[] spotsRaw = IniHelper.GetSection(ini,"Avatar_placement");
		foreach(string s in spotsRaw){
			string [] parts = s.Split('=');
			string [] sParts = parts[1].Split(',');
			string name = sParts[0].Trim();
			float xPos=0;float yPos=0; float zPos=0;
			float.TryParse(sParts[1],out xPos);
			float.TryParse(sParts[2],out yPos);
			float.TryParse(sParts[3],out zPos);
			_ap.LoadSpot(name,xPos,yPos,zPos);
		}
		LoadRoles();
    }


	public void LoadRoles(){
		//set up roles
		_roles = _mqtt.GetComponentInChildren<RoleManager>();
		_roles.OnRolesChanged+=SaveRoles;
		Dictionary<int,string> avs = IniHelper.GetStrings(_catalogPath, "Avatar");
		Dictionary<int,string> voices = IniHelper.GetStrings(_catalogPath, "Voices");
		Dictionary<int,string> azureVoices = IniHelper.GetStrings(_catalogPath, "Azure-"+_language);
		Dictionary<int, int>   avVoices = IniHelper.GetInts(_catalogPath, "Avatar-Voice assignment");
		Dictionary<int, int> aavVoices = IniHelper.GetInts(_catalogPath, "Avatar-Azure-Voice assignment");
		Dictionary<int,string> roles = IniHelper.GetStrings(_catalogPath, "Roles");
		Dictionary<int, int>   roleAvs = IniHelper.GetInts(_catalogPath, "Role-Avatar assignment");
		_roles.Setup(avs,voices,azureVoices,avVoices,aavVoices,roles,roleAvs);
	}

	public void ChangeLanguage(int l){
		_language=IniHelper.GetValue(_catalogPath, "Language",l);
		Dictionary<int,string> avs = IniHelper.GetStrings(_catalogPath, "Avatar");
		Dictionary<int,string> azureVoices = IniHelper.GetStrings(_catalogPath, "Azure-"+_language);
		if(azureVoices.Count<=0)
		{
			Debug.Log("No ini section found for Azure-"+_language);
			return;
		}
		Dictionary<int, int> aavVoices = IniHelper.GetInts(_catalogPath, "Avatar-Azure-Voice assignment");
		_roles.ChangeLanguage(avs,azureVoices,aavVoices);

	}

	void SaveViews(NavPanel.NavEventArgs nargs){
		string path = nargs.fromScenario ? _mqtt._scenCatalogPath : _catalogPath;
		if(!File.Exists(path)){
			Debug.Log("Failed to Save room views. Unable to locate config @ "+path);
			return;
		}
		if(_nav==null)
		{
			return;
		}
		//acquire new data
		string newData = _nav.SerializeNavOptions(nargs.fromScenario);
		int numNewNavs = newData.Split('\n').Length-2;

		//Replace views
		string oldConfig = File.ReadAllText(path);
		//string [] oldData = IniHelper.GetSection(oldConfig,"[View]");
		int numOldNavs = IniHelper.GetSectionSize(oldConfig,"[View]");
		string newConfig = IniHelper.ReplaceSection(oldConfig,"[View]",newData);

		//Write to working directory
		//todo lock routine
		File.WriteAllText(path,newConfig);
		Debug.Log("Writing to: "+path);
		System.TimeSpan diff = File.GetLastWriteTime(path).ToUniversalTime()-_origin;
		long dInt=(long)diff.TotalSeconds;

		//copy to shared <- todo get the reference in Start
		_mqtt = FindObjectOfType<MyMQTT>();
		_mqtt.NavChange(dInt,nargs.fromScenario);
		if(numNewNavs>numOldNavs)
			_mqtt.SendNav(numNewNavs-1);
	}

	void SavePlacements(){
		if(!File.Exists(_catalogPath)){
			Debug.Log("Failed to Save room views. Unable to locate config @ "+_catalogPath);
			return;
		}
		if(_ap==null)
			return;

		//acquire new data
		string newData = _ap.SerializePlacements();

		//read through existing config
		string oldConfig = File.ReadAllText(_catalogPath);
		string newConfig = IniHelper.ReplaceSection(oldConfig,"[Avatar_placement]",newData);
		
		//Write to shared directory
		File.WriteAllText(_catalogPath,newConfig);
		//copy to shared
		_mqtt = FindObjectOfType<MyMQTT>();
		//_mqtt.ShareFile(_catalogFile);
		//only after this write all text do we want mqtt to send the update signal
		_mqtt.ConfigUpdate();
	}

	void SaveRoles(){
		if(!File.Exists(_catalogPath)){
			Debug.Log("Failed to Save room views. Unable to locate config @ "+_catalogPath);
			return;
		}
		if(_roles==null)
			return;

		//acquire new data
		string newAvatarVoice = _roles.SerializeAvatarVoices();
		string newRoleAvatars = _roles.SerializeRoleAvatars();

		//Replace avatar voices
		string oldConfig = File.ReadAllText(_catalogPath);
		string newConfig = IniHelper.ReplaceSection(oldConfig,"[Avatar-Voice assignment]",newAvatarVoice);

		//Replace role avatars
		newConfig = IniHelper.ReplaceSection(newConfig,"[Role-Avatar assignment]",newRoleAvatars);

		//Write to shared directory
		File.WriteAllText(_catalogPath,newConfig);
		//copy to shared
		_mqtt = FindObjectOfType<MyMQTT>();
		//_mqtt.ShareFile(_catalogFile);
		//only after this write all text do we want mqtt to send the update signal
		_mqtt.ConfigUpdate();
	}

	[System.Serializable]
	public class IVBag {
		public string fluid_name;
		public string fluid_color;
		public string label_color;
		public float capacity;
		public float target_infusion_rate;
	}

	//fluids can handle string with quotes
	public List<string> LoadFluidNames(){
		string conf = File.ReadAllText(_catalogPath);
		string[] fluids = IniHelper.GetSection(conf,"IV Bags");
		List<string> names = new List<string>();
		for(int i=0; i<fluids.Length; i++){
			names.Add(fluids[i].Split('=')[0].Trim());
		}
		//bubble sort
		for(int i=1; i<=names.Count-1; i++){
			bool swapped=false;
			for(int j=0; j<names.Count-1; j++){
				if(string.Compare(names[j],names[j+1],true)>0){
					string tmp = names[j];
					names[j]=names[j+1];
					names[j+1]=tmp;
					swapped=true;
				}
			}
			if(!swapped)
				break;
		}
		return names;
	}

	//fix this bad boy to deal without returns separating headers
	//actually this is in IniHelper
	public List<IVBag> LoadFluids(){
		string conf = File.ReadAllText(_catalogPath);
		string[] fluids = IniHelper.GetSection(conf,"IV Bags");
		List<IVBag> bags = new List<IVBag>();
		for(int i=0; i<fluids.Length; i++){
			string s = fluids[i].Split('=')[1].Trim();
			if(s[0]=='\"')
				s=s.Substring(1,s.Length-2);
			bags.Add(JsonUtility.FromJson<IVBag>(s));
			bags[i].fluid_name=fluids[i].Split('=')[0].Trim();
		}
		//bubble sort
		for(int i=1; i<=bags.Count-1; i++){
			bool swapped=false;
			for(int j=0; j<bags.Count-1; j++){
				if(string.Compare(bags[j].fluid_name,bags[j+1].fluid_name,true)>0){
					IVBag tmp = bags[j];
					bags[j]=bags[j+1];
					bags[j+1]=tmp;
					swapped=true;
				}
			}
			if(!swapped)
				break;
		}
		return bags;
	}

	public void LoadScenarioCat(string path){
		Debug.Log("Loading scenario catalog");
		LoadViews(path,true);
	}

	public void LoadViews(string catPath,bool fromScenario){
		//for now just get the views
		string[] viewsRaw = IniHelper.GetSection(File.ReadAllText(catPath),"View");;
		string path =Application.streamingAssetsPath+"/Catalog_"+
					SceneManager.GetSceneByBuildIndex(_mqtt._sceneCode).name+".ini" ;
		int navs = IniHelper.GetSectionSize(File.ReadAllText(path),"View");
		_nav._lockedNavs=navs;
		
		string culture = CultureInfo.CurrentCulture.Name;
		NumberStyles style = NumberStyles.Float;

		//foreach view
		//add an item to the nav panel
		foreach(string v in viewsRaw){
			string [] parts = v.Split('=');
			string [] sParts = parts[1].Split(',');
			string name = sParts[0].Trim();
			float xPos=0;float yPos=0; float zPos=0;
			float xEul=0;float yEul=0;
			float.TryParse(sParts[1],style,CultureInfo.InvariantCulture,out xPos);
			float.TryParse(sParts[2],style,CultureInfo.InvariantCulture,out yPos);
			float.TryParse(sParts[3],style,CultureInfo.InvariantCulture,out zPos);
			float.TryParse(sParts[4],style,CultureInfo.InvariantCulture,out xEul);
			float.TryParse(sParts[5],style,CultureInfo.InvariantCulture,out yEul);
			_nav.AddNewOption(name,xPos,yPos,zPos,xEul,yEul,fromScenario);
		}
	}
}
