//RoleManager.cs
//
//Description: to-do description
//


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif
using Crosstales.RTVoice;

public class RoleManager : MonoBehaviour
{
	public Dictionary<int, Role>  _roles = new Dictionary<int,Role>();
	public Dictionary<int,Avatar> _avatars = new Dictionary<int,Avatar>();
	[System.Serializable]
	public struct AvatarPrefab{
		public string _name;
		public Transform _prefab;
	}
	public List<AvatarPrefab> _avatarPrefabs = new List<AvatarPrefab>();
	CanvasGroup _captionGroup;
	Text _captionText;
	float _captionTimer=0;
	AvatarPlacement _ap;
	public delegate void EventHandler();
	public event EventHandler OnRolesChanged;
	public Dictionary<int, string> _voices = new Dictionary<int,string>();
	public Dictionary<int, string> _azureVoices = new Dictionary<int,string>();
	NavPanel _nav;
	public LayerMask _floor;
	public Transform _dialogueOption;
	MyMQTT _mqtt;
	GameObject _injectionSites;
	Role _curRole;
	AzureSpeechService _ass;
	LiveSpeaker _speaker;
	RoomDoors _roomDoors; 
	public GameObject IVBagPrefab;

	public struct Movement{
		public Vector3 _pos;
		public bool _exit;
		public string _onArriveCoroutine;
		public Movement(Vector3 v, bool b, string onArriveCoroutine = null){
			_pos=v;
			_exit=b;
			_onArriveCoroutine = onArriveCoroutine;
		}
	}

	public class Role{
		public string _name;
		public Avatar _av;
		public Transform _transform;
		public NavMeshAgent _nma;
		public NavMeshObstacle _nmo;
		public Animator _anim;
		public Vector3 _target;
		public Movement _targetMovement;
		public RoleManager _rm;
		public ClickDetection _cd;
		public Canvas _can;
		public Transform _ops;
		public Queue<Movement> _moveQueue;
		public bool _turnOnArrival;
		public bool _moving;
		public bool _outOfRoom;
		public RoomConfig.IVBag bagOnHands;
		public GameObject objectOnHands;
		
		public Role(string name, Avatar av,RoleManager rm){
			_name=name;
			_av = av;
			_rm=rm;
			GameObject alreadyInScene = GameObject.Find(name);
			if(alreadyInScene!=null)
				_transform=alreadyInScene.transform;
			_moveQueue = new Queue<Movement>();
		}
		
		public void Speak(string txt, int txtMode, float vol){
			Debug.Log("speaking: "+txt);
			string spch = txt+";en;"+_av._voice+";1;"+_av._pitch+";"+vol;
			if(txtMode%2==1){
				_rm._captionText.text=txt;
				_rm._captionTimer = 1f+txt.Length/12f;
				_rm._captionGroup.alpha=1f;
			}
			switch(txtMode){
				case 0://ignore
				case 1://caption
				default:
					break;
				case 2://voice
				case 3://both
					//_rm._ass.CancelSpeak();
					if(Application.internetReachability == NetworkReachability.NotReachable)
					{
						_rm._speaker.SilenceLive();
						_rm._speaker.SpeakNativeLive(spch);
					}
					else{
#if UNITY_STANDALONE_WIN || UNITY_EDITOR
						//azure voice lib only avail on windahs
						_rm._ass.Speak(txt,_av._azureVoice);
#else
						_rm._speaker.SilenceLive();
						_rm._speaker.SpeakNativeLive(spch);
#endif
					}
					break;
			}
		}

		public void Spawn(Vector3 pos,Quaternion rot){
			if(_av._prefab==null)
			{
				return ;
			}
			Debug.Log("Spawning Role: "+_name);
			_transform = Instantiate(_av._prefab,pos,rot);
			_nma = _transform.GetComponent<NavMeshAgent>();
			_nma.enabled=false;
			_transform.TryGetComponent<NavMeshObstacle>(out _nmo);
			_anim = _transform.GetComponentInChildren<Animator>();
			_cd = _transform.GetComponentInChildren<ClickDetection>();
			_cd._onClick.AddListener(delegate {ClickedOn();});
			_cd._onClick.AddListener(delegate {_rm.RoleClicked(this);});
			_can = _transform.GetComponentInChildren<Canvas>();
			_ops = _can.transform.GetChild(1);
			ClearDialogueOptions();
			switch(_name){
				case "Nurse1":
				case "Nurse2":
					AddDialogueOption("Run Gem3000 Report");
					AddDialogueOption("Run ACT Test");
					AddDialogueOption("Drain Urometer");
					AddDialogueOption("Drain Hemofilter");
					AddDialogueOption("Replace IV Bag");
					AddDialogueOption("Nevermind");
					break;
				default:
					AddDialogueOption("No options for "+_name);
					AddDialogueOption("Nevermind");
					break;
			}
			AddDialogueOption("Close",99);
			Unclick();
		}

