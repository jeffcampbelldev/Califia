using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class ConfigHelper : EditorWindow
{
	[MenuItem("Config/Current Room/Update room materials")]
	static void UpdateMaterials(){
		string configPath = Application.streamingAssetsPath+"/Config_"+EditorSceneManager.GetActiveScene().name+".ini";
		if(!File.Exists(configPath)){
			Debug.Log("Failed to configure room. Unable to locate config @ "+configPath);
		}
		else{
			//serialize current materials
			MaterialManager mm = GameObject.Find("RoomConfig").GetComponent<MaterialManager>();
			string newData = mm.SerializeFlooring();

			//remove existing config section
			string [] lines = File.ReadAllLines(configPath);
			string newConfig = "";
			bool atSection=false;
			foreach(string l in lines){
				if(!atSection){
					if(l=="[Flooring]")
						atSection=true;
					newConfig+=l+System.Environment.NewLine;
				}
				else if(atSection){
					if(l=="")
					{
						newConfig+=newData;
						atSection=false;//just so we continue adding back original lines
					}
					else
					{
						//actually just don't do anything here
					}
				}
			}
			if(atSection)
				newConfig+=newData;
			//replace with new materials
			File.WriteAllText(configPath,newConfig);
			RefreshLocalData();
		}
	}

	[MenuItem("Config/Current Room/Update Ecmo Pumps")]
	static void UpdatePumps(){
		string configPath = Application.streamingAssetsPath+"/Config_"+EditorSceneManager.GetActiveScene().name+".ini";
		if(!File.Exists(configPath)){
			Debug.Log("Failed to configure room. Unable to locate config @ "+configPath);
		}
		else{
			string newData = "";
			GameObject pumps = GameObject.Find("EcmoPump");
			if(pumps!=null){
				for(int i=0; i<pumps.transform.childCount; i++){
					newData+=i+" = "+pumps.transform.GetChild(i).name+"\n";
				}
			}
			newData+="\n";

			//remove existing config section
			string [] lines = File.ReadAllLines(configPath);
			string newConfig = "";
			bool atSection=false;
			foreach(string l in lines){
				if(!atSection){
					if(l=="[ECMO_Pump]")
						atSection=true;
					newConfig+=l+System.Environment.NewLine;
				}
				else if(atSection){
					if(l=="")
					{
						newConfig+=newData;
						atSection=false;//just so we continue adding back original lines
					}
					else
					{
						//actually just don't do anything here
					}
				}
			}
			if(atSection)
				newConfig+=newData;
			//replace with new monitors
			File.WriteAllText(configPath,newConfig);
			RefreshLocalData();
		}
	}

	[MenuItem("Config/Current Room/Update Heater units")]
	static void UpdateHeaters(){
		string configPath = Application.streamingAssetsPath+"/Config_"+EditorSceneManager.GetActiveScene().name+".ini";
		if(!File.Exists(configPath)){
			Debug.Log("Failed to configure room. Unable to locate config @ "+configPath);
		}
		else{
			string newData = "";
			GameObject heaters = GameObject.Find("HeaterCooler");
			if(heaters!=null){
				for(int i=0; i<heaters.transform.childCount; i++){
					newData+=i+" = "+heaters.transform.GetChild(i).name+"\n";
				}
			}
			newData+="\n";

			//remove existing config section
			string [] lines = File.ReadAllLines(configPath);
			string newConfig = "";
			bool atSection=false;
			foreach(string l in lines){
				if(!atSection){
					if(l=="[Heating_unit]")
						atSection=true;
					newConfig+=l+System.Environment.NewLine;
				}
				else if(atSection){
					if(l=="")
					{
						newConfig+=newData;
						atSection=false;//just so we continue adding back original lines
					}
					else
					{
						//actually just don't do anything here
					}
				}
			}
			if(atSection)
				newConfig+=newData;
			//replace with new monitors
			File.WriteAllText(configPath,newConfig);
			RefreshLocalData();
		}
	}

	[MenuItem("Config/Current Room/Update Media Monitors")]
	static void UpdateMonitors(){
		string configPath = Application.streamingAssetsPath+"/Config_"+EditorSceneManager.GetActiveScene().name+".ini";
		if(!File.Exists(configPath)){
			Debug.Log("Failed to configure room. Unable to locate config @ "+configPath);
		}
		else{
			string newData = "";
			MediaMonitor [] _medias = FindObjectsOfType<MediaMonitor>();
			//bubble sort
			for(int i=1; i<=_medias.Length-1; i++){
				bool swapped=false;
				for(int j=0; j<_medias.Length-1; j++){
					if(_medias[j]._monitorId>_medias[j+1]._monitorId){
						MediaMonitor tmp = _medias[j];
						_medias[j]=_medias[j+1];
						_medias[j+1]=tmp;
						swapped=true;
					}
				}
				if(!swapped)
					break;
			}
			foreach(MediaMonitor mm in _medias){
				newData+=mm.SerializeMonitor()+"\n";
			}
			newData+="\n";

			//remove existing config section
			string [] lines = File.ReadAllLines(configPath);
			string newConfig = "";
			bool atSection=false;
			foreach(string l in lines){
				if(!atSection){
					if(l=="[Media displays]")
						atSection=true;
					newConfig+=l+System.Environment.NewLine;
				}
				else if(atSection){
					if(l=="")
					{
						newConfig+=newData;
						atSection=false;//just so we continue adding back original lines
					}
					else
					{
						//actually just don't do anything here
					}
				}
			}
			if(atSection)
				newConfig+=newData;
			//replace with new monitors
			File.WriteAllText(configPath,newConfig);
			RefreshLocalData();
		}
	}

	[MenuItem("Config/EdgeCalifia3D/Overwrite From NEW")]
	static void UpdateIni(){
		string currentTopicsPath = Application.streamingAssetsPath+"/EdgeCalifia3D.ini";
		string futureTopicsPath = Application.streamingAssetsPath+"/EdgeCalifia3D_new.ini";

		//overwrite ini
		File.WriteAllText(currentTopicsPath,File.ReadAllText(futureTopicsPath));
		INIParser ini = new INIParser();
		ini.Open(currentTopicsPath);
		ini.WriteValue("Base","Prefix","");
		ini.WriteValue("Base","SharedFolder","");
		ini.Close();
	}

	[MenuItem("Config/ICU/Update Catalog")]
	static void UpdateIcu(){
		string resourcesCurrent = Application.streamingAssetsPath+"/Catalog_ICU.ini";
		string resourcesNew = Application.streamingAssetsPath+"/Catalog_ICU_new.ini";

		//overwrite ini
		File.WriteAllText(resourcesCurrent,File.ReadAllText(resourcesNew));
	}

	[MenuItem("Config/OR/Update Catalog")]
	static void UpdateOr(){
		string resourcesCurrent = Application.streamingAssetsPath+"/Catalog_OR.ini";
		string resourcesNew = Application.streamingAssetsPath+"/Catalog_OR_new.ini";

		//overwrite ini
		File.WriteAllText(resourcesCurrent,File.ReadAllText(resourcesNew));
	}

	[MenuItem("Config/OR/Save Catalog")]
	static void SaveOr(){
		string currentCatalog = Application.streamingAssetsPath+"/Catalog_OR.ini";
		string localAppData = System.Environment.ExpandEnvironmentVariables("%localappdata%").Replace("\\","/");
		localAppData+="/Cal3D";
		string sharedPath = localAppData+"/Catalog_OR.ini";

		//overwrite ini
		File.WriteAllText(currentCatalog,File.ReadAllText(sharedPath));
	}

	[MenuItem("Config/ICU/Save Catalog")]
	static void SaveIcu(){
		string currentCatalog = Application.streamingAssetsPath+"/Catalog_ICU.ini";
		string localAppData = System.Environment.ExpandEnvironmentVariables("%localappdata%").Replace("\\","/");
		localAppData+="/Cal3D";
		string sharedPath = localAppData+"/Catalog_ICU.ini";

		//overwrite ini
		File.WriteAllText(currentCatalog,File.ReadAllText(sharedPath));
	}

	[MenuItem("Config/EdgeCalifia3D/Diff topics NEW")]
	static void DiffTopics(){
		string currentTopicsPath = Application.streamingAssetsPath+"/EdgeCalifia3D.ini";
		string futureTopicsPath = Application.streamingAssetsPath+"/EdgeCalifia3D_new.ini";

		if(!File.Exists(currentTopicsPath)){
			Debug.LogError("Error: Could not find current topics path"+currentTopicsPath);
		}
		else if(!File.Exists(futureTopicsPath)){
			Debug.LogError("Error: Could not find future topics path: "+futureTopicsPath);
		}
		else{
			//Read topics
			Dictionary<int, string> currentSubs = GetElementsUnderHeader(currentTopicsPath,"sub");
			Dictionary<int, string> currentPubs = GetElementsUnderHeader(currentTopicsPath,"pub");
			Dictionary<int, string> futureSubs = GetElementsUnderHeader(futureTopicsPath,"sub");
			Dictionary<int, string> futurePubs = GetElementsUnderHeader(futureTopicsPath,"pub");

			Debug.ClearDeveloperConsole();
			//Diff pubs
			CheckDiff(currentPubs,futurePubs,"pub","blue","added");
			CheckDiff(futurePubs,currentPubs,"pub","red","removed");
			CheckDiff(currentSubs,futureSubs,"sub","blue","added");
			CheckDiff(futureSubs,currentSubs,"sub","red","removed");
		}
	}

	[MenuItem("Config/Topic Table/Diff TopicTable.ini")]
	static void DiffTopicTable(){
		string currentTopicsPath = Application.streamingAssetsPath+"/EdgeCalifia3D.ini";
		string ttPath = Application.streamingAssetsPath+"/TopicTable.ini";

		if(!File.Exists(currentTopicsPath)){
			Debug.LogError("Error: Could not find current topics path"+currentTopicsPath);
		}
		else if(!File.Exists(ttPath)){
			Debug.LogError("Error: Could not find topic table path: "+ttPath);
		}
		else{
			//Read topics
			Dictionary<int, string> currentSubs = GetElementsUnderHeader(currentTopicsPath,"sub");
			Dictionary<int, string> currentPubs = GetElementsUnderHeader(currentTopicsPath,"pub");
			Dictionary<int, string> ttSubs = GetElementsUnderHeader(ttPath,"sub");
			Dictionary<int, string> ttPubs = GetElementsUnderHeader(ttPath,"pub");

			Debug.ClearDeveloperConsole();
			//Diff pubs
			CheckDiff(ttPubs,currentPubs,"pub","blue","added");
			CheckDiff(currentPubs,ttPubs,"pub","red","removed");
			CheckDiff(ttSubs,currentSubs,"sub","blue","added");
			CheckDiff(currentSubs,ttSubs,"sub","red","removed");
		}
	}

	[MenuItem("Config/Topic Table/Overwrite TopicTable.ini")]
	static void OverwriteTopicTable(){
		string currentTopicsPath = Application.streamingAssetsPath+"/EdgeCalifia3D.ini";
		string ttPath = Application.streamingAssetsPath+"/TopicTable.ini";

		if(!File.Exists(currentTopicsPath)){
			Debug.LogError("Error: Could not find current topics path"+currentTopicsPath);
		}
		else if(!File.Exists(ttPath)){
			Debug.LogError("Error: Could not find topic table path: "+ttPath);
		}
		else{
			//get list of lines from edge3D.ini
			//get list of lines from topic table
			string[] edgeLines = File.ReadAllLines(currentTopicsPath);
			string[] topicLines = File.ReadAllLines(ttPath);
			List<string> edgeList = new List<string>(edgeLines);
			List<string> tableList = new List<string>(topicLines);
			//assign countA for edge3D start at [pub] section
			//assign countB for tt start at [pub] section
			int countA=0;
			int countB=1;
			for(int i=0; i<edgeList.Count; i++)
			{
				if(edgeList[i]=="[pub]")
				{
					countA=i+1;
					break;
				}
			}
			//loop through each pub in edge3D
			//	if tt[countB]!=pub
			//		ttList.insert(pub=undefined at countB)
			//	countA++
			//	countB++
			for(;countA<edgeList.Count; countA++){
				//break case
				if(edgeList[countA]=="[Interactive]")
					break;
				string topic=TopicsMatch(edgeList[countA],tableList[countB]);
				//Debug.Log(topic);
				if(topic!="")
					tableList.Insert(countB,topic+" = Undefined");
				countB++;
			}
			File.WriteAllLines(ttPath,tableList.ToArray());
		}
	}

	static string TopicsMatch(string a, string b){
		string [] parts = a.Split('=');
		if(parts.Length!=2)
			return "";
		string aTop = parts[1].Trim();
		parts = b.Split('=');
		if(parts.Length!=2)
			return "";
		string bTop = parts[0].Trim();
		if(aTop==bTop)
			return "";
		return aTop;
	}

	static void CheckDiff(Dictionary<int,string> cur, Dictionary<int,string> fut, string label,string color,string msg){
		foreach(string t in fut.Values){
			bool exists=false;
			foreach(string to in cur.Values){
				if(to==t){
					exists=true;
					break;
				}
			}
			if(!exists)
				Debug.Log("<color="+color+">"+label+" "+msg+": </color>"+t);
		}
	}

	static Dictionary<int,string> GetElementsUnderHeader(string path, string header){
		List<string> lines = new List<string>();
		Dictionary<int,string> topics = new Dictionary<int,string>();
		string[] iniLines = File.ReadAllLines(path);
		bool atHeader=false;
		int tmpId=0;
		foreach(string l in iniLines){
			if(!atHeader && l=="["+header+"]")
				atHeader=true;
			else if(atHeader){
				if(l=="")
					break;
				else
				{
					string[] subParts = l.Split('=');
					int id=0;
					//if the part before the = is a number, then we are looking at EdgeCalifia
					//otherwise, it's topic table
					if(int.TryParse(subParts[0].Trim(),out id))
					{//ini topic
						string topic = subParts[1].Trim();
						topics.Add(id,topic);
					}
					else{//topic table topic
						string topic = subParts[0].Trim();
						topics.Add(tmpId,topic);
						tmpId++;
					}
				}
			}
		}
		return topics;
	}

	[MenuItem("Config/Refresh Working Dir")]
	static void RefreshLocalData(){
		System.Diagnostics.Process.Start(Application.streamingAssetsPath+"/Cal3D.exe");
	}

	[MenuItem("Config/Set Server/Remote")]
	static void SetRemoteServer()
	{
		string currentTopicsPath = Application.streamingAssetsPath + "/EdgeCalifia3D.ini";

		//overwrite ini
		INIParser ini = new INIParser();
		ini.Open(currentTopicsPath);
		ini.WriteValue("Base", "Server", "broker.hivemq.com");
		ini.WriteValue("Base", "SharedFolder", EditorUtility.OpenFolderPanel("", "", ""));

		ini.Close();

		// refresh working dir
		RefreshLocalData();
	}

	[MenuItem("Config/Set Server/Local")]
	static void SetLocalServer()
	{
		string currentTopicsPath = Application.streamingAssetsPath + "/EdgeCalifia3D.ini";

		//overwrite ini
		INIParser ini = new INIParser();
		ini.Open(currentTopicsPath);
		ini.WriteValue("Base", "Server", "localhost");
		ini.WriteValue("Base", "SharedFolder", "");
		ini.Close();

		RefreshLocalData();
	}


}

