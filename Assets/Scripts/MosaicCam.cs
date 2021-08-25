using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MosaicCam : MonoBehaviour
{
	float _maxFov=45;
	float _minFov=25;
	Slider _zoom;
	Camera _cam;
	Dropdown _drop;
	NavPanel _nav;
	public int _viewIndex;
	//Cardiohelp _cardiohelp;
	Alaris _alaris;
	Transform _drager;
	Invos _invos;
	GetingeHu _getinge;
	CannulaMenu _cans;
	EcmoCart _ecmo;

	public LayerMask[] _layerMasks;
    // Start is called before the first frame update
    void Start()
    {
		_drager = GameObject.Find("DragerVentilator").transform;
		_cam=GetComponent<Camera>();
		_zoom = GetComponentInChildren<Slider>();
		_zoom.value=Mathf.InverseLerp(_maxFov,_minFov,_cam.fieldOfView);
		_drop=GetComponentInChildren<Dropdown>();
		_drop.options.Clear();
		_drop.onValueChanged.AddListener(delegate{ChangeView();});
		_nav = FindObjectOfType<NavPanel>();
		_nav.OnNavsChanged+=UpdateOptions;
		UpdateOptions(null);
		//_cardiohelp = FindObjectOfType<Cardiohelp>();
		_alaris = FindObjectOfType<Alaris>();
		_invos = FindObjectOfType<Invos>();
		_getinge = FindObjectOfType<GetingeHu>();
		_cans = FindObjectOfType<CannulaMenu>();
    }

	void OnEnable(){
		ChangeView();
	}

	void UpdateOptions(NavPanel.NavEventArgs nargs){
		//clear old options
		_drop.options.Clear();
		foreach(NavPanel.NavOption nopt in _nav._navOptions){
			//add option in dropdown
			Dropdown.OptionData foo = new Dropdown.OptionData();
			foo.text=nopt._navButtonText.text;
			_drop.options.Add(foo);
		}
		_drop.value=_viewIndex;
		_drop.captionText.text=_drop.options[_drop.value].text;
	}

	void ChangeView(){
		if(_nav==null)
			return;
		if(_ecmo==null)
			_ecmo = FindObjectOfType<EcmoCart>();
		NavPanel.NavOption foo = _nav.GetCoords(_drop.value);
		transform.position=foo._position;
		transform.eulerAngles=foo._eulers;
		//transform.position=_nav._navOptions[_drop.value]._position;
		//transform.eulerAngles=_nav._navOptions[_drop.value]._eulers;
		_zoom.value = 0;
		if(_drop.value<_layerMasks.Length)
			_cam.cullingMask=_layerMasks[_drop.value];
		//special cases fun
		switch(_drop.value){
			case 2://pump controller
				EcmoPump pump = _ecmo._pump;
				pump.transform.GetComponentInChildren<Knob>().SetAltCamera(_cam);
				pump.transform.GetComponentInChildren<ClickableRange>().SetAltCamera(_cam);
				break;
			case 3://gas blender
				Transform gasBlender = _ecmo._gasBlender;
				Knob [] knobs = gasBlender.GetComponentsInChildren<Knob>();
				foreach(Knob k in knobs)
					k.SetAltCamera(_cam);
				break;
			case 5://Ventilator
				foreach(Canvas c in _drager.GetComponentsInChildren<Canvas>())
					c.worldCamera=_cam;
				break;
			case 7://oximeter
				_invos.transform.GetComponentInChildren<ClickableRange>().SetAltCamera(_cam);
				break;
			case 9://heater cooler
				//#todo look into this wackiness
				ClickDetection [] cds = _getinge.GetComponentsInChildren<ClickDetection>();
				foreach(ClickDetection cd in cds)
					cd.enabled=true;
				break;
			case 11://infusion pump
				ClickableRange [] crs = _alaris.GetComponentsInChildren<ClickableRange>();
				foreach(ClickableRange cr in crs)
					cr.SetAltCamera(_cam);
				foreach(Canvas c in _alaris.GetComponentsInChildren<Canvas>())
					c.worldCamera=_cam;
				break;
			case 12://iv bag
				ClickDetection [] cds2 = _alaris.GetComponentsInChildren<ClickDetection>();
				foreach(ClickDetection c in cds2)
					c.enabled=true;
				break;
			case 16://cannulas
			case 17:
				GameObject [] cans = GameObject.FindGameObjectsWithTag("Cannula");
				foreach(GameObject go in cans){
					go.GetComponent<ClickDetection>().enabled=true;
				}
				_cans.SetAltCamera(_cam);
				break;
			case 18://hoffman
				GameObject hoff = GameObject.FindGameObjectWithTag("Hoffman");
				if(hoff!=null){
					ClickDetection [] cds3 = hoff.transform.GetComponentsInChildren<ClickDetection>();
					foreach(ClickDetection c3 in cds3)
						c3.enabled=true;
					Knob k = hoff.transform.GetComponentInChildren<Knob>();
					k.SetAltCamera(_cam);
				}
				else
					FindObjectOfType<CameraManager>()._hoffAlt=_cam;
				break;
			default:
				break;
		}

	}

    // Update is called once per frame
    void Update()
    {
	}

	public void Zoom(){
		//Debug.Log(_zoom.value);
		if(_cam!=null){
			_cam.fieldOfView=Mathf.Lerp(_maxFov,_minFov,_zoom.value);
		}
	}
}