		public void UpdateAvatar(Avatar a){
			_av=a;
			if(_transform!=null){
				Vector3 oldPos = _transform.position;
				Quaternion oldRot = _transform.rotation;
				Destroy(_transform.gameObject);
				Spawn(oldPos,oldRot);
				_anim.SetBool("walk",false);
			}
		}

		public void Move(Movement newMovement, bool exit = false){
			//spawn if not spawned yet
			if(_transform==null){
				Vector3 t=GameObject.Find("ExitCam").transform.position;
				_outOfRoom = true;
				Spawn(t,Quaternion.identity);
			}
			//walk to target
			if(_nma==null)
			{
				if(_transform!=null){
					_transform.position= newMovement._pos;
				}
			}
			else {
				if(_moving){
					//enqueue
					_moveQueue.Enqueue(newMovement);
				}
				else{
					_moving = true;
					_targetMovement = newMovement;
					_rm.StartCoroutine(_rm.FaceTarget(this,exit));
				}
			}
		}

		public void Move(Vector3 target, bool exit=false){
			//spawn if not spawned yet
			if(_transform==null){
				Vector3 t=GameObject.Find("ExitCam").transform.position;
				_outOfRoom = true;
				Spawn(t,Quaternion.identity);
			}
			/*
			if(_outOfRoom || exit){
				_rm.AccessToDoors(_transform.gameObject);
			}
			*/
			//walk to target
			if(_nma==null)
			{
				if(_transform!=null){
					_transform.position=target;
				}
			}
			else {
				if(_moving){
					//enqueue
					_moveQueue.Enqueue(new Movement(target,exit));
				}
				else{
				//	_rm.Invoke("EnableAgent", 0.1f);
					_moving = true;
					_target=target;
					_rm.StartCoroutine(_rm.FaceTarget(this,exit));
				}
			}
		}

		public void AddMovement(Movement target){
			if(_moving){
				_moveQueue.Enqueue(target);
			}
		//	else

		}

		public bool Arrived(){
			bool arrived = (_transform.position-_target).sqrMagnitude<.1f;
			if(arrived){
			}
			return arrived;
		}

		public void LookAt(Vector3 pos){
			Vector3 diff = pos-_transform.position;
			diff.y=0;
			_transform.forward=diff;
		}


		public void ClickedOn(){
			if(_moving)
				return;
			_cd.enabled=false;
			_can.enabled=true;
			LookAt(Camera.main.transform.position);
			//get the test cam to look back
			Camera.main.GetComponent<TestCam>().FaceAvatar(_transform.position+Vector3.up*1.6f);
		}

		public void Unclick(){
			_cd.enabled=true;
			_can.enabled=false;
		}

		public void OptionClicked(Transform t){
			int ind = t.GetSiblingIndex();
			switch(_name){
				case "Nurse1":
					switch(ind){
						case 0:
							Debug.Log("Running gem3000");
							_rm._mqtt._nav.NavigateByIndex(0);
							_rm.ShowInjectionSites(true);
							Unclick();
							break;
						case 1:
							_rm._mqtt.RequestACT();
							Unclick();
							break;
						case 2://drain urometer
							_rm.RoleMove(this,18);
							_rm.RoleMove(this,-1);
							_rm.RoleMove(this,4);
							FindObjectOfType<Urometer>().DrainUrine();
							break;
						case 3://drain hemofilter
							_rm.RoleMove(this,18);
							_rm.RoleMove(this,-1);
							_rm.RoleMove(this,4);
							FindObjectOfType<Hemofilter>().DrainCanister();
							break;
						case 4://Replace IV bag
							OffscreenMenuManager.Instance.OpenIVmenu(0, GoForFluidBag);
							break;
						case 5://nevermind
							Unclick();
							_rm.RoleMove(_rm._curRole,-1);
							break;
						default:
							Unclick();
							break;
					}
					break;
				default:
					Unclick();
					_rm.RoleMove(_rm._curRole,-1);
					break;
			}
		}

		public void ClearDialogueOptions(){
			for(int i=_ops.childCount-1; i>=0; i--){
				Destroy(_ops.GetChild(i).gameObject);
			}
		}

