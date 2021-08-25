using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GetingeHu : MonoBehaviour
{
	public Transform _fan;
	float _tempDelta;
	float _setTemp;
	float _prevSetTemp;
	float _changeRate;
	public Text _targetTempText;
	public Text _outputTempText;
	public Text _statusText;
	public Image _waterLevel;
	float _water;
	float _targetWater;
	MyMQTT _mqtt;
	MFloat _actualTemp;
	bool _over38;
	EcmoCart _cart;
    // Start is called before the first frame update
    void Start()
    {
		_changeRate=0.5f;
		_targetWater=0.5f;
		MyMQTT [] qts = FindObjectsOfType<MyMQTT>();
		foreach(MyMQTT qt in qts){
			if(qt.gameObject.tag=="GameController")
				_mqtt=qt;
		}
		_actualTemp = new MFloat(_outputTempText,"0.0");

		_cart = FindObjectOfType<EcmoCart>();

		//will need to make a separate AR Cart var for full functionality
		if (_cart)
			SyncVals(_cart._heaterData);
    }

    // Update is called once per frame
    void Update()
    {
		_fan.Rotate(Vector3.forward*720f*Time.deltaTime);
		if(_tempDelta!=0){
			_setTemp+=_tempDelta*_changeRate*Time.deltaTime;
			if(_cart!=null)
				_cart._heaterData._setPoint=_setTemp;
		}
		//if user tries to set temp over 38 but >38 is not pressed, set to 38
		if(_setTemp>38f && _tempDelta>0 && !_over38)
		{
			_setTemp=_prevSetTemp;
			_prevSetTemp=0f;//force redraw and signal send
		}
		if(Mathf.Abs(_setTemp-_prevSetTemp)>0.1f)
		{
			_targetTempText.text = _setTemp.ToString("0.0");
			_mqtt.SendSetTemp(_setTemp);
			_prevSetTemp=_setTemp;
		}
		_water = Mathf.Lerp(_water,_targetWater,Time.deltaTime);
		_waterLevel.fillAmount=_water;
		_actualTemp.Update();
		if(_actualTemp._val>_setTemp){
			_statusText.text="PASSIVE COOLING";
		}
		else{
			_statusText.text="WARMING-UP TIME";
		}
    }

	//public method so buttons can raise or lower temp
	public void SetTempDelta(int d){
		_tempDelta=d;
	}

	/*
	public void SetTemp(float t){
		_actualTemp._target=t;
	}
	*/

	/*
	public void SetSetTemp(float t){
		_setTemp=t;
	}
	*/

	/*
	public void SetWater(float t){
		_targetWater=t;
	}
	*/

	public void Set38(){
		_over38=true;
		Debug.Log("Over 38 set");
	}
	public void Clear38(){
		_over38=false;
		Debug.Log("Over 38 reset");
	}

	public void SyncVals(EcmoCart.HeaterData hd){
		_actualTemp._target=hd._actual;
		_setTemp=hd._setPoint;
		_targetWater=hd._level*0.01f;
	}
}
