//Use this to spawn and control npc's
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

public class NpcManager : MonoBehaviour
{
	//public Npc [] _npc;
	
	public Role [] _roles;

	public class Role{
		public string _name;
		public Avatar _av;
	}

	public class Avatar{
		public string _name;
		public Transform _prefab;
		public string _voice;
		public float _pitch;
	}

	/*
	[System.Serializable]
	public class Npc{
		public Transform _prefab;
		[HideInInspector]
		public Transform _transform;
		[HideInInspector]
		public int _location=-1;
		public string _name;
		[HideInInspector]
		public NavMeshAgent _nma;
		[HideInInspector]
		public Vector3 _target;
		[HideInInspector]
		public Animator _anim;
		[HideInInspector]
		public string _voice;

		public void Reset(){
			_transform=null;
			_location=-1;
		}

		public void Spawn(){
			Debug.Log("spawning");
			Vector3 pos = FindObjectOfType<NpcNavHelper>()._entrance;
			_transform = Instantiate(_prefab,pos,Quaternion.identity);
			_nma = _transform.GetComponent<NavMeshAgent>();
			_anim = _transform.GetComponentInChildren<Animator>();
		}
		public void GoTo(int locIndex){
			NpcNavHelper nnh = FindObjectOfType<NpcNavHelper>();
			Vector3 loc;
			if(locIndex>0)
				loc = nnh._locs[locIndex-1]._pos;
			else
				loc = nnh._entrance;
			_target=loc;
			_location=locIndex;
			_nma.enabled=true;
			_nma.SetDestination(loc);
			_anim.SetBool("walk",true);
		}

		public bool Arrived(){
			return (_transform.position-_target).sqrMagnitude<.05f;
		}
		public void LookAt(Vector3 pos){
			_nma.enabled=false;
			Vector3 diff = pos-_transform.position;
			diff.y=0;


			//_transform.localEulerAngles = Vector3.up*_transform.localEulerAngles.y;
			_transform.forward=diff;
			_anim.SetBool("walk",false);
		}
	}
	*/

	LiveSpeaker _speaker;
	CanvasGroup _captionGroup;
	Text _captionText;
	float _captionTimer=0;

    // Start is called before the first frame update
    void Start()
    {
		//closed caption elements
		_captionGroup = transform.Find("CaptionCanvas").GetChild(0).GetComponent<CanvasGroup>();
		_captionText=_captionGroup.transform.GetChild(0).GetComponent<Text>();
		SceneManager.sceneLoaded += OnSceneLoaded;
    }

	void OnSceneLoaded(Scene scene, LoadSceneMode mode){
		/*
		foreach(Npc npc in _npc){
			npc.Reset();
		}
		*/
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

	public void SetVoice(string avName, string avVoice){
		/*
		foreach(Npc npc in _npc){
			if(npc._name==avName)
				npc._voice=avVoice;
		}
		*/
	}

	public void AvatarAction(int roleIndex ,int locationIndex, string text, int textMode, int volume){
		//tts
		if(text!=""){
			//get avatar
			//get voice
			//get pitch
		}
		//movement
		/*
		if(locationIndex>=0 && locationIndex!=_npc[avatarIndex]._location && _npc[avatarIndex]._prefab!=null){
			if(_npc[avatarIndex]._transform==null)
				_npc[avatarIndex].Spawn();
			_npc[avatarIndex].GoTo(locationIndex);
			StartCoroutine(FaceTarget(_npc[avatarIndex]));
		}

		//tts
		if(text=="")
			return;
		string [] voiceParts = _npc[avatarIndex]._voice.Split('-');
		string voice = voiceParts[0];
		string voiceMod="";
		if(voiceParts.Length>1)
			voiceMod=voiceParts[1];
		float pitch=1;
		switch(voiceMod){
			case "A":
				pitch=0.75f;
				break;
			case "B":
				pitch=1.2f;
				break;
			case "C":
				pitch=1.5f;
				break;
			case "D":
				pitch=2f;
				break;
		}
		float vol = ((float)volume)*.01f;
		string spch = text+";en;"+voice+";1;"+pitch+";"+vol;
		if(_speaker==null)
			_speaker=FindObjectOfType<LiveSpeaker>();
		switch(textMode){
			case 0://ignore
			default:
				break;
			case 1://caption
				_captionText.text=text;
				_captionTimer = 1f+text.Length/12f;
				_captionGroup.alpha=1f;
				break;
			case 2://voice
				_speaker.SpeakNativeLive(spch);
				break;
			case 3://both
				_captionText.text=text;
				_captionTimer = 1f+text.Length/12f;
				_captionGroup.alpha=1f;
				_speaker.SpeakNativeLive(spch);
				break;
		}
		*/
	}

		/*
	IEnumerator FaceTarget(Npc _npc){
		while(!_npc.Arrived())
			yield return null;
		Vector3 looky = FindObjectOfType<NpcNavHelper>()._patient;
		_npc.LookAt(looky);
	}
		*/

}
