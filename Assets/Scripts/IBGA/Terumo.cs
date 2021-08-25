using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Terumo : MonoBehaviour
{
	bool _tempMode;
	public Text []_atText;
	public Text []_thirtySevenText;
	public Text [] _tempModeText;
	public CanvasGroup _art550;
	public CanvasGroup _artVen550;
	public CanvasGroup _art500;
	public CanvasGroup _artVen500;
	public Image _progress;
	float _timer=0;
	//data
	public MFloat _postPh;
	public MFloat _postPco2;
	public MFloat _postPo2;
	public MFloat _tempArt;
	public MFloat _tempVen;
	public MFloat _postSo2;
	public MFloat _hco3;
	public MFloat _be;
	public MFloat _kPlus;
	public MFloat _vo2;
	public MFloat _prePh;
	public MFloat _prePco2;
	public MFloat _prePo2;
	public MFloat _do2;
	public MFloat _hct;
	public MFloat _preSo2;
	public MFloat _hgb;
	public MFloat _flowRate;
	List<MFloat> _mFloats = new List<MFloat>();

	public GameObject[] _artVenCircuit;
	public GameObject[] _artCircuit;
	public Transform _venCuvetteTarget;
	public Transform _venCuvetteDefault;
	public Transform _venCuvette;
	public Transform _venHead;
	Vector3 _venHeadLocalPos;
	public RawImage _label500;
	public RawImage _label550;
	[Tooltip("0 = 500, 1 = 550")]
	public int _model;
	int _modules;
	//bool _started=false;
	public GameObject _calcShield;
	EcmoCart _cart;

	MyMQTT _mqtt;

	void OnDisable(){
		/*
		foreach(GameObject go in _artVenCircuit)
			go.SetActive(false);
		foreach(GameObject go in _artCircuit)
			go.SetActive(false);
		_venCuvette.GetComponent<MeshRenderer>().enabled=false;
		*/
	}
	void OnEnable(){
		/*
		if(_started)
			SetModules(_modules);
			*/
	}

    // Start is called before the first frame update
    void Start()
    {
		_mFloats.Add(_postPh);
		_mFloats.Add(_postPco2);
		_mFloats.Add(_postPo2);
		_mFloats.Add(_tempArt);
		_mFloats.Add(_tempVen);
		_mFloats.Add(_postSo2);
		_mFloats.Add(_hco3);
		_mFloats.Add(_be);
		_mFloats.Add(_kPlus);
		_mFloats.Add(_vo2);
		_mFloats.Add(_prePh);
		_mFloats.Add(_prePco2);
		_mFloats.Add(_prePo2);
		_mFloats.Add(_do2);
		_mFloats.Add(_hct);
		_mFloats.Add(_preSo2);
		_mFloats.Add(_hgb);
		_mFloats.Add(_flowRate);
		_venHeadLocalPos = _venHead.localPosition;

		MyMQTT [] qts = FindObjectsOfType<MyMQTT>();
		foreach(MyMQTT qt in qts){
			if(qt.gameObject.tag=="GameController")
				_mqtt=qt;
		}

		//init hardware setup
		SetModel();
		SetModules(1);
		SetValid(true);
		_tempMode=false;
		ToggleTempMode(false);//toggle to actual
		//_started=true;

		_cart = FindObjectOfType<EcmoCart>();
		SyncVals(_cart._ibgaData);
    }

    // Update is called once per frame
    void Update()
    {
		_timer+=Time.deltaTime;
		if(_timer>2f)
			_timer=0;
		_progress.fillAmount=_timer*.5f;
		foreach(MFloat mf in _mFloats)
			mf.Update();
    }

	public void SetModel(){
		//temp code
		_label500.enabled=_model==0;
		_label550.enabled=_model==1;
		//if(index==0 || index==1)
		//	_model=index;
		//gotta reset the modules because that controls the ui canvas
		SetModules(_modules);
	}

	public void ToggleTempMode(bool pub){
		_tempMode=!_tempMode;
		foreach(Text t in _atText)
			t.color=_tempMode? Color.black : Color.grey;
		foreach(Text t in _thirtySevenText)
			t.color = _tempMode ? Color.grey : Color.black;
		foreach(Text t in _tempModeText)
			t.text= _tempMode ? "Actual" : _thirtySevenText[0].text;
		if(pub)
			_mqtt.UpdateIbgaTempMode(_tempMode? 1 : 0);
	}

	public void SetTempMode(bool mode){
		_tempMode=!mode;
		ToggleTempMode(false);
	}


	public void SetModules(int index){
		if(index<0 || index>1)
			return;
		_modules=index;
		//this is super temp code
		_art500.alpha = index==0 && _model==0? 1:0;
		_artVen500.alpha = index==1 && _model==0? 1:0;
		_art550.alpha = index==0 && _model==1? 1:0;
		_artVen550.alpha = index==1 && _model==1? 1:0;

		//if modules == 1, venous cuvette is in place, else it's at default
		Transform tmp = index==1? _venCuvetteTarget : _venCuvetteDefault;
		_venCuvette.position=tmp.position;
		_venCuvette.rotation=tmp.rotation;

		//circuites
		/*
		foreach(GameObject go in _artVenCircuit)
			go.SetActive(index==1);
		foreach(GameObject go in _artCircuit)
			go.SetActive(index==0);
		Transform tmp = index==1? _venCuvetteTarget : _venCuvetteDefault;
		_venCuvette.position=tmp.position;
		_venCuvette.rotation=tmp.rotation;
		_venCuvette.GetComponent<MeshRenderer>().enabled=index==1;

		StopAllCoroutines();
		if(gameObject.activeSelf)
			StartCoroutine(SnapInCuvette(_venHead,_venHeadLocalPos,Vector3.back*.1f,index==1));
			*/
	}

	IEnumerator SnapInCuvette(Transform head,Vector3 defaultOffset, Vector3 offset,bool audio){
		//head.localPosition=defaultOffset;
		head.localPosition=defaultOffset+offset;
		//yield return null;
		float timer=0;
		while(timer<1f){
			timer+=Time.deltaTime;
			head.localPosition=Vector3.Lerp(defaultOffset+offset,defaultOffset,timer);
			yield return null;
		}
		/*
		if(audio)
			_venCuvetteTarget.GetComponent<AudioSource>().Play();
			*/
	}

	public void SyncVals(EcmoCart.IbgaData id){
		_postPh._target=id._postPh;
		_postPco2._target=id._postPco2;
		_postPo2._target=id._postPo2;
		_tempArt._target=id._tempArt;
		_tempVen._target=id._tempVen;
		_postSo2._target=id._postSo2;
		_hco3._target=id._hco3;
		_be._target=id._be;
		_kPlus._target=id._kPlus;
		_vo2._target=id._vo2;
		_prePh._target=id._prePh;
		_prePco2._target=id._prePco2;
		_prePo2._target=id._prePo2;
		_do2._target=id._do2;
		_hct._target=id._hct;
		_preSo2._target=id._preSo2;
		_hgb._target=id._hgb;
		_flowRate._target=id._flowRate;
	}

	public void SetValid(bool v){
		_calcShield.SetActive(!v);
	}
}
