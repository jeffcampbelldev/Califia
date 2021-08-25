using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

public class ClipboardHelper : MonoBehaviour
{
	Clipboard[] _clips;
	public int _numTabs;
	MyMQTT _mqtt;
	[HideInInspector]
	public Clipboard _curClip;
	[HideInInspector]
	public int _lastTab=-1;
	[System.Serializable]
	public class Tab{
		public string _dir;
		public Dictionary<string,System.DateTime> _pathTimes;
		public List<string> _path;
		public int _tab;
		public string _label;
		public string _col;
		public int _status;
		public int _formIndex;
		
		public Tab(int tab){
			_pathTimes = new Dictionary<string,System.DateTime>();
			_path = new List<string>();
			_tab=tab;
			_label="---";
			_col="00000000";
			_status=0;
			_formIndex=0;
		}

		public void Update(string path, string label, string color, int status,ClipboardHelper ch,bool render){
			Debug.Log("updating tab - render: "+render);
			_status=status;
			_col=color;
			_label=label;
			if(_status==1)
			{
				_dir = Path.GetDirectoryName(path);
				Reload(path.Replace('\\','/'),ch,render);
			}
			else{
				if(render)
				{
					if(ch._curClip!=null)
					{
						ch._curClip.ClearTex();
					}
				}
			}
		}

		public void Setup(Transform t){
			RawImage img = t.GetComponent<RawImage>();
			Text txt = t.GetChild(0).GetComponent<Text>();
			Button b = t.GetComponent<Button>();
			if(_status==1)
			{
				b.interactable=true;
				string colHex = "#"+_col;
				Color color;
				if(ColorUtility.TryParseHtmlString(colHex, out color))
					img.color=color;
				txt.text=_label;
			}
			else{
				b.interactable=false;
				img.color = new Color(0,0,0,0);
				txt.text="---";
			}
		}

		public void Render(ClipboardHelper ch,Clipboard cb){
			if(_path.Count>0 && _status==1){
				Debug.Log("form index: "+_formIndex);
				Debug.Log("Out of forms: "+_path.Count);
				if(_formIndex>=_path.Count)
					return;
				string path = _path[_formIndex];
				string ext = Path.GetExtension(path);
				//Debug.Log("Loading form with extension: "+ext);
				Debug.Log("Loading image from Render method");
				ch.StartCoroutine(ch.LoadImageR(_path[_formIndex],this,new System.DateTime(0),cb));
			}
		}

		public void SwipeForm(int dir){
			_formIndex+=dir;
			if(_formIndex>=_path.Count)
				_formIndex=0;
			else if(_formIndex<0)
				_formIndex=_path.Count-1;
		}

		public void SetForm(int index){
			_formIndex=index;
		}

		public void Reload(string latest,ClipboardHelper ch, bool render){
			//set form index
			Debug.Log("Reloading");
			if(!_path.Contains(latest))
			{
				_path.Add(latest);
				_formIndex=_path.Count-1;
				Debug.Log("path does not contain: "+latest);
			}
			else{
				_formIndex=_path.IndexOf(latest);
				Debug.Log("path does contain: "+latest);
			}
			Debug.Log("Tab index: "+_tab);
			Debug.Log("Form index: "+_formIndex);
			Debug.Log("Path size: "+_path.Count);
			//render latest
			Debug.Log("Loading image from reload");
			if(_pathTimes.ContainsKey(latest)) //if prev version loaded, pass prev write time
			{
				Debug.Log("Already have previous version");
				ch.StartCoroutine(ch.LoadImageR(latest,this,_pathTimes[latest],render));
			}
			else
			{
				//Debug.Log("Somehow didn't save latest time");
				ch.StartCoroutine(ch.LoadImageR(latest,this,new System.DateTime(0),render));
			}
			//set active tab
			if(render && ch._curClip!=null)
			{
				ch._curClip._lastTab=_tab;
			}
		}

		public void SaveWriteTime(System.DateTime timestamp, string f){
			if(_pathTimes.ContainsKey(f)){
				Debug.Log("Overwriting write time: "+timestamp);
				_pathTimes[f]=timestamp;
			}
			else{
				Debug.Log("Saving new write time: "+timestamp);
				_pathTimes.Add(f,timestamp);
			}
		}

		public void FillDropdown(Dropdown d){
			d.gameObject.SetActive(true);
			d.options.Clear();
			foreach(string p in _path){
				Dropdown.OptionData od = new Dropdown.OptionData();
				od.text=Path.GetFileName(p).Split('.')[0];
				d.options.Add(od);
			}
		}

