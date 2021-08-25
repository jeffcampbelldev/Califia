using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Clipboard : MonoBehaviour
{
	public Transform _tabPrefab;
	public Transform _tabParent;
	BoxCollider _blocker;
	ClipboardHelper _helper;
	bool _init;
	public LayerMask _mouseLayer;
	Camera _handCam;
	ItemStand _stand;
	public RawImage _main;
	float _aspect;
	Vector2 _maxDim;
	TestCam _cam;
	float _defaultFov;
	float _maxFov=60f;
	Vector3 _originalPos;
	int _swipeState=0;
	Vector3 _swipeCur;
	Vector3 _swipeStart;
	//public float _swipeThresh;
	[HideInInspector]
	public int _lastTab;
	public Button _prev;
	public Button _next;
	public Dropdown _dropdown;
	public Slider _slider;
    // Start is called before the first frame update
    void Start()
    {
    }

	public void Init(){
		float tabHeight = (_tabParent.GetComponent<RectTransform>().sizeDelta.y*.8f)/(float)_helper._numTabs;
		for(int i=0; i<_helper._numTabs; i++){
			Transform t = Instantiate(_tabPrefab,_tabParent);
			RectTransform tabRect = t.GetComponent<RectTransform>();
			Vector2 sz = tabRect.sizeDelta;
			tabRect.sizeDelta = new Vector2(sz.x,tabHeight);
			t.GetChild(0).GetComponent<RectTransform>().sizeDelta=tabRect.sizeDelta;
			t.GetComponent<RawImage>().color=new Color(0,0,0,0);//Color.HSVToRGB((float)i/5f,1,.75f);
			t.GetComponent<Button>().onClick.AddListener(delegate {TabClicked(t);});
		}
		_handCam = GameObject.FindGameObjectWithTag("HandCamera").GetComponent<Camera>();
		GetComponent<Canvas>().worldCamera=_handCam;
		ItemStand[] stands = FindObjectsOfType<ItemStand>();
		foreach(ItemStand i in stands){
			if(i._item._name=="clipboard")
				_stand=i;
		}
		_maxDim=_main.GetComponent<RectTransform>().sizeDelta;
		_aspect = _maxDim.x/_maxDim.y;
		_cam=FindObjectOfType<TestCam>();
		_defaultFov = _handCam.fieldOfView;
		_swipeState=0;
		_lastTab=-1;
		_originalPos = _handCam.transform.localPosition;
		_dropdown.options.Clear();
		_dropdown.gameObject.SetActive(false);
		_init=true;
	}

	void OnEnable(){
		_helper = FindObjectOfType<ClipboardHelper>();
		_helper._curClip=this;
		_blocker = Camera.main.transform.Find("Blocker").GetComponent<BoxCollider>();
		_blocker.enabled=true;
		if(!_init)
			Init();
		UpdateTabs();
		_cam.enabled=false;
		_handCam.fieldOfView=_defaultFov;
		if(_helper._lastTab!=-1){
			//render that bad boy
			TabClicked(_helper._lastTab);
		}
		_swipeState=0;
	}

	void OnDisable(){
		_blocker.enabled=false;
		_cam.enabled=true;
		_handCam.fieldOfView=_defaultFov;
		_handCam.transform.localPosition = _originalPos;
	}

	//Utilize CH data to render tab buttons
	//Called by CH via MQTT
	public void UpdateTabs(){
		for(int i=0; i<_tabParent.childCount; i++){
			_helper.UpdateTab(_tabParent.GetChild(i),i);//this calls Setup
		}
	}

	public void TabClicked(Transform t){
		int index = t.GetSiblingIndex();
		Debug.Log("Clicked tab: "+index);
		_helper._lastTab=index;
		_lastTab=index;
		_helper.RenderTab(index,this);
	}

	public void TabClicked(int index){
		Debug.Log("Clicked tab: "+index);
		_helper._lastTab=index;
		_lastTab=index;
		Debug.Log("last tab = "+_lastTab);
		//_helper._tabs[index].Reload();
		_helper.RenderTab(index,this);
	}

	public void RotateForm(int dir){
		Debug.Log("last tab = "+_lastTab);
		if(_lastTab!=-1)
		{
			Debug.Log("Swiping form");
			_helper.SwipeForm(_lastTab,dir);
			_helper.RenderTab(_lastTab,this);
		}
	}

	public void GotoForm(){
		if(_lastTab!=-1)
		{
			_helper.SetForm(_lastTab,_dropdown.value);
			_helper.RenderTab(_lastTab,this);
		}
	}

	public void SetTex(Texture2D tex){
		if(!gameObject.activeInHierarchy)
			return;
		StopAllCoroutines();
		StartCoroutine(FormChangeR(tex));
	}

	IEnumerator FormChangeR(Texture2D tex){
		float timer=0;
		float dur=0.15f;
		while(timer<dur){
			timer+=Time.deltaTime;
			_main.color=Color.white*Mathf.Lerp(1,0.5f,timer/dur);
			yield return null;
		}
		ScaleTexture(tex.width,tex.height);
		_main.texture=tex;
		timer=0;
		while(timer<dur){
			timer+=Time.deltaTime;
			_main.color=Color.white*Mathf.Lerp(0.5f,1f,timer/dur);
			yield return null;
		}
		_main.color=Color.white;
	}

	public void ClearTex(){
		_main.texture=null;
	}

	void ScaleTexture(int w, int h){
		Vector2 rawRes = new Vector2(w,h);
		float rawAspect = rawRes.x/rawRes.y;
		//source file is wider than target display
		if(rawAspect>=_aspect){
			_main.GetComponent<RectTransform>().sizeDelta = new Vector2(_maxDim.x,_maxDim.x/rawAspect);
		}
		//source file is taller than target display
		else{
			_main.GetComponent<RectTransform>().sizeDelta = new Vector2(_maxDim.y*rawAspect,_maxDim.y);
		}
	}

	public void PutAway(){
		_stand.ReturnItem();
	}

	public void ZoomIn(){
		_handCam.fieldOfView-=5;
		Debug.Log(_handCam.fieldOfView);
		_handCam.fieldOfView=Mathf.Min(_maxFov,_handCam.fieldOfView);
	}

	public void ZoomOut(){
		_handCam.fieldOfView+=5;
		_handCam.fieldOfView=Mathf.Max(30,_handCam.fieldOfView);
	}

	public void Update(){
		RaycastHit hit;
		Vector3 ray = (Vector3)Input.mousePosition;

		//clamp on
		if(!Physics.Raycast(_handCam.transform.position,_handCam.ScreenPointToRay(ray).direction,out hit,1f,_mouseLayer)){
			if(Input.GetMouseButtonDown(0)){
				PutAway();
			}
		}
		_handCam.fieldOfView-=Input.mouseScrollDelta.y;
		_handCam.fieldOfView=Mathf.Clamp(Mathf.Min(_maxFov,_handCam.fieldOfView), 30, _maxFov);
		switch(_swipeState){
			case 0:
				if(Input.GetMouseButtonDown(0))
				{
					_swipeState=1;
					_swipeStart=Input.mousePosition;
				}
				break;
			case 1:
				if(Input.GetMouseButtonUp(0)){
					_swipeState=0;
				}
				else{
					_swipeCur=Input.mousePosition;
					Vector3 change = _handCam.ScreenToViewportPoint(_swipeCur - _swipeStart);

					//bounds checking
					if ( (_handCam.transform.localPosition.x < (_originalPos.x - .2) && change.x > 0)
						|| (_handCam.transform.localPosition.x > (_originalPos.x + .15) && change.x < 0))
						change.x = 0f;

					if ((_handCam.transform.localPosition.y < (_originalPos.y - .1) && change.y > 0)
						|| (_handCam.transform.localPosition.y > (_originalPos.y + .1) && change.y < 0))
						change.y = 0f;

					_handCam.transform.Translate(change * -.007f, Space.Self);
					
				}
				break;
			case 2:
				if(Input.GetMouseButtonUp(0)){
					_swipeState=0;
				}
				break;
		}
	}
}