		public void AddDialogueOption(string s,int i=-1){
			if(i!=-1)
			{
				Button ex = _can.transform.Find("Exit").GetComponent<Button>();
				ex.onClick.AddListener(delegate {Unclick();});
			}
			else{
				Transform op = Instantiate(_rm._dialogueOption,_ops);
				if(op.GetComponent<Button>()!=null){
					op.GetComponent<Button>().onClick.AddListener(delegate {OptionClicked(op);});
				}
				op.GetChild(1).GetComponent<Text>().text=s;
			}
		}

		public void EnableNavMeshAgent(){
			_nma.enabled = true;
		}

		public void GoForFluidBag(RoomConfig.IVBag bag){
			Debug.Log("Go for the fluid: " + bag.fluid_name);

			bagOnHands = bag;
			_rm.RoleMove(this, -1, "TakeBagFluid");
			_rm.RoleMove(this, 10, "SetIvBag");
		}
	}

	public class Avatar{
		public string _name;
		public Transform _prefab;
		public string _voice;
		public string _azureVoice;
		public string _voiceFull;
		public float _pitch;
		public int _index;

		public Avatar(string name, Transform pref){
			_name=name;
			_prefab=pref;
		}

		public void SetAzureVoice(string name){
			_azureVoice=name;
		}

		public void SetVoice(string voiceFull){
			_voiceFull=voiceFull;
			string [] voiceParts = voiceFull.Split('-');
			_voice = voiceParts[0];
			string voiceMod="";
			if(voiceParts.Length>1)
				voiceMod=voiceParts[1];
			_pitch=1f;
			switch(voiceMod){
				case "A":
					_pitch=0.75f;
					break;
				case "B":
					_pitch=1.2f;
					break;
				case "C":
					_pitch=1.5f;
					break;
				case "D":
					_pitch=2f;
					break;
			}
		}
	}

    // Start is called before the first frame update
    void Start()
    {
		//closed caption elements
		_captionGroup = transform.Find("CaptionCanvas").GetChild(0).GetComponent<CanvasGroup>();
		_captionText=_captionGroup.transform.GetChild(0).GetComponent<Text>();
		SceneManager.sceneLoaded += OnSceneLoaded;
		_ap = FindObjectOfType<AvatarPlacement>();
		_nav = FindObjectOfType<NavPanel>();
		_mqtt = transform.parent.GetComponent<MyMQTT>();
		_ass = FindObjectOfType<AzureSpeechService>();
		_speaker=FindObjectOfType<LiveSpeaker>();
		_roomDoors = FindObjectOfType<RoomDoors>();
    }

	void OnSceneLoaded(Scene scene, LoadSceneMode mode){
		_ap = FindObjectOfType<AvatarPlacement>();
		_nav = FindObjectOfType<NavPanel>();
	}

    // Update is called once per frame
    void Update()
    {
		//closed captions
		if(_captionTimer>0){
			_captionTimer-=Time.deltaTime;
			if(_captionTimer<=0)
				_captionGroup.alpha=0;
		}
    }

	public void Setup(Dictionary<int,string> avs,Dictionary<int,string> voices,
			Dictionary<int,string> azureVoices,Dictionary<int, int> avVoices,
			Dictionary<int, int> aavVoices,Dictionary<int,string> roles,
			Dictionary<int, int> roleAvs){

		//remember voices
		_voices=voices;
		_azureVoices = azureVoices;

		//clear existing data
		_avatars.Clear();
		_roles.Clear();
		//setup avatars
		foreach(int i in avs.Keys){
			string name = avs[i];
			Transform prefab=null;
			foreach(AvatarPrefab ap in _avatarPrefabs){
				if(ap._name==name)
					prefab=ap._prefab;
			}
			_avatars.Add(i,new Avatar(avs[i],prefab));
			_avatars[i].SetVoice(voices[avVoices[i]]);
			_avatars[i].SetAzureVoice(azureVoices[aavVoices[i]]);
		}

		//setup roles
		foreach(int r in roleAvs.Keys){
			_roles.Add(r, new Role(roles[r],_avatars[roleAvs[r]],this));
		}
	}

	public void ChangeLanguage(Dictionary<int,string> avs,
			Dictionary<int,string> azureVoices,Dictionary<int, int> aavVoices){
		foreach(int i in avs.Keys){
			_avatars[i].SetAzureVoice(azureVoices[aavVoices[i]]);
		}
	}

