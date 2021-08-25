using System;
using UnityEngine;
using UnityEngine.UI;

public class OffscreenMenu : MonoBehaviour
{
    public Camera _cam;
	protected Canvas _can;
	protected TestCam _testCam;
	protected BoxCollider _blocker;
    protected MyMQTT _mqtt;
    protected Text _title;
	public ConveyorBelt _belt;
	protected Action onSelectionConfirm;

    public virtual void Start(){
		_cam.enabled=false;
		_can = GetComponent<Canvas>();
		_can.enabled=false;
		_testCam = FindObjectOfType<TestCam>();
		_blocker = _testCam.transform.Find("Blocker").GetComponent<BoxCollider>();
		_title = transform.Find("Title").GetComponent<Text>();
		MyMQTT [] qts = FindObjectsOfType<MyMQTT>();
		foreach(MyMQTT qt in qts){
			if(qt.gameObject.tag=="GameController")
				_mqtt=qt;
		}
    }

	public virtual void OpenMenu(int module){
		ShowMenu(true);
	}

	public virtual void HideMenu(){
		ShowMenu(false);
	}

    public void ShowMenu(bool show){
		//enable cam
		_cam.enabled=show;
		//enable can
		_can.enabled = show;
		//disable testcam
		_testCam.enabled = !show;
		//block clicks
		_blocker.enabled=show;
	}
}
