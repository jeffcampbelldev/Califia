using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HemofilterMenu : MonoBehaviour
{
	Canvas _can;
	CanvasGroup _menu;
    // Start is called before the first frame update
    void Start()
    {
		_can=GetComponent<Canvas>();
		_can.enabled=false;
		_menu = GetComponentInChildren<CanvasGroup>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

	public void ShowMenu(){
		_can.enabled=true;
		//Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.parent.position);
		//Vector3 screenPos = Camera.main.S
		Vector3 screenPos = Input.mousePosition;
		Vector3 canPos = new Vector3(1920f*screenPos.x/(float)Screen.width,
				1080f*screenPos.y/(float)Screen.height,0);
		canPos.x-=1920*.5f;
		canPos.y-=1080*.5f;
		RectTransform rect = _menu.GetComponent<RectTransform>();
		canPos.y+=rect.sizeDelta.y;
		float xClamp = -1920*.5f+rect.sizeDelta.x;
		float yClamp = -1080*.5f+rect.sizeDelta.y;
		canPos.x = Mathf.Clamp(canPos.x,xClamp,-xClamp);
		canPos.y = Mathf.Clamp(canPos.y,yClamp,-yClamp);
		_menu.transform.localPosition=canPos;

	}

	public void HideMenu(){
		_can.enabled=false;
	}
}
