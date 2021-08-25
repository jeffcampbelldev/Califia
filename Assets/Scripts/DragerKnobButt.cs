using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class DragerKnobButt : MonoBehaviour
{
	[System.Serializable]
	public class DragerKnob{
		[HideInInspector]
		public Button _butt;
		[HideInInspector]
		public Text _valueDisplay;
		[HideInInspector]
		public Text _labelDisplay;
		[HideInInspector]
		public Transform _indicator;
		public string _label;
		public bool _hasSubscript;
		public float _min;
		public float _max;
		[HideInInspector]
		public float _val;
		public string _format;
	}
	DragerKnob _selected;
	public DragerKnob[] _knobs;//touch screen button knobs
	public Knob _knob;//main knob

	//timer to reset value of rotary knob is not clicked in time
	float _resetTimer;

	EventSystem _eventSys;

	const float _indicatorRange=135f;

	public DragerStats _dragerStats;

	public Transform _knobButtPrefab;

	MyMQTT _mqtt;

    // Start is called before the first frame update
    void Start()
    {
		_eventSys = EventSystem.current;
		foreach(DragerKnob dk in _knobs){
			Transform t = Instantiate(_knobButtPrefab,transform);
			//buttknob starts at min
			dk._val=dk._min;
			//get ui references
			dk._butt=t.GetChild(0).GetComponent<Button>();
			dk._indicator=t.GetChild(1);
			float range01=Mathf.InverseLerp(dk._min,dk._max,dk._val);
			dk._indicator.localEulerAngles=Vector3.back*Mathf.Lerp(-_indicatorRange,_indicatorRange,range01);
			dk._valueDisplay=t.GetChild(2).GetComponent<Text>();
			dk._valueDisplay.text=dk._val.ToString(dk._format);
			dk._labelDisplay=t.GetChild(3).GetComponent<Text>();
			dk._labelDisplay.text=dk._label;
			dk._labelDisplay.transform.GetChild(0).gameObject.SetActive(dk._hasSubscript);
			//add hooks
			dk._butt.onClick.AddListener(delegate{SelectKnobButt(dk);});
		}
    }

	public void SelectKnobButt(DragerKnob dk){
		if(dk!=_selected){
			if(_selected!=null)
				DeselectKnobButt();
			_selected=dk;
			//tell the knob what it's min and max values should be
			_knob._minVal=dk._min;
			_knob._maxVal=dk._max;
			_knob._val=dk._val;
			_knob._changeSpeed=(_knob._maxVal-_knob._minVal)*0.0005f;
			_resetTimer=10f;
		}
	}

	public void ValueChanged(){
		if(_selected!=null)
		{
			_selected._valueDisplay.text=_knob._val.ToString(_selected._format);
			float range01=Mathf.InverseLerp(_selected._min,_selected._max,_knob._val);
			_selected._indicator.localEulerAngles=Vector3.back*Mathf.Lerp(-_indicatorRange,_indicatorRange,range01);
			_resetTimer=10f;
		}
	}

	public void ValueConfirmed(){
		if(_selected!=null){
			_selected._val=_knob._val;
			//send the force val to mqtt
			Debug.Log("Confirming value for knob: "+_selected._val);
			_mqtt = FindObjectOfType<MyMQTT>();
			_mqtt.ForceVentValue(_selected._label,_selected._val);
			_dragerStats.UpdateStats(_selected);
			DeselectKnobButt();
		}
	}

	public void SetValueByLabel(string label, float val){
		foreach(DragerKnob dk in _knobs){
			if(dk._label==label){
				dk._val=val;
				dk._valueDisplay.text=dk._val.ToString(dk._format);
				float range01=Mathf.InverseLerp(dk._min,dk._max,dk._val);
				dk._indicator.localEulerAngles=Vector3.back*Mathf.Lerp(-_indicatorRange,_indicatorRange,range01);
				return;
			}
		}
	}

	private void DeselectKnobButt(){
		_selected._valueDisplay.text=_selected._val.ToString(_selected._format);
		float range01=Mathf.InverseLerp(_selected._min,_selected._max,_selected._val);
		_selected._indicator.localEulerAngles=Vector3.back*Mathf.Lerp(-_indicatorRange,_indicatorRange,range01);
		_selected=null;
		_eventSys = EventSystem.current;
		_eventSys.SetSelectedGameObject(null);
	}

    // Update is called once per frame
    void Update()
    {
		if(_selected!=null)
		{
			_resetTimer-=Time.deltaTime;
			if(_resetTimer>0)
				_selected._butt.Select();
			else
				DeselectKnobButt();
		}
    }
}