	//
	public void RoleAction(int roleIndex, string roleName,int locationIndex, string text, int textMode, int volume,bool turnAndSpeak=false){
		if(roleName!=null && roleName!=""){
			roleIndex = GetRoleIndex(roleName);
		}
		//tts
		//don't speak if in opening scene
		if(SceneManager.GetActiveScene().buildIndex>1)
		{
			if(text!=""){
				if(_roles.ContainsKey(roleIndex))
					_roles[roleIndex].Speak(text,textMode,((float)volume)*.01f);
				else
					Debug.Log("oops no role for index: "+roleIndex);
				switch(textMode){
					case 0://ignore
					case 2://voice
					default:
						break;
					case 1://caption
					case 3://both
						_captionText.text=text;
						_captionTimer = 1f+text.Length/12f;
						_captionGroup.alpha=1f;
						break;
				}
			}
		}
		else{
			Debug.Log("Oops not in a sim scene");
		}
		_roles[roleIndex]._turnOnArrival=true;
		RoleMove(_roles[roleIndex],locationIndex);
	}
	public void RoleMove(Role r ,int locationIndex, string onArriveAction = null){
		NavPanel.NavOption no = _nav.GetOptionByIndex(locationIndex);
		Vector3 target;
		bool exit=false;
		if(r._nmo!=null)
			r._nmo.enabled=false;
		if(no==null)
		{
			//gotta clean this up 
			target=GameObject.Find("ExitCam").transform.position;
			exit=true;
		}
		else
			target=no._position;
		//find nearest floor
		RaycastHit hit;
		Movement newMovement;
		if(onArriveAction == null)
			newMovement = new Movement(target, exit);
		else
			newMovement = new Movement(target, exit, onArriveAction);

		if(Physics.Raycast(target,Vector3.down,out hit,10f,_floor))
		{
			target.y=hit.point.y;
			//not sure why this check was done?
			//probably some special case I forgot about
			if(target.x>-500f){
				r.Move(newMovement,exit);

			}
		}
	}

	IEnumerator FaceTarget(Role role,bool exit=false){
		role.Unclick();
		bool stuck=false;
		Vector3 prevPos = role._transform.position;
		float stuckTimer=0;
		yield return new WaitForSeconds(1f);
		role._nma.enabled = true;
		role._nma.SetDestination(role._targetMovement._pos);
		role._anim.SetBool("walk",true);

		//avoid overlap with player on avatar spawns
		if(role._outOfRoom){
			float playerDistance = (Camera.main.GetComponent<TestCam>().transform.position - role._targetMovement._pos).sqrMagnitude;
			if(playerDistance < 3.6f)
				role._nma.stoppingDistance = role._nma.stoppingDistance + 2.5f;
		}
		else{
			role._nma.stoppingDistance = 0f;
		}


		while(!role.Arrived() && !stuck)
		{
			//role._transform.forward=role._transform.position-prevPos;
			yield return null;
			if(prevPos==role._transform.position){
				stuckTimer+=Time.deltaTime;
				if(stuckTimer>0.1f)
					stuck=true;
			}
			else{
				stuckTimer=0;
				prevPos=role._transform.position;
			}
			role.LookAt(role._targetMovement._pos);
		}

		//if this movement has an action on arrive wait until the action is completed
		if(role._targetMovement._onArriveCoroutine != null){
			yield return StartCoroutine(role._targetMovement._onArriveCoroutine);
		}

		role._moving = false;
		if(role._moveQueue.Count>0){
			Movement m = role._moveQueue.Dequeue();
			role.Move(m,m._exit);
		}
		else
		{
			role._anim.SetBool("walk",false);
			role._nma.enabled=false;
			if(!exit)
			{
				if(role._nmo != null){
					role._nmo.enabled = true;
				}
				role._outOfRoom = false;
				role._cd._onClick.Invoke();
				role.Speak("How can I help you?",FindObjectOfType<ScenarioManager>().ttsMode,0.3f);
			}
			else
				role._outOfRoom = true;
		}
	}

	public string GetAvatarFromRole(string role){
		Avatar av=null;
		foreach(Role r in _roles.Values){
			if(r._name==role)
				av=r._av;
		}
		if(av==null)
			return "";
		else
			return av._name;
	}

	public string GetVoiceFromAvatar(string avatar){
		foreach(Avatar a in _avatars.Values){
			if(a._name==avatar)
				return a._voiceFull;
		}
		return "";
	}

