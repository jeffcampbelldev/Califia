using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ClampMenu : MonoBehaviour
{
	Tube _tuber;
	Canvas _can;
	//CanvasGroup _menuMain;
	CanvasGroup _menuAdjust;
	public Slider _adjustSlider;
	public Text _valDisplay;

    // Start is called before the first frame update
    void Start()
    {
		_can=GetComponent<Canvas>();
		_can.enabled=false;
		//_menuMain=transform.GetChild(1).GetComponent<CanvasGroup>();
		_menuAdjust=transform.Find("ClampAdjustMenu").GetComponent<CanvasGroup>();
		//_menuMain.alpha=0;
		_menuAdjust.alpha=0;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

	public void ActivateOnTube(Tube t){
		_can.enabled=true;
		//ActivateCanvasGroup(_menuMain,true);
		//ActivateCanvasGroup(_menuAdjust,true);
		AdjustClamp();
		_tuber=t;
		//position 
		Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.parent.position);
		Vector3 canPos = new Vector3(1920f*screenPos.x/(float)Screen.width,
				1080f*screenPos.y/(float)Screen.height,0);
		canPos.x-=1920*.5f;
		canPos.y-=1080*.5f;
		RectTransform rect = _menuAdjust.GetComponent<RectTransform>();
		canPos.y+=rect.sizeDelta.y;
		float xClamp = -1920*.5f+rect.sizeDelta.x;
		float yClamp = -1080*.5f+rect.sizeDelta.y;
		canPos.x = Mathf.Clamp(canPos.x,xClamp,-xClamp);
		canPos.y = Mathf.Clamp(canPos.y,yClamp,-yClamp);
		//_menuMain.transform.localPosition=canPos;
		_menuAdjust.transform.localPosition=canPos;
		if(TestCam._useController)
			_adjustSlider.Select();
	}

	public void HideMenu(){
		_tuber.HideClampMenu(transform.parent);
		_can.enabled=false;
	}

	public void RemoveClamp(){
		_tuber.RemoveClamp(transform.parent);
		HideMenu();
	}

	public void AdjustClamp(){
		//ActivateCanvasGroup(_menuMain,false);
		ActivateCanvasGroup(_menuAdjust,true);
		_valDisplay.text=(_adjustSlider.value/5f).ToString("00%");
	}

	void ActivateCanvasGroup(CanvasGroup cg, bool ac){
		cg.alpha = ac? 1f: 0f;
		cg.interactable=ac;
		cg.blocksRaycasts=ac;
	}

	public void AdjustSliderUpdate(){
		float v = (_adjustSlider.value/5f);
		_valDisplay.text=v.ToString("00%");
		if(_tuber!=null)
			_tuber.AdjustClamp(transform.parent,v);
	}

	public void ResetClampMenu(){
		_adjustSlider.value=_adjustSlider.minValue;
	}
}
