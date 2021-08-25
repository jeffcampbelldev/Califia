using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PrimingVolumeMenu : MonoBehaviour
{
	InputField _input;
	Canvas _can;
	TestCam _cam;
	BoxCollider _blocker;
    // Start is called before the first frame update
    void Start()
    {
		_can = GetComponent<Canvas>();
		_input = GetComponentInChildren<InputField>();
		_can.enabled=false;
		_cam = FindObjectOfType<TestCam>();
		_blocker = _cam.transform.Find("Blocker").GetComponent<BoxCollider>();
    }

    // Update is called once per frame
    void Update()
    {
		if(_can.enabled && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))){
			Submit();
		}
    }

	public void OpenMenu(GameObject bub){
		_can.enabled=true;
		_cam.enabled=false;
		_blocker.enabled=true;
		if(bub.transform.GetComponentInChildren<InfoBubble>()!=null)
			bub.transform.GetComponentInChildren<InfoBubble>().Snooze();
		_input.Select();
		_input.ActivateInputField();
	}

	public void CloseMenu(){
		_can.enabled=false;
		_cam.enabled=true;
		_blocker.enabled=false;
	}

	public void Submit(){
		float amount = 0;
		float.TryParse(_input.text, out amount);
		Debug.Log("Submitting priming vol: "+amount);
		FindObjectOfType<MyMQTT>().SendPrimer(amount);
		CloseMenu();
	}
}