	public void SaveAssignment(string role, string avatar, string voiceFull){
		//save avatar
		foreach(Role r in _roles.Values){
			if(r._name==role){
				foreach(Avatar av in _avatars.Values){
					if(av._name==avatar){
						//assign voice to avatar
						av.SetVoice(voiceFull);
						//assign avatar to role
						r.UpdateAvatar(av);
						//check if the old avatar was 
					}
				}
			}
		}
		OnRolesChanged.Invoke();
		UpdateAvatars();
	}

	public string SerializeAvatarVoices(){
		string ser="";
		foreach(int k in _avatars.Keys){
			string voice = _avatars[k]._voiceFull;
			int voiceCode=0;
			foreach(int v in _voices.Keys){
				if(_voices[v]==voice)
					voiceCode=v;
			}
			ser+=k+" = "+voiceCode+System.Environment.NewLine;
		}
		ser+=System.Environment.NewLine;
		return ser;
	}

	public string SerializeRoleAvatars(){
		string ser="";
		foreach(int k in _roles.Keys){
			string name = _roles[k]._av._name;
			int avIndex=0;
			foreach(int a in _avatars.Keys){
				if(_avatars[a]._name==name)
					avIndex=a;
			}
			ser+=k+" = "+avIndex+System.Environment.NewLine;
		}
		ser+=System.Environment.NewLine;
		return ser;
	}

	public void UpdateAvatars(){

	}

	public void RoleClicked(Role r){
		_curRole=r;
	}

	public void ShowInjectionSites(bool b){
		if(_injectionSites==null)
		{
			_injectionSites=GameObject.Find("InjectionSites");
			foreach(Transform t in _injectionSites.transform){
				Button butt = t.GetComponentInChildren<Button>();
				butt.onClick.AddListener(delegate {ClickedInjectionSite(butt.transform);});
			}
		}
		foreach(Transform t in _injectionSites.transform){
			t.gameObject.SetActive(b);
		}
		Camera.main.transform.Find("Blocker").GetComponent<BoxCollider>().enabled=true;
	}

	public void ClickedInjectionSite(Transform t){
		int ind =0;
		int.TryParse(t.name.Split('-')[1],out ind);
		ShowInjectionSites(false);
		//RoleMove(_curRole,15);
		RoleMove(_curRole,-1);
		//RoleMove(_curRole,0);
		_mqtt.RequestGem3000(ind);
		Camera.main.transform.Find("Blocker").GetComponent<BoxCollider>().enabled=false;
	}

	public int GetRoleIndex(string name){
		foreach(int k in _roles.Keys){
			if(_roles[k]._name==name)
				return k;
		}
		return -1;
	}

	private void AccessToDoors(GameObject gameObject){
		//doors open automatically
		//_roomDoors.RequestUseDoor(gameObject);
	}

	public IEnumerator TakeBagFluid(){
		//Find right hand transform
		Transform rightHand = _curRole._transform;
		foreach(Transform child in _curRole._transform.GetComponentsInChildren<Transform>())
 		{	
			 if(child.name == "mixamorig:RightHand"){
				 rightHand = child;
		 	}
 		}
		//Instantiate the bag and attach it to the avatar right hand
		GameObject ivBag = Instantiate(IVBagPrefab, rightHand);
		ivBag.transform.localPosition = new Vector3(0,0,0);

		//displaying target bag
		LiquidHelper bagLiquid = ivBag.GetComponent<LiquidHelper>();
		bagLiquid.Initialize();
		ivBag.GetComponent<LiquidHelper>().SetBag(_curRole.bagOnHands);
		_curRole.objectOnHands = ivBag;

		yield return null;
	}

	public IEnumerator SetIvBag(){
		Alaris alaris = FindObjectOfType<Alaris>();
		_curRole.LookAt(alaris.transform.position);

		//Start animation
		_curRole._anim.SetBool("walk",false);
		yield return new WaitForSeconds(0.1f);
		_curRole._anim.SetLayerWeight(1, 100f);
		_curRole._anim.SetTrigger("handGrab");
		yield return new WaitForSeconds(1.75f);
		
		//Find the first free channel, in case there is no free channel replace the first one
		int targetChannel = alaris.GetFreeChannelFluid();
		if(targetChannel == -1)
			targetChannel = 0;

		//Replace bags
		alaris.SetChannelFluid(targetChannel ,_curRole.bagOnHands);
		Destroy(_curRole.objectOnHands);
		_curRole.bagOnHands = null;
		yield return new WaitForSeconds(3f);
		_curRole._anim.SetLayerWeight(1, 0f);
	}
}
