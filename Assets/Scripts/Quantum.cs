using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Quantum : MonoBehaviour
{
	public Transform _artRoller;
	public Transform _knobPanels;
	public Transform _knobMiniPanels;
	public Transform _vsPanelsLParent;
	[System.Serializable]
	public class QKnob{
		public string _label;
		public string _pubAlias;
		public float _changeSpeed;
		public string _textConversion;
		public Color _color;
		public bool _matchColor;
		[HideInInspector]
		public Transform _panel;
		[HideInInspector]
		public RawImage _headerBg;
		[HideInInspector]
		public Text _headerText;
		[HideInInspector]
		public Text _mainText;
		[HideInInspector]
		public float _val;
		public float _min;
		public float _max;

		//copy constructor
		public QKnob(QKnob copy){
			_color=copy._color;
			_matchColor=copy._matchColor;
			_label = copy._label;
			_min=copy._min;
			_max=copy._max;
			_pubAlias = copy._pubAlias;
			_changeSpeed=copy._changeSpeed;
			_textConversion=copy._textConversion;
		}

		public void Setup(Transform panel, Quantum q){
			_panel=panel;
			_headerBg = _panel.Find("HeaderBG").GetComponent<RawImage>();
			_headerText = _panel.Find("HeaderText").GetComponent<Text>();
			_mainText = _panel.Find("MainValue").GetComponent<Text>();
			_headerBg.color=_color;
			_headerText.text=_label;
			_val=0;
			if(_matchColor)
				_mainText.color=_color;
			if(_panel.Find("Plus")!=null){
				Button plus = _panel.Find("Plus").GetComponent<Button>();
				Button minus = _panel.Find("Minus").GetComponent<Button>();
				plus.onClick.AddListener(delegate{q.IncVal(this);});
				minus.onClick.AddListener(delegate{q.DecVal(this);});
			}
		}

		public void ToggleMini(QKnob other){
			_color=other._color;
			_matchColor=other._matchColor;
			_label = other._label;
			_headerBg.color=_color;
			_headerText.text=_label;
			_min=other._min;
			_max=other._max;
			_pubAlias=other._pubAlias;
			_changeSpeed=other._changeSpeed;
			_textConversion=other._textConversion;
			if(_matchColor)
				_mainText.color=_color;
			CopyValue(other);
		}

		public void CopyValue(QKnob other){
			_mainText.text=other._mainText.text;
			_val=float.Parse(_mainText.text);
		}

		public void ChangeValue(float delta){
			_val+=delta;
			_mainText.text = _val.ToString("0.00");
		}
	}

	[System.Serializable]
	public class QDataPanel{
		public Text _artFlow;
		public Text _venFlow;
		public Text _sao2;
		public Text _svo2;
		public Text _hb;
		public Text _fio2;
		public Text _sweep;
		public Text _po2;
		public Text _pco2;
		public Text _rso2L;
		public Text _rso2R;
		public Text _cvp;
		public Text _venTemp;
		public Text _bladTemp;
		public Text _map;
		public Text _diastole;
		public Text _systole;
	}

	[System.Serializable]
	public class Tubing{
		public Material _reservoirMat;
		public Material _reservoirCapMat;
		public Tube[] _venTube;
		public Tube[] _artTube;
	}

	[System.Serializable]
	public class VSPanel{
		public string _label;
		public string _unit;
		public Color _color;
		[HideInInspector]
		public RawImage _frame;
		[HideInInspector]
		public Text _labelText;
		[HideInInspector]
		public Text _valueText;
		[HideInInspector]
		public Text _unitText;

		public void Setup(Transform panel){
			_frame = panel.GetComponent<RawImage>();
			_labelText = panel.Find("Label").GetComponent<Text>();
			_valueText = panel.Find("Value").GetComponent<Text>();
			_unitText = panel.Find("Unit").GetComponent<Text>();
			_frame.color=_color;
			_labelText.text=_label;
			_valueText.text = "0.00";
			_valueText.color=_color;
			_unitText.text=_unit;
		}

	}
	public QKnob [] _qKnobs;
	public int [] _minis;
	private QKnob [] _qKnobMinis;
	public Knob[] _knobs;
	public VSPanel[] _vsPanelsL;
	public MFloat[] _mData;
	//this may be temp - not sure if buttons have continuous press
	//if it is continuous that QKnobs should probably hold a reference to float
	public float _changeRate;

	public QDataPanel _data;

	public LiquidHelper _res;
	[HideInInspector]
	public float _artFlow;

	public Tubing _tubing;

	//occluders
	public Animator _venOccAnim;
	public float _venOcc;
    // Start is called before the first frame update
    void Start()
    {
		//set up main knob panels
		for(int i=0; i<_qKnobs.Length; i++)
		{
			_qKnobs[i].Setup(_knobPanels.GetChild(i),this);
		}
		//set up minis
		_qKnobMinis = new QKnob[_minis.Length];
		for(int i=0; i<_qKnobMinis.Length; i++){
			//a mini is basically a copy of the main knobs
			_qKnobMinis[i] = new QKnob(_qKnobs[_minis[i]]);
			_qKnobMinis[i].Setup(_knobMiniPanels.GetChild(i),this);
			_knobs[i].UpdateBounds(_qKnobMinis[i]._min,_qKnobMinis[i]._max);
			_knobs[i]._pubAlias=_qKnobMinis[i]._pubAlias;
			_knobs[i]._changeSpeed=_qKnobMinis[i]._changeSpeed;
			_knobs[i]._textConversion=_qKnobMinis[i]._textConversion;
		}
		//set up vent system panels
		for(int i=0; i<_vsPanelsL.Length; i++){
			_vsPanelsL[i].Setup(_vsPanelsLParent.GetChild(i));
		}
		_mData = new MFloat[16];
		_mData[0] = new MFloat(_data._venFlow,"0.00");
		_mData[1] = new MFloat(_data._sao2,"0");
		_mData[2] = new MFloat(_data._svo2,"0");
		_mData[3] = new MFloat(_data._hb,"0.0");
		_mData[4] = new MFloat(new Text[]{_data._fio2,_vsPanelsL[0]._valueText},"0");
		_mData[5] = new MFloat(new Text[]{_data._sweep,_vsPanelsL[1]._valueText},"0.0");
		_mData[6] = new MFloat(_data._po2,"0");
		_mData[7] = new MFloat(_data._pco2,"0");
		_mData[8] = new MFloat(_data._rso2L,"0");
		_mData[9] = new MFloat(_data._rso2R,"0");
		_mData[10] = new MFloat(_data._cvp,"0");
		_mData[11] = new MFloat(_data._venTemp,"0.0");
		_mData[12] = new MFloat(_data._bladTemp,"0.0");
		_mData[13] = new MFloat(_data._map,"0");
		_mData[14] = new MFloat(_data._diastole,"0");
		_mData[15] = new MFloat(_data._systole,"0");
    }

    // Update is called once per frame
    void Update()
    {
		_artRoller.Rotate(Vector3.up*Time.deltaTime*90f*_artFlow);
		foreach(MFloat mf in _mData)
			mf.Update();
    }

	public void UpdateKnobMini(int mIndex){
		QKnob main = _qKnobs[_minis[mIndex]];
		main.CopyValue(_qKnobMinis[mIndex]);
		//check for arterial flow
		CheckValue(main);
	}

	//checks for main Q panel and minis
	//todo: check for vs panel as well! I think these may be hardcoded now
	public void SetVal(string label, float val){
		foreach(QKnob qk in _qKnobs){
			//Debug.Log(qk._label);
			if(qk._label==label)
			{
				qk._val=val;
				qk.ChangeValue(0);//this causes text to update
				CheckForMini(qk);
			}
		}
	}

	public void IncVal(QKnob knobPanel){
		//change upper panel
		knobPanel.ChangeValue(_changeRate);
		//check for mini
		CheckForMini(knobPanel,true);
	}
	public void DecVal(QKnob knobPanel){
		//change upper panel
		knobPanel.ChangeValue(-_changeRate);
		//check for mini
		CheckForMini(knobPanel,true);
	}
	private void CheckForMini(QKnob knobPanel, bool publish=false){
		for(int i=0; i<_qKnobMinis.Length; i++){
			if(_qKnobMinis[i]._label==knobPanel._label)
			{
				_qKnobMinis[i].CopyValue(knobPanel);
				_knobs[i].JustSetValue(knobPanel._val,publish);
			}
		}
		CheckValue(knobPanel);
	}

	void CheckValue(QKnob qk){
		switch(qk._label){
			case "Arterial":
				_artFlow=qk._val;
				_data._artFlow.text=qk._val.ToString("0.00");
				_vsPanelsL[3]._valueText.text=qk._val.ToString("0.0");
				//Debug.Log("Setting art flow: "+_artFlow);
				break;
			case "Venous Clamp":
				_venOcc=qk._val;
				_venOccAnim.SetFloat("Blend",_venOcc*.01f);
				//Debug.Log("Setting ven occluder: "+_venOcc);
				break;
			default:
				break;
		}
	}

	public void SetData(string topic,float val){
		switch(topic){
			case "HLM/arterial/flow_rate":
				_data._artFlow.text=val.ToString("0.00");
				SetVal("Arterial",val);
				//later these vs panels should be included in SetVal
				_vsPanelsL[3]._valueText.text=val.ToString("0.0");
				break;
			case "HLM/venous/flow_rate":
				//_data._venFlow.text=val.ToString("0.00");
				_mData[0]._target=val;
				break;
			case "HLM/venous/occluder":
				SetVenOccluder(val);
				SetVal("Venous Clamp",val);
				break;
			case "HLM/sao2":
				_mData[1]._target=val;
				//_data._sao2.text=val.ToString("0");
				break;
			case "HLM/svo2":
				_mData[2]._target=val;
				//_data._svo2.text=val.ToString("0");
				break;
			case "HLM/hb":
				_mData[3]._target=val;
				//_data._hb.text=val.ToString("0.0");
				break;
			case "HLM/gas_mixer/fio2":
				_mData[4]._target=val;
				//_data._fio2.text=val.ToString("0");
				//_vsPanelsL[0]._valueText.text=val.ToString("0.0");
				break;
			case "HLM/gas_mixer/rate":
				_mData[5]._target=val;
				//_data._sweep.text=val.ToString("0.0");
				//_vsPanelsL[1]._valueText.text=val.ToString("0.0");
				break;
			case "HLM/pao2":
				_mData[6]._target=val;
				//_data._po2.text=val.ToString("0");
				break;
			case "HLM/paco2":
				_mData[7]._target=val;
				//_data._pco2.text=val.ToString("0");
				break;
			case "HLM/rso2/left":
				_mData[8]._target=val;
				//_data._rso2L.text=val.ToString("0");
				break;
			case "HLM/rso2/right":
				_mData[9]._target=val;
				//_data._rso2R.text=val.ToString("0");
				break;
			case "HLM/cvp":
				_mData[10]._target=val;
				//_data._cvp.text=val.ToString("0");
				break;
			case "HLM/temperature_ven":
				_mData[11]._target=val;
				//_data._venTemp.text=val.ToString("0.0");
				break;
			case "HLM/temperature_bladder":
				_mData[12]._target=val;
				//_data._bladTemp.text=val.ToString("0.0");
				break;
			case "HLM/abp/mean":
				_mData[13]._target=val;
				//_data._map.text=val.ToString("0");
				break;
			case "HLM/abp/diastole":
				_mData[14]._target=val;
				//_data._diastole.text=val.ToString("0");
				break;
			case "HLM/abp/systole":
				_mData[15]._target=val;
				//_data._systole.text=val.ToString("0");
				break;
			default:
				break;
		}
	}

	public void ToggleMini(int minIndex){
		int mini =_minis[minIndex];
		if(mini < _qKnobs.Length-5)
			mini+=5;
		else if(mini>=5)
			mini-=5;
		_minis[minIndex]=mini;
		QKnob qk = _qKnobs[mini];
		_qKnobMinis[minIndex].ToggleMini(qk);
		_knobs[minIndex].UpdateBounds(qk._min,qk._max);
		_knobs[minIndex]._pubAlias=qk._pubAlias;
		_knobs[minIndex]._changeSpeed=qk._changeSpeed;
		_knobs[minIndex]._textConversion=qk._textConversion;
		_knobs[minIndex].JustSetValue(qk._val,false);
	}

	public void SetResVolume(float f){
		_res._targetHeight=f/4;
	}

	public void SetVenOccluder(float f){
		_venOcc=f;
		_venOccAnim.SetFloat("Blend",_venOcc*.01f);
	}

	public void SetColor(string topic, string col){
		Color color;
		string colHex = "#"+col;
		if(!ColorUtility.TryParseHtmlString(colHex, out color))
		{
			Debug.Log("issue parsing color");
			return;
		}
		switch(topic){
			case "HLM/venous/reservoir/blood_color":
				_tubing._reservoirMat.SetColor("_Color",color);
				_tubing._reservoirCapMat.SetColor("_Color",color);
				_tubing._venTube[1].SetBloodColor(col);
				Debug.Log("Setting colors");
				break;
			case "HLM/venous/tubing/blood_color":
				_tubing._venTube[0].SetBloodColor(col);
				/*
				foreach(Tube t in _tubing._venTube){
					t.SetBloodColor(col);
				}
				*/
				break;
			case "HLM/venous/tubing/label_color":
				foreach(Tube t in _tubing._venTube){
					t.SetLabelColor(col);
				}
				break;
			case "HLM/arterial/tubing/blood_color":
				foreach(Tube t in _tubing._artTube){
					t.SetBloodColor(col);
				}
				break;
			case "HLM/arterial/tubing/label_color":
				foreach(Tube t in _tubing._artTube){
					t.SetLabelColor(col);
				}
				break;
			default:
				break;
		}
	}
}
