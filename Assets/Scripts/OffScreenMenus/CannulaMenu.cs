using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class CannulaMenu : OffscreenMenu
{
	Dictionary<int,string> _sites = new Dictionary<int,string>();
	Dictionary<int,string> _cannulas = new Dictionary<int,string>();

	int _curSite=-1;
	Dictionary<int,CircuitManager.CannulaN> _canSites = new Dictionary<int,CircuitManager.CannulaN>();
	Dictionary<int,Vector3> _siteDefaults = new Dictionary<int, Vector3>();
	Dictionary<int,Vector3> _siteDefaultRots = new Dictionary<int, Vector3>();
	public Transform[] _siteTransforms;
	public Transform _site0;
	public Transform _site3;
	public Transform _site7;
	public Transform [] _canPrefabs;
	CircuitManager _circuitMan;
	public Canvas _canSubMenuCanvas;
	public RectTransform _canSubMenu;
	public RectTransform _canDepthMenu;
	public RectTransform _canRotMenu;
	public Slider _depthSlider;
	public Slider _rotationSlider;
	float _minRot=-180f;
	float _maxRot=180f;
	float _lastRotTime;
	float _lastDepthTime;
	public RectTransform _depthBG;
	Text _depthText;
	public RectTransform _rotBG;
	Text _rotText;
	public Button _depthButton;
	public Button _rotButton;
	NavPanel _nav;
	Camera _myCam;
	Camera _origCam;
    // Start is called before the first frame update
    public override void Start()
    {
		base.Start();
		_myCam = GameObject.Find("DefaultCamera").GetComponent<Camera>();
		_origCam = _myCam;

		//lets use the Inihelper for this and clean up the Start method
		//read the ini for [sites] and [cannulas]
		string[] iniLines;
		string canPath = Application.streamingAssetsPath+"/Cannulas.ini";
		iniLines = File.ReadAllLines(canPath);
		bool atSites=false;
		foreach(string l in iniLines){
			if(!atSites && l=="[Sites]")
				atSites=true;
			else if(atSites){
				if(l=="")
					break;
				else
				{
					string[] pubParts = l.Split('=');
					int id=0;
					int.TryParse(pubParts[0].Trim(),out id);
					string site = pubParts[1].Trim();
					_sites.Add(id,site);
				}
			}
		}
		bool atCans=false;
		foreach(string l in iniLines){
			if(!atCans && l=="[Cannulas]")
				atCans=true;
			else if(atCans){
				if(l=="")
					break;
				else
				{
					string[] pubParts = l.Split('=');
					int id=0;
					int.TryParse(pubParts[0].Trim(),out id);
					string can = pubParts[1].Trim();
					_cannulas.Add(id,can);
				}
			}
		}
		_circuitMan = FindObjectOfType<CircuitManager>();

		HideSubMenu();
		ShowDepthMenu(false);
		ShowRotMenu(false);
		_depthText=_depthBG.GetChild(0).GetComponent<Text>();
		_rotText = _rotBG.GetChild(0).GetComponent<Text>();
		_nav = FindObjectOfType<NavPanel>();

    }
	
	public override void OpenMenu(int site){
		base.OpenMenu(site);
		_title.text = _sites[site];
		//set available cannulas to the subset based on site and circuit
		List<int> avail = _circuitMan.GetAvailableCannulas(site);
		_belt.SetRange(avail);
		//set conveyer built to aim at index
		if(_canSites.ContainsKey(site))
			_belt.SetSelected(_canSites[site].index);//this does assume that a cannula has been set...
		else
			_belt.SetSelected(0);
	}
	public void HideCanMenu(){
		ShowMenu(false);
	}

	public void ForceCannula(){
		int can = _belt.GetSelectedObject();
		SetCannula(_circuitMan._ecmoData.Cannulas[can],_curSite);
		ShowMenu(false);
	}

	//this method has some wack parameters
	//forceAnim and sameCan don't really work the way you'd think
	//The goal is simply to determine when to play the cannula insertion animation
	//vs. setting the depth and rotation
	public void SetCannula(CircuitManager.CannulaN can, int site, bool forceAnim=false, bool pub=true,
			bool resetDar=false){//resetDar is used to reset depth and rotation
		//note that in OR, and maybe some other cases, we don't have a circuit set up, and no cannulas
		//so when the site is not assigned, let's just skip this whole procedure
		if(site>=_siteTransforms.Length)
			return;
		bool sameCan=false;
		if(_canSites.ContainsKey(site))
		{
			//If replacing a cannula with the same cannula (this happens when changing depth / rotation)
			//Or when forcing a circuit change
			if(_canSites[site].index==can.index){
				sameCan=!forceAnim;
			}
			//preserve cannula depth and rotation unless previous cannula was an empty 
			//in which case the depth and rotation would be 5 nines
			if(_canSites[site].index==0||resetDar)
				_canSites[site].CopyDepthAndRotation(can);

			_canSites[site].CopyVals(can);
		}
		//only happens on init - initializes all can sites to default va arrangement
		else
		{
			_canSites.Add(site,can.Copy());
		}
		//set cannula at site
		if(site==0)
			SetTransformAndAnimate(0,_canPrefabs[can.index],sameCan);
		else if(site==3)
			SetTransformAndAnimate(3,_canPrefabs[can.index],sameCan);
		else if(site==7)
			SetTransformAndAnimate(7,_canPrefabs[can.index],sameCan);

		//send signal
		if(pub)
			_mqtt.ForceCannula(site,_canSites[site]);
	}

	public void PubCan(int site){
		_mqtt.ForceCannula(site,_canSites[site]);
	}

	void SetTransformAndAnimate(int sIndex, Transform prefab,bool sameC){
		if(_siteTransforms[sIndex]==null)
		{
			Debug.Log("oops");
			return;
		}
		if(!sameC){
			//clear existing cannula
			for(int i=_siteTransforms[sIndex].childCount; i>0; i--)
				Destroy(_siteTransforms[sIndex].GetChild(i-1).gameObject);
			//add new one
			Transform can = Instantiate(prefab,_siteTransforms[sIndex]);
			can.localPosition=Vector3.zero;
			can.localEulerAngles=Vector3.zero;
			if(_canSites[sIndex]._root==null)
				_canSites[sIndex]._root=_siteTransforms[sIndex];
			//add click listener
			ClickDetection cd = can.GetComponent<ClickDetection>();
			cd._onClick.AddListener(delegate{CanClicked(_siteTransforms[sIndex]);});
			//remove cap
			foreach(Transform t in can)
			{
				if(t.GetComponent<MeshRenderer>()!=null)
					t.gameObject.SetActive(false);
			}
			//animate
			StartCoroutine(InsertCannulaR(_canSites[sIndex],_siteTransforms[sIndex],sIndex));
		}
		else{
			UpdateCannulaDepthAndRotation(sIndex);
		}
	}

	public void ResetCannulaRoots(){
		for(int i=0; i<_siteTransforms.Length; i++){
			if(_siteTransforms[i]!=null)
			{
				if(_siteDefaults.ContainsKey(i))
					_siteDefaults[i]=_siteTransforms[i].position;
				else
					_siteDefaults.Add(i,_siteTransforms[i].position);
			}
		}
	}


	IEnumerator InsertCannulaR(CircuitManager.CannulaN can, Transform trans,int site){
		yield return new WaitForSeconds(.1f);
		
		//set position
		Vector3 endPos;
		if(!_siteDefaults.ContainsKey(site))
			_siteDefaults.Add(site,trans.position);
		if(can.length==0)
		{
			endPos=_siteDefaults[site];
		}
		else{
			float d = can._depth;
			float id = can.length;
			float dn = d/id;//dn is depth normalized
			//dn=1f;
			endPos = _siteDefaults[site]-trans.forward*(1-dn)*id;
		}
		Vector3 startPos=endPos-trans.forward*.1f;

		//set rotation
		float r = _canSites[site]._rotation;
		Vector3 localE = trans.localEulerAngles;
		localE.z=r;
		trans.localEulerAngles = localE;

		//animate
		float timer=0;
		float dur=2f;
		while(timer<dur){
			timer+=Time.deltaTime;
			trans.position = Vector3.Lerp(startPos,endPos,timer/dur);
			yield return null;
		}
		trans.position=endPos;
	}

	public void CanClicked(Transform t){
		if(_canSubMenuCanvas.enabled ||_can.enabled)
			return;
		float dst = (t.position-_myCam.transform.position).magnitude;
		//check distance - if greater than .5, move to position
		//if site is 7, move to cannula ij
		//else move to femeral can
		//
		//else open up the menu
		if(dst>0.6f)
		{
			int site=0;
			int.TryParse(""+t.name[t.name.Length-1],out site);
			//#todo probably don't want to hardcode these nav indices
			if(site==0 || site==3)
				_nav.NavigateByIndex(16);
			else if(site==7)
				_nav.NavigateByIndex(17);
		}
		else{
			int site=0;
			int.TryParse(""+t.name[t.name.Length-1],out site);
			_curSite=site;
			Vector3 screenPos = Camera.main.WorldToScreenPoint(t.position);
			Vector3 canPos = new Vector3(1920f*screenPos.x/(float)Screen.width,
					1080f*screenPos.y/(float)Screen.height,0);
			canPos.x-=1920*.5f;
			canPos.y-=1080*.5f;
			canPos.y+=_canSubMenu.sizeDelta.y;
			float xClamp = -1920*.5f+_canSubMenu.sizeDelta.x;
			float yClamp = -1080*.5f+_canSubMenu.sizeDelta.y;
			canPos.x = Mathf.Clamp(canPos.x,xClamp,-xClamp);
			canPos.y = Mathf.Clamp(canPos.y,yClamp,-yClamp);
			_canSubMenuCanvas.enabled=true;
			_testCam.enabled = false;
			_blocker.enabled=true;
			_canSubMenu.localPosition=canPos;
			_canDepthMenu.localPosition=canPos;
			_canRotMenu.localPosition=canPos;
			CanvasGroup cg = _canSubMenu.GetComponent<CanvasGroup>();
			cg.alpha=1f;
			cg.interactable=true;
			cg.blocksRaycasts=true;
			//set depth and rotation buttons enabled only if cur site is 7 and cannula is not none
			bool drEnabled=_curSite==7&&_canSites.ContainsKey(_curSite) && _canSites[_curSite].index!=0;
			_depthButton.interactable=drEnabled;
			_rotButton.interactable=drEnabled;
		}
	}

	public void HideSubMenu(){
		_canSubMenuCanvas.enabled=false;
		CanvasGroup cg = _canSubMenu.GetComponent<CanvasGroup>();
		cg.alpha=0f;
		cg.interactable=false;
		cg.blocksRaycasts=false;
		_testCam.enabled = true;
		_blocker.enabled=false;
		ShowDepthMenu(false);
		ShowRotMenu(false);
	}

	public void ShowDepthMenu(bool show){
		CanvasGroup cg = _canDepthMenu.GetComponent<CanvasGroup>();
		if(!show&&cg.alpha==1f){
			if(_canSites.ContainsKey(_curSite)){
				float newd = _depthSlider.value*_canSites[_curSite].length;
				float oldd = _canSites[_curSite]._depth;
				if(Mathf.Abs(newd-oldd)>0.001f){
					_canSites[_curSite]._depth=newd;
					_mqtt.ForceCannula(_curSite,_canSites[_curSite]);
				}
			}
		}
		_canSubMenuCanvas.enabled=show;
		cg.alpha=show ? 1: 0;
		cg.interactable=show;
		cg.blocksRaycasts=show;
		_testCam.enabled = !show;
		_blocker.enabled = show;
	}

	public void ShowRotMenu(bool show){
		CanvasGroup cg = _canRotMenu.GetComponent<CanvasGroup>();
		if(!show&&cg.alpha==1f){
			if(_canSites.ContainsKey(_curSite)){
				//check rotation
				float newr = Mathf.Lerp(_minRot,_maxRot,_rotationSlider.value);
				float oldr = _canSites[_curSite]._rotation;
				if(Mathf.Abs(newr-oldr)>0.001f){
					_canSites[_curSite]._rotation=newr;
					_mqtt.ForceCannula(_curSite,_canSites[_curSite]);
				}
			}
		}
		_canSubMenuCanvas.enabled=show;
		cg.alpha=show ? 1: 0;
		cg.interactable=show;
		cg.blocksRaycasts=show;
		_testCam.enabled = !show;
		_blocker.enabled = show;
	}

	public void ShowCanSelectionMenu(){
		HideSubMenu();
		OpenMenu(_curSite);
	}

	public void ShowCanDepthMenu(){
		HideSubMenu();
		ShowDepthMenu(true);
		//get depth for cannula
		CircuitManager.CannulaN can = _canSites[_curSite];
		float d = can._depth;
		float id = can.length;
		float dn = d/id;
		_depthSlider.value=dn;
		//set cannula depth
		can._root.position = _siteDefaults[_curSite]-can._root.forward*(1-dn)*can.length;
	}

	public void DepthChange(){
		CircuitManager.CannulaN can = _canSites[_curSite];
		//set cannula depth
		can._root.position = _siteDefaults[_curSite]-can._root.forward*(1-_depthSlider.value)*can.length;
		float newd = _depthSlider.value*_canSites[_curSite].length;
		if(Time.time-_lastDepthTime>=.1f){
			
			float oldd = _canSites[_curSite]._depth;
			if(Mathf.Abs(newd-oldd)>0.001f){
				_canSites[_curSite]._depth=newd;
				_mqtt.ForceCannula(_curSite,_canSites[_curSite]);
			}
			_lastDepthTime=Time.time;
		}
		Vector3 pos = _depthBG.position;
		pos.x=_depthSlider.handleRect.position.x;
		_depthBG.position=pos;
		_depthText.text=(newd*100f).ToString("0.0 cm");
	}

	void UpdateCannulaDepthAndRotation(int sIndex){
		CircuitManager.CannulaN can = _canSites[sIndex];
		Vector3 defaultPos;
		if(_siteDefaults.ContainsKey(sIndex))
			defaultPos=_siteDefaults[sIndex];
		else
		{
			defaultPos=can._root.position;
			_siteDefaults.Add(sIndex,defaultPos);
		}
		can._root.position = defaultPos-can._root.forward*(can.length-can._depth);
		Vector3 localE = can._root.localEulerAngles;
		float r = can._rotation;
		localE.z=r;
		can._root.localEulerAngles = localE;
	}

	public void ShowCanRotMenu(){
		HideSubMenu();
		ShowRotMenu(true);
		//get rotation stuff
		CircuitManager.CannulaN can = _canSites[_curSite];
		float r = can._rotation;
		_rotationSlider.value=Mathf.InverseLerp(_minRot,_maxRot,r);
		Vector3 localE = can._root.localEulerAngles;
		localE.z=r;
		can._root.localEulerAngles = localE;
	}

	public void RotChange(){
		CircuitManager.CannulaN can = _canSites[_curSite];
		Vector3 localE = can._root.localEulerAngles;
		float r = Mathf.Lerp(_minRot,_maxRot,_rotationSlider.value);
		localE.z=r;
		can._root.localEulerAngles = localE;
		float newr = Mathf.Lerp(_minRot,_maxRot,_rotationSlider.value);
		if(Time.time-_lastRotTime>=.1f){
			float oldr = _canSites[_curSite]._rotation;
			if(Mathf.Abs(newr-oldr)>0.001f){
				_canSites[_curSite]._rotation=newr;
				_mqtt.ForceCannula(_curSite,_canSites[_curSite]);
			}
			_lastRotTime=Time.time;
		}
		Vector3 pos = _rotBG.position;
		pos.x=_rotationSlider.handleRect.position.x;
		_rotBG.position=pos;
		_rotText.text=(newr).ToString("0.0 d");
	}

	public void SetAltCamera(Camera c){
		if(c==null)
			_myCam=_origCam;
		else
			_myCam=c;
	}

	public bool HasCannulaAtSite(int site){
		return _canSites.ContainsKey(site) && _canSites[site].index!=0;
	}

	public int GetCannulaAtSite(int site){
		return _canSites[site].index;
	}
}
