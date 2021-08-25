//Header comment goes here

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GenericPump : EcmoPump
{
	public MFloat _flowRate;
	Material _lightMat;
	public MeshRenderer _lightStrip;
    // Start is called before the first frame update
    new void Start()
    {
		base.Start();
		_lightMat = _lightStrip.material;
    }

    // Update is called once per frame
    void Update()
    {
		_flowRate.Update();
    }

	public override void SyncVals(EcmoCart.PumpData pd){
		Debug.Log("Setting flow rate: "+pd._flowRate);
		_flowRate._target=pd._flowRate;
		if(_rpmKnob==null)
			Debug.Log("yup rpm knob null");
		_rpmKnob.JustSetValue(pd._rpm);
	}

	public override void SetAlarm(int code, int type, string msg, float vol){
		base.SetAlarm(code,type,msg,vol);
		if(_inAlarm){
			//set strip color based on type
			if(type==1)
				_lightMat.SetColor("_EmissionColor",Color.red);
			else
				_lightMat.SetColor("_EmissionColor",Color.yellow);
		}
		else
			_lightMat.SetColor("_EmissionColor",Color.green);

		switch(code){
			case 0:
				SetColor(_flowRate._text,type,code);
				break;
			default:
				break;
		}
	}

	public void SetColor(Text[] txt, int type,int index){
		//assign colors (in static cases)
		foreach(Text t in txt){
			if(type==0)
				t.color=Color.green;
			else if(type==1)
				t.color=Color.red;
			else
				t.color=Color.yellow;
		}
	}
}
