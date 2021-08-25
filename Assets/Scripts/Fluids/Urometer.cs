using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Urometer : MonoBehaviour
{
	public LiquidHelper _bard;
	public float _bardCapacity;
	MyMQTT _mqtt;
    // Start is called before the first frame update
    void Start()
    {
		MyMQTT [] qts = FindObjectsOfType<MyMQTT>();
		foreach(MyMQTT qt in qts){
			if(qt.gameObject.tag=="GameController")
				_mqtt=qt;
		}
    }

    // Update is called once per frame
    void Update()
    {
        
    }

	public void SetData(float volume, string color){
		//for now just send straight to bard bag
		//volume
		_bard._targetHeight=volume/_bardCapacity;
		//color
		string colHex = "#"+color;
		Color col;
		if(!ColorUtility.TryParseHtmlString(colHex, out col))
			return;
		_bard.SetColor(col);
	}

	public void DrainUrine(){
		_bard._targetHeight=0f;
		_mqtt.ForceDrain();
	}
}
