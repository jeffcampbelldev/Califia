using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DragerStats : MonoBehaviour
{
	public Color _defaultStringColor;
	public Color _defaultNumColor;
	public Color _defaultBackgroundColor;
	public Color _alarmRed;
	public Color _alarmYellowLow;
	public Color _alarmYellowHigh;
	public Color _alarmTextColor;
	public CanvasGroup _statusBar;
	public Transform _statPrefab;
	[System.Serializable]
	public class Stat{
		[HideInInspector]
		public RawImage _bg;
		[HideInInspector]
		public Text _value;
		[HideInInspector]
		public Text _label;
		[HideInInspector]
		public Text _sub;
		[HideInInspector]
		public Text _units;
		public string _defaultValue;
		public string _defaultLabel;
		public string _defaultUnits;
		public bool _hasSubscript;
		[HideInInspector]
		public float _alarmTimer=-1f;//this tells us A) if in alarm and B) blink timer
		[HideInInspector]
		public bool _alarmHigh=true;//this bool tracks the alarm bg color saturation
		public string _format;
		[HideInInspector]
		public int _alarmType;

		public void SetAlarm(DragerStats s,int type){
			//dont worry about setting bg color, that's done in Update
			_bg.color=s._defaultBackgroundColor;
			//set text color
			_alarmType=type;
			switch(type){
				case 0:
				default:
					_alarmTimer=-1f;
					_value.color = s._defaultNumColor;
					_label.color = s._defaultStringColor;
					if(_hasSubscript)
						_sub.color = s._defaultStringColor;
					_units.color = s._defaultStringColor;
					break;
				case 1:
				case 2:
				case 3:
					_alarmTimer=0;
					_value.color = s._alarmTextColor;
					_label.color = s._alarmTextColor;
					if(_hasSubscript)
						_sub.color = s._alarmTextColor;
					_units.color = s._alarmTextColor;
					break;
			}
		}
	}
	public Stat[] _stats;
	MFloat[] _data;
	float _alarmPulseTime=0.25f;
	AudioSource _alarm;
	public CanvasGroup _alarmStop;
	float _silenceTimer=0;
	Text _silenceTimerText;
	public Text _modeText;
	public DragerKnobButt _dragerKnobs;
	//for alarm bar
	public SkinnedMeshRenderer _smr;
	public int _matIndex;
	Material _mat;
	bool _inAlarm;
	public MeshRenderer _mouthPiece;
    // Start is called before the first frame update
    void Start()
    {
		_alarm = GetComponent<AudioSource>();
		foreach(Stat s in _stats){
			//instance the prefab
			Transform t = Instantiate(_statPrefab,transform);
			s._bg = t.GetComponent<RawImage>();
			s._value=t.GetChild(0).GetComponent<Text>();
			s._label=t.GetChild(1).GetComponent<Text>();
			s._units=t.GetChild(2).GetComponent<Text>();
			s._value.text=s._defaultValue;
			s._label.text=s._defaultLabel;
			s._units.text=s._defaultUnits;
			if(!s._hasSubscript)
				s._label.transform.GetChild(0).gameObject.SetActive(false);
			else
				s._sub=s._label.transform.GetChild(0).GetComponent<Text>();
		}
		_alarmStop.alpha=0;
		_silenceTimerText=_alarmStop.transform.GetChild(0).GetComponent<Text>();
		//maybe these shouldn't be hardcoded to match the inspector assignments
		_data = new MFloat[6];
		for(int i=0; i<_data.Length; i++){
			_data[i]= new MFloat(_stats[i]._value,_stats[i]._format);
		}
		_mat=_smr.materials[_matIndex];
    }

	//called by DragerKnobButt when a dial is changed to see if one of the stats
	//should update accordingly
	public void UpdateStats(DragerKnobButt.DragerKnob knob){
		for(int i=0; i<_stats.Length; i++){
			if(_stats[i]._label.text==knob._label){
				_data[i].SnapTo(knob._val);
				break;
			}
		}
	}

	public void SetAlarm(int param, int type, string msg, float volume){
		_statusBar.transform.GetChild(0).GetComponent<Text>().text=msg;
		_alarm.volume=volume;
		switch(param){
			case 0:
				SetAlarmByString(type,"RR");
				break;
			case 1:
				SetAlarmByString(type,"PEEP");
				break;
			case 2:
				break;
			case 3:
				break;
			case 4:
				SetAlarmByString(type,"FiO");
				break;
			case 5:
				SetAlarmByString(type,"MVe");
				break;
			case 6:
				SetAlarmByString(type,"VT");
				break;
			case 7:
				SetAlarmByString(type,"Pmean");
				break;
			default:
				break;
		}
		_alarm.Stop();
		_statusBar.alpha=0;
		if(CheckAlarmAudio())
		{
			_alarm.Play();
			_statusBar.alpha=1;
			_inAlarm=true;
		}
		else{
			//clear highlight mat's emission scalar
			_mat.SetFloat("_EmissionPower",0);
			_inAlarm=false;
		}
		//this clears out the alarm silencer if on
		if(_silenceTimer>0)
			SilenceAlarm();
	}


	private void SetAlarmByString(int type, string label){
		foreach(Stat s in _stats){
			if(s._defaultLabel==label)
			{
				s.SetAlarm(this, type);
				return;
			}
		}
	}

	private bool CheckAlarmAudio(){
		foreach(Stat s in _stats){
			if(s._alarmType!=0)
				return true;
		}
		return false;
	}

	public void SetValue(string topic, float val){
		switch(topic){
			case "Ventilator/breath_rate":
				//dup as knob
				SetValueByLabel("RR",val);
				_dragerKnobs.SetValueByLabel("RR",val);
				break;
			case "Ventilator/ti":
				//just knob
				_dragerKnobs.SetValueByLabel("Ti",val);
				break;
			case "Ventilator/fio2":
				//dup as knob
				SetValueByLabel("FiO",val);
				_dragerKnobs.SetValueByLabel("FiO",val);
				break;
			case "Ventilator/pip":
				_dragerKnobs.SetValueByLabel("Pinsp",val);
				break;
			case "Ventilator/peep":
				//dup as knob
				SetValueByLabel("PEEP",val);
				_dragerKnobs.SetValueByLabel("PEEP",val);
				break;
			case "Ventilator/tv":
				SetValueByLabel("VT",val);
				break;
			case "Ventilator/mv":
				SetValueByLabel("MVe",val);
				break;
			case "Ventilator/delta_pressure":
				break;
			case "Ventilator/mean_ap":
				SetValueByLabel("Pmean",val);
				break;
			default:
				break;
		}
	}

	private void SetValueByLabel(string label, float val)
	{
		for(int i=0; i<_stats.Length; i++){
			if(_stats[i]._defaultLabel==label){
				//_data[i]._target=val;
				_data[i].SnapTo(val);
				break;
			}
		}
		/*
		foreach(Stat s in _stats){
			if(s._defaultLabel==label)
			{
				s._value.text=val.ToString(s._format);
				return;
			}
		}
		*/
	}

	public void SetMode(int msg){
		if(msg==0)
			_modeText.text="PC-SIMV+";
		else if(msg==1)
			_modeText.text="VC-SIMV+";
	}

	public void SilenceAlarm(){
		//if alarm silenced, unsilent it
		if(_silenceTimer>0){
			_silenceTimer=-1f;
			_alarmStop.alpha=0;
			if(_statusBar.alpha>0)
				_alarm.Play();
		}//if alarm playing, silence it
		else{
			_alarm.Stop();
			_alarmStop.alpha=1;
			_silenceTimer=120f;
		}
	}

    // Update is called once per frame
    void Update()
    {
		foreach(MFloat mf in _data)
			mf.Update();
		foreach(Stat s in _stats){
			if(s._alarmTimer>=0){
				s._alarmTimer-=Time.deltaTime;
				if(s._alarmTimer<0){
					//toggle color
					s._alarmHigh = !s._alarmHigh;
					switch(s._alarmType){
						case 1:
							s._bg.color = _alarmRed;
							break;
						case 2:
							s._bg.color = s._alarmHigh ? _alarmYellowHigh : _alarmYellowLow;
							break;
						case 3:
							s._bg.color = _alarmYellowHigh;
							break;
						default:
							break;
					}
					//reset timer
					s._alarmTimer=_alarmPulseTime;
				}
			}
		}
		if(_silenceTimer>0)
		{
			_silenceTimer-=Time.deltaTime;
			if(_silenceTimer<=0){
				//unsilent
				_alarmStop.alpha=0f;
				if(_statusBar.alpha>0)
					_alarm.Play();
			}
			else{
				_silenceTimerText.text=_silenceTimer.ToString("0 s");
			}
		}
		if(_inAlarm){
			_mat.SetFloat("_EmissionPower",Mathf.PingPong(Time.time,.25f)*4);
		}
    }

	public void SetVis(bool vis){
		transform.parent.parent.gameObject.SetActive(vis);
		_mouthPiece.enabled=vis;
	}
}
