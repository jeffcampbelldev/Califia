//EcmoPump.cs
//
//Description: Parent class for ecmo pump
//


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EcmoPump : MonoBehaviour
{
	[HideInInspector]
	public Knob _rpmKnob;
	[HideInInspector]
	public EcmoCart _cart;
	protected MyMQTT _mqtt;
	public GameObject _calcShield;
	//alarm
	[HideInInspector]
	public Dictionary<int,int> _alarms = new Dictionary<int,int>();
	protected bool _inAlarm=false;
	public Text _message;
	protected AudioSource _alarm;
	
    // Start is called before the first frame update
    protected void Start()
    {
		_rpmKnob = transform.GetComponentInChildren<Knob>();
		if(_rpmKnob==null)
			Debug.Log("oops rpm knob is null");
		_cart = transform.GetComponentInParent<EcmoCart>();
		MyMQTT [] qts = FindObjectsOfType<MyMQTT>();
		foreach(MyMQTT qt in qts){
			if(qt.gameObject.tag=="GameController")
				_mqtt=qt;
		}
		SetValid(true);//removes calc shield
		_alarm = GetComponent<AudioSource>();//alarm audio source

		//it is typically up to the subclass to set up it's UI fields and call SyncVals
    }

    // Update is called once per frame
    void Update()
    {
        
    }

	public virtual void UpdateRPM(Knob k){
		//Debug.Log("rpm: "+k._val);
		_cart._pd._rpm=k._val;
	}

	public virtual void SyncVals(EcmoCart.PumpData pd){
		//Debug.Log("syncing vals");
	}

	public virtual void SetAlarm(int code, int type, string msg, float vol){
		Debug.Log("Wee woo");
		if(type==0 && _alarms.ContainsKey(code))
			_alarms.Remove(code);
		else if(type!=0 && !_alarms.ContainsKey(code))
			_alarms.Add(code,type);
		_inAlarm = _alarms.Count!=0;
		_message.text=msg;
		if(_inAlarm && type!=0)
		{
			_alarm.volume=vol;
			_alarm.Play();
		}
		else
			_alarm.Stop();
	}

	public void SetValid(bool v){
		if(_calcShield!=null)
			_calcShield.SetActive(!v);
	}
}