		public void ClearDropdown(Dropdown d){
			d.gameObject.SetActive(false);
		}

		public void SetDropdown(Dropdown d){
			d.value=_formIndex;
		}
	}

	public Dictionary<int,Tab> _tabs = new Dictionary<int,Tab>();

    // Start is called before the first frame update
    void Start()
    {
		//why do we add this temp tab at the beginning? 
		/*
		for(int i=0; i<_numTabs; i++){
			Tab tmp = new Tab();
			_tabs.Add(tmp);
		}
		*/
    }


	public void UpdateForm(string path, int tab, string label, string col, int status,bool tabChange,MyMQTT m){
		Debug.Log("Updating form tab: "+tab);
		_mqtt=m;
		string fullPath = _mqtt._sharedFolder+"/"+path.Replace('\\','/');
		bool redraw = tabChange || _lastTab==tab || _lastTab==-1;
		if(redraw && status==1)
			_lastTab=tab;

		//check if tab exists or if new tab is to be created
		if(_tabs.ContainsKey(tab))
			_tabs[tab].Update(fullPath,label,col,status,this,redraw);
		else
		{
			Tab t = new Tab(tab);
			_tabs[tab]=t;
			_tabs[tab].Update(fullPath,label,col,status,this,redraw);
		}

		//actually update the tabs on the clipboard model and UI
		_clips = FindObjectsOfType<Clipboard>();
		foreach(Clipboard c in _clips)
		{
			c.UpdateTabs();
			if(redraw)
				c._lastTab=tab;
		}
	}	

	public void UpdateTab(Transform t, int tabIndex){
		if(_tabs.ContainsKey(tabIndex))
			_tabs[tabIndex].Setup(t);
	}

	public void RenderTab(int tabIndex,Clipboard cb){
		_tabs[tabIndex].Render(this,cb);
	}

	public void SwipeForm(int tabIndex, int dir){
		if(_tabs.ContainsKey(tabIndex))
			_tabs[tabIndex].SwipeForm(dir);
	}

	public void SetForm(int tabIndex, int index){
		if(_tabs.ContainsKey(tabIndex))
			_tabs[tabIndex].SetForm(index);
	}

	IEnumerator LoadImageR(string path,Tab t, System.DateTime lastWrite, bool render=true){
		float timer=0;
		float maxTime=10f;
		float checkPeriod=0.5f;
		while(!File.Exists(path)){
			Debug.Log("Waiting for clipboard file: "+path);
			yield return new WaitForSeconds(checkPeriod);
			timer+=checkPeriod;
			if(timer>maxTime)
				break;
		}
		//if we have a real path
		if(timer<=maxTime){
			timer=0;
			while(lastWrite.Ticks>0 && File.GetLastWriteTime(path)==lastWrite){
				Debug.Log("clipboard file exists but is out of date: "+path);
				//Debug.Log("last write: "+lastWrite);
				//Debug.Log("getlastwriteTime: "+File.GetLastWriteTime(path));
				yield return new WaitForSeconds(.5f);
				timer+=checkPeriod;
				if(timer>maxTime)
					break;
			}
			//if we have a relevant timestamp
			if(timer<=maxTime){

				if(render){
					using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(path))
					{
						yield return uwr.SendWebRequest();

						if (uwr.isNetworkError || uwr.isHttpError)
						{
							Debug.Log(uwr.error);
						}
						else
						{
							if(_curClip!=null)
							{
								var texture = DownloadHandlerTexture.GetContent(uwr);
								_curClip.SetTex(texture);
							}
						}
					}
				}
				//refresh list of files
				//t.SaveWriteTime(File.GetLastWriteTime(path),path);
				string[] files = Directory.GetFiles(t._dir);
				t._path.Clear();
				for(int i=0; i<files.Length; i++){
					string f = files[i].Replace('\\','/');
					t._path.Add(f);
					t.SaveWriteTime(File.GetLastWriteTime(files[i]),f);
				}
				if(render && _curClip!=null)
				{
					bool arrows = t._path.Count>1;
					_curClip._next.interactable=arrows;
					_curClip._prev.interactable=arrows;
					if(arrows)
					{
						t.FillDropdown(_curClip._dropdown);
						t.SetDropdown(_curClip._dropdown);
					}
					else
						t.ClearDropdown(_curClip._dropdown);
				}
			}
		}
	}
}
