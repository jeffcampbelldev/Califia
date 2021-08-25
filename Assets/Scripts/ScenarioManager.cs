//ScenarioManager.cs
//
//Description: Manages interaction with scenario panel.
//Loads scenario files, sets up steps, and manages timing of steps
//Reads scenario file parameters and triggers events accordingly
//


using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ScenarioManager : MonoBehaviour
{
	//scenario file parameter name to index / order of appearacne
	Dictionary<string,int> _stepKeys = new Dictionary<string,int>();
	//[scenario] header pulled from TopicTable.ini
	Dictionary<string,string> _stepTopics = new Dictionary<string,string>();

	public int _curStep;
	public Text _fn;
	public struct Step {
		public int _index;
		public float _dur;
		public float _endTime;
		public Transform _marker;
		public RawImage _img;
		public string[] _params;

		public void Init(){
			_img = _marker.GetComponent<RawImage>();
		}
	}
	Step [] _steps;
	public RectTransform _timeline;
	Image _progress;
	public Transform _stepIcon;

	//timing, scaling, ui
	float _minNorm=0;
	float _maxNorm=1f;
	float _totalTime=0f;
	float _totalTimeActual=0f;
	float _curTime=0f;
	float _nulTime=1f;
	public Color _activeCol, _inactiveCol;
	public Slider _zoom;
	public Scrollbar _scroll;
	MyMQTT _mqtt;
	public Text _curTimeText;
	public Text _totalTimeText;
	public Button _playButton;
	RawImage _playImage;
	public Texture2D _playTex,_pauseTex,_reloadTex;
	bool _playing;
	ConfirmMenu _confirm;
	public Text _statusText;
	RoomConfig _room;
	[HideInInspector]
	public int ttsMode=3;
	int _prevStep=-1;
	public Text _stepNum;

	//Coroutine management
	IEnumerator _loadScenarioRoutine;
	IEnumerator _loadCatRoutine;
	IEnumerator _commandRoutine;
	IEnumerator _stepRoutine;

	NavPanel _nav;

	InfoBubble _bubble;

    // Start is called before the first frame update
    void Start()
    {
		//init mqtt
		MyMQTT [] qts = FindObjectsOfType<MyMQTT>();
		foreach(MyMQTT qt in qts){
			if(qt.gameObject.tag=="GameController")
				_mqtt=qt;
		}
		//get topic - scenario param pairs
		string topicTablePath = _mqtt.GetWorkPath("TopicTable.ini");
		_stepTopics = IniHelper.GetStringDict(topicTablePath,"Scenario");

		//ui init
		_playImage = _playButton.GetComponent<RawImage>();
		_playing=false;
		_progress = _timeline.GetComponentInChildren<Image>();
		_scroll.handleRect=_scroll.transform.GetChild(0).GetChild(0).GetComponent<RectTransform>();
		_confirm=FindObjectOfType<ConfirmMenu>();

		_nav = FindObjectOfType<NavPanel>();
		//_bubble = FindObjectOfType<InfoBubble>();
		SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

	void OnSceneUnloaded(Scene scene){
		//play first step if exists
		//PlayPause();
		Play(1);
	}



	//called by mqtt - sometimes scenario file topics are received before we have access to the file
	//in the case of remote setups
	public void LoadScenario(string fn, int step,string cat){
		StopAllCoroutines();
		StartCoroutine(LoadScenarioR(fn,step,cat));
	}

	//Tries to load scenario with timeout
	IEnumerator LoadScenarioR(string fn, int step,string cat){
		yield return null;
		_fn.text=Path.GetFileName(fn);
		string path = _mqtt._sharedFolder+"/"+fn;
		int tries=0;
		int maxTries=10;
		while(!File.Exists(path)){
			Debug.Log("Couldn't find path: "+path);
			yield return new WaitForSeconds(0.5f);
			tries++;
			if(tries>maxTries)
				break;
		}
		if(tries<=maxTries){
			//read scenario catalog - used for QuickNav options
			if(cat!="")
				StartCoroutine(LoadCatR(cat));
			//read scenario file - used for scenario steps
			yield return new WaitForSeconds(0.5f);
			string [] lines = File.ReadAllLines(path);

			//get keys
			_stepKeys.Clear();
			string [] keys = lines[0].Split('\t');
			for(int i=0; i<keys.Length; i++){
				_stepKeys.Add(keys[i],i);
			}

			//remove any old step transforms
			for(int i=_timeline.transform.childCount-1; i>=0; i--){
				if(_timeline.transform.GetChild(i).GetComponent<Image>()==null)
					Destroy(_timeline.transform.GetChild(i).gameObject);
			}

			//load in new steps
			_totalTime=0;
			_steps = new Step[lines.Length-1];
			for(int i=1; i<lines.Length; i++){
				string [] parts = lines[i].Split('\t');
				Step s = new Step();
				float f=0;
				float.TryParse(parts[1],out f);
				s._dur=f==-1? 0f : f;
				_totalTime+=f==-1? 2f : f;
				_totalTimeActual+=f==-1? 0f : f;
				s._endTime=_totalTimeActual;
				s._marker=Instantiate(_stepIcon,_timeline);
				Button b = s._marker.GetComponent<Button>();
				b.onClick.AddListener(delegate {StepClick(s);});
				Text t = s._marker.GetChild(0).GetComponent<Text>();
				t.text=i+" - "+parts[3];
				t.enabled=false;

				//get all the step params
				s._params = new string[parts.Length];
				for(int j=0; j<parts.Length; j++){
					s._params[j]=parts[j];
				}
				s.Init();
				s._index=i-1;
				_steps[s._index]=s;
			}

			//set up at first step (we index by 0, but scenario is indexed by 1
			ZoomTimeline(_zoom);
			_curStep=step-1;
			Play(1);
			int mins = Mathf.FloorToInt(_totalTimeActual/60);
			int secs = Mathf.FloorToInt(_totalTimeActual%60);
			_totalTimeText.text = "/ "+mins.ToString("0")+":"+secs.ToString("00");
		}
	}

	IEnumerator LoadCatR(string cat){
		yield return null;
		string path = _mqtt._sharedFolder+"/"+cat;
		int tries=0;
		int maxTries=10;
		while(!File.Exists(path)){
			Debug.Log("Couldn't find path: "+path);
			yield return new WaitForSeconds(0.5f);
			tries++;
			if(tries>maxTries)
				break;
		}
		if(tries<=maxTries){
			//read file
			yield return new WaitForSeconds(0.5f);
			//use the views from this cat to append to the "master" list
			_mqtt._room.LoadScenarioCat(path);
			_mqtt._scenCatalogPath=path;
		}
		else{

		}
	}

	public void NextStep(){
		_curStep++;
		if(_curStep>=_steps.Length)
			_curStep=_steps.Length-1;
		else
		{
			Play(1,true);
		}
	}

	public void PrevStep(){
		_curStep--;
		if(_curStep<0)
			_curStep=0;

		Play(1,true);
	}

	void RepositionSteps(){
		if(_steps==null)
			return;
		float tt=0;
		foreach(Step s in _steps){
			float normPos = tt/_totalTime; //0 = start of scenario, 1 = end

			//interpolate between steps visible within timeline
			normPos = Mathf.InverseLerp(_minNorm,_maxNorm,normPos);
			tt+=s._dur==0? _nulTime : s._dur;
			s._marker.localPosition=Vector3.right*Mathf.Lerp(-_timeline.sizeDelta.x*.5f,
					_timeline.sizeDelta.x*.5f,normPos);
			s._marker.localPosition+=Vector3.up*20f;
		}
	}

	//starts timer if needed and loads params listed in [scenario] of TopicTable.ini
	//mode = 0 = pause
	//mode = 1 = play
	//pub = whether or not to publish mqtt event on load 
	void LoadStep(){
		bool reachedStep=false;
		_curTime = 0f;
		for(int i=0; i<_steps.Length; i++)
		{
			if(!reachedStep){
				
				if(i==_curStep){
					reachedStep=true;
					int mins = Mathf.FloorToInt(_curTime/60);
					int secs = Mathf.FloorToInt(_curTime%60);
					_curTimeText.text = mins+":"+secs.ToString("00");
					//load step parameters
					foreach(string stepParam in _stepTopics.Keys){
						MyMQTT.StringByteArr sba = new MyMQTT.StringByteArr();
						sba._str=_mqtt._prefix+_stepTopics[stepParam];
						if(!_stepKeys.ContainsKey(stepParam))
							continue;
						string paramVal = _steps[_curStep]._params[_stepKeys[stepParam]];
						float fVal=-1;
						if(float.TryParse(paramVal, out fVal)){
							//got a float - base case
							sba._bytes=System.BitConverter.GetBytes(fVal);
						}
						else{
							fVal=-1f;
							//Special cases for strings that don't correspond 1-1 for the mqtt 
							//equivalent
							if(stepParam=="Character"){
								Debug.Log("handling tts command");
								string role = _steps[_curStep]._params[_stepKeys[stepParam]];
								string txt = _steps[_curStep]._params[_stepKeys["Text to Speech"]];
								Debug.Log("txt: "+txt);
								MyMQTT.RoleData rd = new MyMQTT.RoleData();
								rd.role_name=role;
								rd.tts=txt;
								rd.tts_mode=ttsMode;
								rd.volume=30;
								string rds = JsonUtility.ToJson(rd);
								sba._bytes = Encoding.UTF8.GetBytes(rds);
								//set this so the message is sent
								fVal=0f;
							}
							else if(stepParam=="Gender"){
								if(paramVal=="Female")
									fVal=1f;
								else if(paramVal=="Male")
									fVal=0f;
								sba._bytes=System.BitConverter.GetBytes(fVal);
							}
							else if(stepParam=="Navigation"){
								int navIndex = _nav.GetIndexFromNavName(paramVal);
								Debug.Log("Reading nav command: "+paramVal + " index: "+navIndex);
								fVal=(float)navIndex;
								sba._bytes=System.BitConverter.GetBytes(fVal);
							}
						}
						if(fVal!=-1)
						{
							Debug.Log("Enqueueing command");
							StartCoroutine(EnqueueCommandR(sba));
						}
					}
					//set status bar
					_statusText.text="Step "+(_curStep+1)+" - "+_steps[_curStep]._params[_stepKeys["Surgical Events"]];
				}
				else{
					_curTime+=_steps[i]._dur;
				}
			}
			_steps[i]._img.color=i==_curStep? _activeCol : _inactiveCol;
		}
	}

	//Used for handling scenario file parameters
	//Sends params as mqtt events to MyMQTT
	//_commandLock is on while MyMQTT is iterating over command buffer
	IEnumerator EnqueueCommandR(MyMQTT.StringByteArr sba){
		while(_mqtt._commandLock){
			yield return null;
		}
		_mqtt._commands.Add(sba);
	}

	//handler to the zoom slider
	public void ZoomTimeline(Slider s){
		_maxNorm=Mathf.Lerp(1f,_curTime/_totalTime,s.value);
		_minNorm=Mathf.Lerp(0f,_curTime/_totalTime,s.value);
		RepositionSteps();
		_scroll.size=_maxNorm-_minNorm;
		_scroll.value = (_maxNorm+_minNorm)*.5f;
		float minT=Mathf.Lerp(0,_totalTime,_minNorm);
		float maxT=Mathf.Lerp(0,_totalTime,_maxNorm);
		_progress.fillAmount = Mathf.InverseLerp(minT,maxT,_curTime);
	}

	//handler for the scrollbar - pans the timeline
	public void ScrollTimeline(Scrollbar s){
		float delta = .5f*(_maxNorm-_minNorm);
		float minAvg = delta;
		float maxAvg = 1f-delta;
		float normAvg = Mathf.Lerp(minAvg,maxAvg,s.value);
		_minNorm=normAvg-delta;
		_maxNorm=normAvg+delta;
		RepositionSteps();
		float minT=Mathf.Lerp(0,_totalTime,_minNorm);
		float maxT=Mathf.Lerp(0,_totalTime,_maxNorm);
		_progress.fillAmount = Mathf.InverseLerp(minT,maxT,_curTime);
	}

	//Used for play, pause, and reload - all in the same button
	public void PlayPause(){
		if(_steps==null || _steps.Length<=0)
			return;
		if(_steps[_curStep]._dur<=0){
			Play(1,true);
		}
		else{
			if(_playing){
				Play(0,true);
			}
			else{
				Play(1,true);
			}
		}
	}

	//Running timer for step of non-negative duration
	IEnumerator StepTimerR(){
		_playing=true;
		float endTime = _steps[_curStep]._endTime;
		yield return null;
		float minT;
		float maxT;
		while(_curTime<endTime){
			_curTime+=Time.deltaTime;
			int mins = Mathf.FloorToInt(_curTime/60);
			int secs = Mathf.FloorToInt(_curTime%60);
			_curTimeText.text = mins+":"+secs.ToString("00");
			minT=Mathf.Lerp(0,_totalTime,_minNorm);
			maxT=Mathf.Lerp(0,_totalTime,_maxNorm);
			_progress.fillAmount = Mathf.InverseLerp(minT,maxT,_curTime);
			yield return null;
		}
		_playing=false;
		NextStep();
	}

	public void ResetScenario(){
		_confirm.ConfirmRequest("Are you sure you'd like to reload scenario? Any changes made to the scene will be undone.");
		_confirm._confirm.onClick.AddListener(delegate {Reload();});
	}

	//handler to the little step buttons on the timeline
	public void StepClick(Step s){
		Debug.Log("Confirm clicked step: "+s._index);
		_confirm.ConfirmRequest("Are you sure you'd like to jump to step "+(s._index+1)+" - "+s._params[3]+"?");
		_confirm._confirm.onClick.AddListener(delegate {JumpStep(s);});
	}

	public void JumpStep(Step s){
		_curStep=s._index;
		Play(1,true);
	}

	public void Reload(){
		_curStep=0;
		Play(2,true);
	}

	//key entry point to scenario control
	public void Play(int mode,bool pub=false){
		if(SceneManager.GetActiveScene().buildIndex<=1 || this==null || _steps==null || _curStep>=_steps.Length){
			return;
		}
		if(mode==1){
			StartStep();
		}
		else if(mode==0){
			Pause();
			if(_prevStep!=_curStep)
				LoadStep();
		}
		else if(mode==2){
			_curStep=0;
			StartStep();
		}
		if(pub)
			_mqtt.Play(mode,_curStep+1);
		float minT=Mathf.Lerp(0,_totalTime,_minNorm);
		float maxT=Mathf.Lerp(0,_totalTime,_maxNorm);
		_progress.fillAmount = Mathf.InverseLerp(minT,maxT,_curTime);
		_prevStep=_curStep;
		_stepNum.text="Step: "+(_curStep+1);
	}

	void StartStep(){
		if(_steps[_curStep]._dur>0){
			if(_stepRoutine!=null)
				StopCoroutine(_stepRoutine);
			_stepRoutine = StepTimerR();
			StartCoroutine(_stepRoutine);
			_playImage.texture=_pauseTex;
			if(_prevStep!=_curStep)
				LoadStep();
		}
		else{
			if(_stepRoutine!=null)
				StopCoroutine(_stepRoutine);
			_playButton.interactable=true;
			_playImage.texture=_reloadTex;
			LoadStep();
		}
	}

	public void Pause(){
		_playing=false;
		if(_stepRoutine!=null)
			StopCoroutine(_stepRoutine);
		_playImage.texture=_playTex;
		float minT=Mathf.Lerp(0,_totalTime,_minNorm);
		float maxT=Mathf.Lerp(0,_totalTime,_maxNorm);
		_progress.fillAmount = Mathf.InverseLerp(minT,maxT,_curTime);
	}
}
