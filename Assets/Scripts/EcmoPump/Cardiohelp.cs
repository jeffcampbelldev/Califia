//header comment goes here

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Cardiohelp : EcmoPump
{
	[System.Serializable]
	public class DataFields
	{
		public Text [] _flowRate;
		public Text [] _rpm;
		public Text [] _pVen;
		public Text [] _pArt;
		public Text [] _pInt;
		public Text [] _dP;
		public Text [] _svo2;
		public Text [] _tVen;
		public Text [] _tArt;
		public Text [] _hb;
		public Text [] _hct;

		public void SetColorByIndex(int index, Color c){
			switch(index){
				case 0:
					foreach(Text t in _flowRate)
						t.color=c;
					break;
				case 1:
					foreach(Text t in _pArt)
						t.color=c;
					break;
				case 2:
					foreach(Text t in _pVen)
						t.color=c;
					break;
				case 3:
					foreach(Text t in _tArt)
						t.color=c;
					break;
				case 4:
					foreach(Text t in _tVen)
						t.color=c;
					break;
				case 5:
					foreach(Text t in _svo2)
						t.color=c;
					break;
			}
		}
	}

	public DataFields _dataText;
	MFloat [] _data;

	public RawImage _bgImage;
	public Color _grey, _red,_blue,_yellow;
	float _alarmTimer=0;
	public float _pulsePeriod;

	public CanvasGroup [] _screens;
	int _homeScreen = 1;
	int _curScreen=1;
	public Text _type;

	// Start is called before the first frame update
	new void Start()
	{
		base.Start();
		GotoScreen(_curScreen);//default screen

		_data = new MFloat[11];
		_data[0]=new MFloat(_dataText._flowRate,"0.00");
		_data[1]=new MFloat(_dataText._rpm,"0");
		_data[2]=new MFloat(_dataText._pVen,"0");
		_data[3]=new MFloat(_dataText._pArt,"0");
		_data[4]=new MFloat(_dataText._dP,"0.0");
		_data[5]=new MFloat(_dataText._svo2,"0.0");
		_data[6]=new MFloat(_dataText._tVen,"0.0");
		_data[7]=new MFloat(_dataText._tArt,"0.0");
		_data[8]=new MFloat(_dataText._hb,"0.0");
		_data[9]=new MFloat(_dataText._hct,"0.0");
		_data[10]=new MFloat(_dataText._pInt,"0");
		
		SyncVals(_cart._pd);
	}

	// Update is called once per frame
	void Update()
	{
		foreach(MFloat mf in _data){
			mf.Update();
		}
		if(_inAlarm){
			_alarmTimer+=Time.deltaTime;
			if(_alarmTimer>_pulsePeriod){

				if(_bgImage.color==_red)
				{
					_bgImage.color=_blue;
					foreach(int i in _alarms.Keys){
						if(_alarms[i]==2)
							_dataText.SetColorByIndex(i,_blue);
					}
				}
				else
				{
					_bgImage.color=_red;
					foreach(int i in _alarms.Keys){
						if(_alarms[i]==2)
							_dataText.SetColorByIndex(i,_yellow);
					}
				}
				_alarmTimer=0;
			}
		}
	}

	public override void SyncVals(EcmoCart.PumpData pd){
		_data[0]._target=pd._flowRate;
		_data[2]._target=pd._pressureVen;
		_data[3]._target=pd._pressureArt;
		_data[6]._target=pd._tempVen;
		_data[7]._target=pd._tempArt;
		_data[4]._target=pd._pressureDelta;
		_data[5]._target=pd._svo2;
		_data[8]._target=pd._hb;
		_data[9]._target=pd._hct;
		_data[10]._target=pd._pressureInt;
		_data[1].SnapTo(pd._rpm);
		_rpmKnob.JustSetValue(pd._rpm);
	}

	public override void SetAlarm(int code, int type, string msg, float vol){
		base.SetAlarm(code,type,msg,vol);
		switch(code){
			case 0:
				SetColor(_dataText._flowRate,type,code);
				break;
			case 1:
				SetColor(_dataText._pArt,type,code);
				break;
			case 2:
				SetColor(_dataText._pVen,type,code);
				break;
			case 3:
				SetColor(_dataText._tArt,type,code);
				break;
			case 4:
				SetColor(_dataText._tVen,type,code);
				break;
			case 5:
				SetColor(_dataText._svo2,type,code);
				break;
			default:
				break;
		}
		if(!_inAlarm)
			_bgImage.color=_grey;
	}

	public void SetColor(Text[] txt, int type,int index){

		//assign colors (in static cases)
		foreach(Text t in txt){
			if(type==0)
				t.color=_blue;
			else if(type==1)
				t.color=_red;
			else
				t.color=_yellow;
		}
	}

	//this is the handler for the rpm knob in scene
	public override void UpdateRPM(Knob k){
		base.UpdateRPM(k);
		//why is this needed? doesn't the knob set the value ok?
		_data[1].SnapTo(k._val);
	}

	public void GotoScreen(int screen){
		if(screen>=_screens.Length)
			return;
		for(int i=0; i<_screens.Length; i++){
			_screens[i].alpha = screen==i? 1f : 0f;
			if(screen==i)
				_type.text=_screens[i].transform.name.Split('-')[1];
		}
		_curScreen=screen;
		if(_mqtt!=null && screen>0)
			_mqtt.PanelUpdate(screen-1);
	}

	public void RotateHomeScreen(){
		if(_curScreen==0)
			return;
		_homeScreen++;
		if(_homeScreen>=_screens.Length)
			_homeScreen=1;
		GotoScreen(_homeScreen);
	}
}
