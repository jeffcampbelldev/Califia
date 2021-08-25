using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LifeMotion : EcmoPump
{
	public Text _rpmText;
	public Text _flowRateText;
	public Image _rpmBar;
	public Image _flowRateBar;
	public Text _svo2Text;
	public Transform _svo2Arrow;
	public Text _dpText;
	public Text _pvenText;
	public Transform _pvenArrow;
	public Text _partText;
	public Transform _partArrow;
	public Text _tartText;
	public Transform _tartArrow;
	public Text _hbText;
	public Transform _hbArrow;
	MFloat[] _data;

    // Start is called before the first frame update
    new void Start()
    {
		base.Start();
		_data = new MFloat[8];
		_data[0] = new MFloat(_rpmText,"0");
		_data[1] = new MFloat(_flowRateText,"0.00");
		_data[2] = new MFloat(_svo2Text,"0");
		_data[3] = new MFloat(_dpText,"0");
		_data[4] = new MFloat(_pvenText,"0");
		_data[5] = new MFloat(_partText,"0");
		_data[6] = new MFloat(_tartText,"0.0");
		_data[7] = new MFloat(_hbText,"0.0");

		SyncVals(_cart._pd);
    }

    // Update is called once per frame
    void Update()
    {
		foreach(MFloat mf in _data)
			mf.Update();
    }

	public override void UpdateRPM(Knob k){
		base.UpdateRPM(k);
		_data[0].SnapTo(k._val);
		_rpmBar.fillAmount=Mathf.InverseLerp(0,9000,k._val);
	}

	public override void SyncVals(EcmoCart.PumpData pd){
		float flip;
		_data[1]._target=pd._flowRate;
		_flowRateBar.fillAmount=Mathf.InverseLerp(-10f,12f,_data[1]._val);
		_data[0].SnapTo(pd._rpm);
		_rpmKnob.JustSetValue(pd._rpm);
		_rpmBar.fillAmount=Mathf.InverseLerp(0,9000,pd._rpm);
		_data[2]._target=pd._svo2;
		flip = _data[2]._target < _data[2]._val ? -1f : 1f;
		_svo2Arrow.localScale = new Vector3(1,flip,1);
		_data[3]._target=pd._pressureDelta;
		_data[4]._target=pd._pressureVen;
		flip = _data[4]._target < _data[4]._val ? -1f : 1f;
		_pvenArrow.localScale = new Vector3(1,flip,1);
		_data[5]._target=pd._pressureArt;
		flip = _data[5]._target < _data[5]._val ? -1f : 1f;
		_partArrow.localScale = new Vector3(1,flip,1);
		_data[6]._target=pd._tempArt;
		flip = _data[6]._target < _data[6]._val ? -1f : 1f;
		_tartArrow.localScale = new Vector3(1,flip,1);
		_data[7]._target=pd._hb;
		flip = _data[7]._target < _data[7]._val ? -1f : 1f;
		_hbArrow.localScale = new Vector3(1,flip,1);
	}

	public void UpdatePanelText(string topic, float val){
		float flip=1;
		switch(topic){
			case "ECMO_Pump/centrifugal/flow_rate":
				_data[1]._target=val;
				_flowRateBar.fillAmount=Mathf.InverseLerp(-10f,12f,_data[1]._val);
				break;
			case "ECMO_Pump/centrifugal/rpm":
				_data[0].SnapTo(val);
				_rpmKnob.JustSetValue(val);
				_rpmBar.fillAmount=Mathf.InverseLerp(0,9000,val);
				break;
			case "ECMO_Pump/svo2":
				_data[2]._target=val;
				flip = _data[2]._target < _data[2]._val ? -1f : 1f;
				_svo2Arrow.localScale = new Vector3(1,flip,1);
				break;
			case "ECMO_Pump/pressure_dp":
				_data[3]._target=val;
				break;
			case "ECMO_Pump/pressure_ven":
				_data[4]._target=val;
				flip = _data[4]._target < _data[4]._val ? -1f : 1f;
				_pvenArrow.localScale = new Vector3(1,flip,1);
				break;
			case "ECMO_Pump/pressure_art":
				_data[5]._target=val;
				flip = _data[5]._target < _data[5]._val ? -1f : 1f;
				_partArrow.localScale = new Vector3(1,flip,1);
				break;
			case "ECMO_Pump/temperature_art":
				_data[6]._target=val;
				flip = _data[6]._target < _data[6]._val ? -1f : 1f;
				_tartArrow.localScale = new Vector3(1,flip,1);
				break;
			case "ECMO_Pump/hb":
				_data[7]._target=val;
				flip = _data[7]._target < _data[7]._val ? -1f : 1f;
				_hbArrow.localScale = new Vector3(1,flip,1);
				break;
		}
	}
}
