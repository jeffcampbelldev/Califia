//Pager.cs
//
//Description: Little helper to call team members into the room
//Loads it's list and calls avatars in via RoleManager instance
//

//

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Pager : MonoBehaviour
{
	public LayerMask _mouseLayer;
	public Camera _handCam;
	public ItemStand _stand;
	List<int> _roles;
	Dictionary<int,string> _roleNames;
	MyMQTT _mqtt;
	public BoxCollider _blocker;
	public Text _callee;
	int _curRole=0;
	RoleManager _rm;
	AudioSource _audioSource;
	public AudioClip _buttonBeep;
	GameObject _select;
	GameObject _selectText;
	GameObject _up;
	GameObject _down;
	bool _runningAnim = false;
	public AnimationCurve _buttonCurve;

	void OnEnable(){
		if(_mqtt==null){
			MyMQTT [] qts = FindObjectsOfType<MyMQTT>();
			foreach(MyMQTT qt in qts){
				if(qt.gameObject.tag=="GameController")
					_mqtt=qt;
			}
		}
		if(_roles==null){
			_roles=new List<int>();
			_roleNames=new Dictionary<int,string>();
			_rm = FindObjectOfType<RoleManager>();
			foreach(int k in _rm._roles.Keys){
				if(k>2){
					_roles.Add(k);
					_roleNames.Add(k,_rm._roles[k]._name);
				}
			}
			_callee.text=_roleNames[_roles[_curRole]];
		}
		_audioSource = GetComponent<AudioSource>();
		_blocker.enabled=true;
		_select = gameObject.transform.GetChild(1).transform.GetChild(2).gameObject;
		_selectText = gameObject.transform.GetChild(0).transform.GetChild(2).gameObject;
		_up = gameObject.transform.GetChild(1).transform.GetChild(3).gameObject;
		_down = gameObject.transform.GetChild(1).transform.GetChild(0).gameObject;
	}

	void OnDisable(){
		_blocker.enabled=false;
	}

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
		RaycastHit hit;
		Vector3 ray = (Vector3)Input.mousePosition;
		if(!Physics.Raycast(_handCam.transform.position,_handCam.ScreenPointToRay(ray).direction,out hit,1f,_mouseLayer)){
			if(Input.GetMouseButtonDown(0)){
				_stand.ReturnItem();
				_runningAnim = false;
			}
		}
    }

	public void Cycle(int dir){
        if (!_runningAnim)
        {
			_audioSource.PlayOneShot(_buttonBeep);
			_runningAnim = true;
			if (dir == 1)
			{
				//play anim for up arrow
				StartCoroutine(buttonAnimR(_up));
			}
			if (dir == -1)
			{
				//play anim for down arrow
				StartCoroutine(buttonAnimR(_down));
			}

			_curRole += dir;
			if (_curRole >= _roles.Count)
				_curRole = 0;
			else if (_curRole < 0)
				_curRole = _roles.Count - 1;
			_callee.text = _roleNames[_roles[_curRole]];
		}
	}

	public void Call(){
		if (!_runningAnim)
		{
			Debug.Log("calling role: " + _roles[_curRole]);
			_runningAnim = true;
			_audioSource.Play();
			StartCoroutine(buttonAnimR(_select));
			_rm.RoleAction(_roles[_curRole], "", 5, "", 0, 0, true);
		}
	}

	private IEnumerator buttonAnimR(GameObject button)
    {
		float timePassed = 0f;
		Vector3 origPos = Vector3.zero;

		if (button == _select)
        {
			origPos = _selectText.transform.localPosition;
        }

		while (timePassed < .5f)
        {
			timePassed += Time.deltaTime;
			button.transform.localPosition = Vector3.up * Mathf.Lerp(0, _buttonCurve.Evaluate(timePassed), timePassed) * .01f;

			if (button == _select)
            {
				_selectText.transform.localPosition = origPos - Vector3.forward * Mathf.Lerp(0, _buttonCurve.Evaluate(timePassed), timePassed) * .01f;
			}

			yield return null;
		}

		_runningAnim = false;
	}
}
