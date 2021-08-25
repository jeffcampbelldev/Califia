using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

//this class shares a lot with AvatarPlacement the two should probably be polymorphed
public class AvatarAssignment : NGInspector
{
	public GameObject _inspector;
	BoxCollider _blocker;
	TestCam _cam;
	public Sprite [] _avSprites;
	public Sprite _invis;
	public Dropdown _roleDrop;
	public Dropdown _avatarDrop;
	public Dropdown _voiceDrop;
	RoleManager _rm;
	string _lastScene;
	public Image _previewSprite;
	public Button _save;
    // Start is called before the first frame update
    void Start()
    {
		_cam = FindObjectOfType<TestCam>();
		_blocker=_cam.transform.Find("Blocker").GetComponent<BoxCollider>();
		_rm = FindObjectOfType<RoleManager>();
		_inspector.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

	public override void ShowInspector(bool show){
		_inspector.SetActive(show);
		if(_blocker!=null)
			_blocker.enabled=show;
		if(_cam!=null)
			_cam.enabled=!show;
		if(show){
			string curScene = SceneManager.GetActiveScene().name;
			if(curScene!=_lastScene){
				//Setup menu
				//Setup role drop down
				_roleDrop.options.Clear();
				foreach(RoleManager.Role role in _rm._roles.Values){
					Dropdown.OptionData tmp = new Dropdown.OptionData(role._name);
					_roleDrop.options.Add(tmp);
				}
				//Setup avatar drop down
				_avatarDrop.options.Clear();
				foreach(RoleManager.Avatar av in _rm._avatars.Values){
					Dropdown.OptionData tmp = new Dropdown.OptionData(av._name);
					bool matchFound=false;
					foreach(Sprite s in _avSprites){
						if(s.name==tmp.text)
						{
							tmp.image=s;
							matchFound=true;
						}
					}
					if(!matchFound)
						tmp.image=_invis;
					_avatarDrop.options.Add(tmp);
				}
				//Setup voice drop down
				_voiceDrop.options.Clear();
				foreach(string voice in _rm._voices.Values){
					Dropdown.OptionData tmp = new Dropdown.OptionData(voice);
					_voiceDrop.options.Add(tmp);
				}

				//set up selected vals
				RoleSelectionChanged();
				AvatarSelectionChanged();
				_save.interactable=false;
				_lastScene=curScene;
				_voiceDrop.RefreshShownValue();
				_avatarDrop.RefreshShownValue();
				_roleDrop.RefreshShownValue();
			}	
		}
	}

	public void RoleSelectionChanged(){
		_save.interactable=false;
		string selectedRole = _roleDrop.options[_roleDrop.value].text;
		string avVal = _rm.GetAvatarFromRole(selectedRole);
		for(int i=0; i<_avatarDrop.options.Count; i++){
			if(_avatarDrop.options[i].text==avVal)
			{
				_avatarDrop.value=i;
				//The thing is we want the voices to refresh such that changing the role
				//clears the whole slate regardless of whether or not the avatar changes
				AvatarSelectionChanged();
				return;
			}
		}
		//Debug.Log("oops could not find corresponding avatar for role");
	}

	public void AvatarSelectionChanged(){
		//update sprite
		_previewSprite.sprite=_avatarDrop.options[_avatarDrop.value].image;

		//change voice
		string selectedAvatar = _avatarDrop.options[_avatarDrop.value].text;
		string voice = _rm.GetVoiceFromAvatar(selectedAvatar);
		for(int i=0; i<_voiceDrop.options.Count; i++){
			if(_voiceDrop.options[i].text==voice)
			{
				_voiceDrop.value=i;
				break;
			}
		}
		_voiceDrop.RefreshShownValue();
		//Debug.Log("oops could not find corresponding voice for avatar");
	}

	public void VoiceSelectionChanged(){

	}

	public void RoleButtonClick(){
		_roleDrop.Show();
	}
	public void AvatarButtonClick(){
		_save.interactable=true;
		_avatarDrop.Show();
	}
	public void VoiceButtonClick(){
		_save.interactable=true;
		_voiceDrop.Show();
	}

	public void SaveAssignments(){
		string role = _roleDrop.options[_roleDrop.value].text;
		string avatar = _avatarDrop.options[_avatarDrop.value].text;
		string voice = _voiceDrop.options[_voiceDrop.value].text;
		_rm.SaveAssignment(role,avatar,voice);
	}
}
