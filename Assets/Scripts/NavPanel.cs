//NavPanel.cs
//
//Description: Manages navigation hot spots or quickNavs
//Handles the gui elements as well as storing cam position / rotations
//Uses RoomConfig.cs to read/write to file
//Uses TestCam.cs for camera motion
//

//

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class NavPanel : MonoBehaviour
{
	float _targY;
	BoxCollider _blocker;
	public TouchJoystick[] _disablable;
 	public Transform _layout;
 	public Transform _secondLayout;
	public Transform _itemPrefab;
	public GameObject _clickAway;
	TestCam _cam;
	[HideInInspector]
	public List<NavOption> _navOptions = new List<NavOption>();
	List<NavOption> _scenNavOptions = new List<NavOption>();
	List<NavOption> _recents = new List<NavOption>();
	const int _maxSpots = 23;
	bool _opened;
	MyMQTT _mqtt;
	[HideInInspector]
	public int _lockedNavs=-1;
	int _maxRecents;
	public Transform _recentsParent;
	List<RecentNav> _recentNavs = new List<RecentNav>();
	EcmoCart _cart;

	public struct RecentNav{
		public Button _butt;
		public Text _label;
		public int _index;
	}

	public class NavOption{

		public Transform _trans;
		public Button _navButton;
		public Text _navButtonText;
		public Button _editButton;
		public Button _renameButton;
		public Button _deleteButton;
		public GameObject _inFieldGo;
		public InputField _inField;
		public GameObject _popUp;
		public Vector3 _position;
		public Vector3 _eulers;
		public bool _fromScenario;

		public NavOption(){
		}

		public NavOption(Transform t){
			_trans=t;
			_navButton = t.GetChild(0).GetComponent<Button>();
			_navButtonText = t.GetChild(0).GetComponent<Text>();
			_editButton = t.GetChild(1).GetComponent<Button>();
			_inFieldGo = t.GetChild(2).gameObject;
			_popUp = t.GetChild(3).gameObject;
			_popUp.SetActive(false);
			_renameButton = _popUp.transform.GetChild(0).GetComponent<Button>();
			_deleteButton = _popUp.transform.GetChild(1).GetComponent<Button>();
			_inField = _inFieldGo.GetComponent<InputField>();
			_inFieldGo.SetActive(false);
			Transform mainC = Camera.main.transform;
			_position = mainC.position;
			_eulers = mainC.eulerAngles;
			_fromScenario=false;
		}
		public NavOption(Transform t, string name, Vector3 pos, Vector3 eul){
			_trans=t;
			_navButton = t.GetChild(0).GetComponent<Button>();
			_navButtonText = t.GetChild(0).GetComponent<Text>();
			_navButtonText.text=name;
			_editButton = t.GetChild(1).GetComponent<Button>();
			_inFieldGo = t.GetChild(2).gameObject;
			_popUp = t.GetChild(3).gameObject;
			_popUp.SetActive(false);
			_renameButton = _popUp.transform.GetChild(0).GetComponent<Button>();
			_deleteButton = _popUp.transform.GetChild(1).GetComponent<Button>();
			_inField = _inFieldGo.GetComponent<InputField>();
			_inFieldGo.SetActive(false);
			_position = pos;
			_eulers = eul;
			_fromScenario=false;
		}
		public string Serialize(){

			CultureInfo cultureInfo = CultureInfo.InvariantCulture;
			string ser = _navButtonText.text+","+_position.x.ToString(cultureInfo)+","+_position.y.ToString(cultureInfo)+","+_position.z.ToString(cultureInfo)+","+_eulers.x.ToString(cultureInfo)+","+_eulers.y.ToString(cultureInfo);
			return ser;
		}
	}

	//event when a nav option is edited / removed / created
	public delegate void EventHandler(NavEventArgs nargs);
	public event EventHandler OnNavsChanged;
	public class NavEventArgs : System.EventArgs{
		public bool fromScenario;
	}

	public UnityEvent onNavHide;
	public UnityEvent onNavShow;

	//this tracks where the add view button is
	//used to know where to insert new views
	int _addButtonIndex;

	VerticalLayoutGroup _vlg;

    // Start is called before the first frame update
    void Start()
    {
		_cam = FindObjectOfType<TestCam>();
		_blocker=_cam.transform.Find("Blocker").GetComponent<BoxCollider>();
		_clickAway.SetActive(false);
		_cart = FindObjectOfType<EcmoCart>();

		MyMQTT [] qts = FindObjectsOfType<MyMQTT>();
		foreach(MyMQTT qt in qts){
			if(qt.gameObject.tag=="GameController")
				_mqtt=qt;
		}

		//recent nav options setup
		Button[] butts = _recentsParent.GetComponentsInChildren<Button>();
		int index=0;
		foreach(Button b in butts){
			RecentNav rn = new RecentNav();
			rn._label = b.transform.GetChild(0).GetComponent<Text>();
			rn._butt = rn._label.transform.parent.GetComponent<Button>();
			_recentNavs.Add(rn);
			rn._index=index;

			b.onClick.AddListener(delegate{GotoRecent(rn);});
			index++;
		}
		_maxRecents=butts.Length;

		_vlg = transform.parent.GetComponent<VerticalLayoutGroup>();

		NavigateByIndex(0);
		
		//drag threshold
		int defaultValue = EventSystem.current.pixelDragThreshold;
         EventSystem.current.pixelDragThreshold = Mathf.Max(defaultValue, (int) (defaultValue * Screen.dpi / 160f));

    }

	void Update(){
		if(Input.GetKeyDown(KeyCode.Alpha1)){
			if(_cam.enabled)
				GotoRecent(0);
		}
		if(Input.GetKeyDown(KeyCode.Alpha2)){
			if(_cam.enabled)
				GotoRecent(1);
		}
		if(Input.GetKeyDown(KeyCode.Alpha3)){
			if(_cam.enabled)
				GotoRecent(2);
		}
		if(Input.GetKeyDown(KeyCode.Alpha4)){
			if(_cam.enabled)
				GotoRecent(3);
		}
		if(Input.GetButtonDown("Navigate")){
			if(_cam.enabled){
				_cam.enabled=false;
				Debug.Log("User wants to navigate");
				_recentNavs[0]._butt.Select();
			}
		}
		if(Input.GetButtonDown("Cancel")){
			GameObject sel = EventSystem.current.currentSelectedGameObject;
			if(sel!=null){
				foreach(RecentNav rn in _recentNavs){
					if(sel==rn._butt.gameObject)
					{
						EventSystem.current.SetSelectedGameObject(null);
						_cam.enabled=true;
						break;
					}
				}
			}
		}
	}

	void ShowPopup(NavOption nOpt){
		HidePopups();
		nOpt._popUp.SetActive(true);
	}

	void HidePopup(NavOption nOpt){
		nOpt._popUp.SetActive(false);
	}

	public void HidePopups(){
		foreach(NavOption no in _navOptions){
			HidePopup(no);
		}
		foreach(NavOption no in _scenNavOptions){
			HidePopup(no);
		}
	}

	void EditOption(NavOption nOpt){
		HidePopup(nOpt);
		//activate input field
		nOpt._inFieldGo.SetActive(true);
		//set placeholder text and input text
		string originalText = nOpt._navButtonText.text;
		//nOpt._inField.placeholder.text=originalText;
		nOpt._navButtonText.text = "";//nOpt._inField.text;
		nOpt._inField.text=originalText;
		nOpt._inField.Select();
		nOpt._inField.ActivateInputField();
		//disable nav button
		nOpt._navButton.enabled=false;
	}

	public void SaveOption(NavOption nOpt,bool fromScenario){
		nOpt._navButtonText.text = nOpt._inField.text;
		nOpt._inField.DeactivateInputField();
		nOpt._inFieldGo.SetActive(false);
		nOpt._navButton.enabled=true;
		//save to config
		if(Time.timeSinceLevelLoad>1f)
		{
			NavEventArgs nargs = new NavEventArgs();
			nargs.fromScenario=fromScenario;
			OnNavsChanged.Invoke(nargs);
		}
	}

	void DeleteOption(NavOption nOpt,bool fromScenario){
		if(!fromScenario)
			_navOptions.Remove(nOpt);
		else
			_scenNavOptions.Remove(nOpt);
		_addButtonIndex--;
		Destroy(nOpt._trans.gameObject);
		//save to config
		if(OnNavsChanged!=null)
		{
			NavEventArgs nargs = new NavEventArgs();
			nargs.fromScenario=fromScenario;
			OnNavsChanged.Invoke(nargs);
		}
	}

	//This AddNewOption is called by the Add button in the gui
	public void AddNewOption(bool fromScenario){
		if(fromScenario && !File.Exists(_mqtt._scenCatalogPath))
			return;
		Transform parent = fromScenario ? _secondLayout : _layout;
		Transform viewItem = Instantiate(_itemPrefab,parent);

		//this could be problematic if our add button ever got destroyed
		viewItem.SetSiblingIndex(parent.childCount-2);
		_addButtonIndex++;
		NavOption nOpt;
		nOpt = new NavOption(viewItem);
		//set text
		nOpt._navButtonText.text="New View";
		InitNavOption(nOpt,fromScenario);
		
		//trying something
		EditOption(nOpt);
	}

	//While this one is called from RoomConfig on loading the catalog file
	public void AddNewOption(string name, float posX,float posY,float posZ,float eulX,float eulY,
			bool fromScenario){
		Transform viewItem;
		if(!fromScenario)
		{
			viewItem = Instantiate(_itemPrefab,_layout);
			viewItem.SetSiblingIndex(_addButtonIndex);
			_addButtonIndex++;
		}
		else{
			viewItem = Instantiate(_itemPrefab,_secondLayout);
		}
		NavOption nOpt;
		nOpt = new NavOption(viewItem,name, new Vector3(posX,posY,posZ), new Vector3(eulX,eulY,0));
		nOpt._fromScenario=fromScenario;
		InitNavOption(nOpt,fromScenario);


	}

	void InitNavOption(NavOption nOpt, bool fromScenario=false){
		if(!fromScenario)
		{
			_navOptions.Add(nOpt);
		}
		else{
			_scenNavOptions.Add(nOpt);
		}
		//edit button clicked
		nOpt._editButton.onClick.AddListener(delegate{ShowPopup(nOpt);});
		//input field on end edit completes editing the option
		nOpt._inField.onEndEdit.AddListener(delegate{SaveOption(nOpt,fromScenario);});

		//delete button deletes option
		nOpt._deleteButton.onClick.AddListener(delegate{DeleteOption(nOpt,fromScenario);});
		nOpt._renameButton.onClick.AddListener(delegate{EditOption(nOpt);});

		//clicking on the text button itself, goes to a view
		//changeview
		nOpt._navButton.onClick.AddListener(delegate{Navigate(nOpt);});

		bool unlocked = _navOptions.Count>_lockedNavs;
		nOpt._editButton.gameObject.SetActive(unlocked);
		nOpt._trans.GetComponent<RawImage>().color=unlocked? new Color(0.5f,1,1) : new Color(1,1,1);

		//refresh layout
		if(_vlg!=null)
		{
			Canvas.ForceUpdateCanvases();
			_vlg.SetLayoutVertical();
		}
	}

	//Called when user clicks on navigation option in QuickNav menu
	void Navigate(NavOption nOpt, int index=-1){
		EventSystem.current.SetSelectedGameObject(null);
		_cam.enabled=true;
		SetOrbitTarget(nOpt);
		//navigate
		NavigateView(nOpt, index);
		int counter=0;
		foreach(NavOption nop in _navOptions){
			if(nop._navButtonText.text==nOpt._navButtonText.text){
				if(_mqtt==null)
					_mqtt = FindObjectOfType<MyMQTT>();
				_mqtt.SendNav(counter);
				transform.GetComponentInParent<LearnerPanels>().HidePanel(3);
				return;
			}
			counter++;
		}
		foreach(NavOption nop in _scenNavOptions){
			if(nop==nOpt){
				if(_mqtt==null)
					_mqtt = FindObjectOfType<MyMQTT>();
				_mqtt.SendNav(counter);
				transform.GetComponentInParent<LearnerPanels>().HidePanel(3);
				return;
			}
			counter++;
		}
	}

	public void SetHoffmanLoc(Vector3 loc){
		//todo replace hardcoded nonsense
		if(_navOptions.Count>17)
		{
			_navOptions[17]._position=loc+Vector3.up*1;
			_navOptions[17]._eulers=Vector3.right*90;
		}
	}

	public void NavigateByIndex(int index){
		if(index<0)
			return;
		NavOption nOpt = new NavOption();
		if(index <_navOptions.Count){
			nOpt = _navOptions[index];
			Navigate(nOpt,index);
		}
		else if(index-_navOptions.Count<_scenNavOptions.Count){
			nOpt = _scenNavOptions[index-_navOptions.Count];
			Navigate(nOpt,index);
		}
	}

	void SetOrbitTarget(NavOption nOpt){
		RaycastHit hit;
		GameObject foo = new GameObject();
		foo.transform.position = nOpt._position;
		foo.transform.eulerAngles = nOpt._eulers;
		LayerMask layers = ~0;
		if(Physics.Raycast(foo.transform.position,foo.transform.forward, out hit,3f,layers)){
			_cam.SetOrbitTarget(hit.transform.position);
		}
		Destroy(foo);

	}

	public int GetIndexFromNavName(string name){
		int counter=0;
		foreach(NavOption nop in _navOptions){
			if(nop._navButtonText.text==name)
				return counter;
			counter++;
		}
		foreach(NavOption nop in _scenNavOptions){
			if(nop._navButtonText.text==name)
				return counter;
			counter++;
		}
		return counter;
	}

	public void GotoRecent(int index){
		if(_recents.Count>index)
			Navigate(_recents[index]);
	}

	public void GotoRecent(RecentNav rn){
		int index = rn._index;
		if(_recents.Count>index)
			Navigate(_recents[index]);
	}

	//#todo - NEEDS REWORK
	public NavOption GetCoords(int index){
		Transform looky = null;
		NavOption foo = new NavOption();
		switch(index){
			case -1:
			default:
				foo._position=_navOptions[index]._position;
				foo._eulers=_navOptions[index]._eulers;
				break;
			case 2://pump
				looky = _cart._pump.transform.Find("LookTarget");
				foo._position=looky.position;
				foo._eulers=looky.eulerAngles;
				break;
			case 3:
				looky = _cart._gasBlender.transform.Find("LookTarget");
				foo._position=looky.position;
				foo._eulers=looky.eulerAngles;
				break;
			case 4:
				if(_cart._oxygenator!=null)
					looky = EcmoCart.FindRecursive(_cart._oxygenator,"LookTarget");
				else
					looky = _cart._pump.transform.Find("LookTarget");
				foo._position=looky.position;
				foo._eulers=looky.eulerAngles;
				break;
		}
		return foo;
	}

	//#todo - NEEDS REWORK
	public void NavigateView(NavOption nOpt, int index=-1){
		if(index==-1)
			index = GetIndexFromNavName(nOpt._navButtonText.text);

		switch(index){
			case -1:
			default:
				_cam.GoTo(nOpt._position,nOpt._eulers);
				break;
			case 2://pump
				_cam.LerpToTransformDirect(_cart._pump.transform.Find("LookTarget"));
				break;
			case 3:
				_cam.LerpToTransformDirect(_cart._gasBlender.transform.Find("LookTarget"));
				break;
			case 4:
				if(_cart._oxygenator!=null)
					_cam.LerpToTransformDirect(EcmoCart.FindRecursive(_cart._oxygenator,"LookTarget"));
				else
					_cam.LerpToTransformDirect(_cart._pump.transform.Find("LookTarget"));
				break;
		}
		if(_recents.Contains(nOpt)){
			//keep order same
		}
		else{
			if(_recents.Count==0){//special case for initial overview
				_recents.Insert(0,nOpt);
			}
			else if(_recents.Count <_maxRecents){//special case for not yet filled list
				_recents.Insert(1,nOpt);
			}
			else{
				_recents.RemoveAt(_maxRecents-1);//default case when recents is filled
				_recents.Insert(1,nOpt);
			}
		}

		for(int i=0; i<_recents.Count; i++){
			_recentNavs[i]._label.text=(i+1)+". "+_recents[i]._navButtonText.text;
		}
	}

	public string SerializeNavOptions(bool fromScen){
		string ser = "";
		if(!fromScen)
		{
			for(int i=0; i<_navOptions.Count; i++){
				ser+=(i+" = ");
				ser+=_navOptions[i].Serialize();
				ser+=System.Environment.NewLine;
			}
		}
		else{
			for(int i=0; i<_scenNavOptions.Count; i++){
				ser+=(i+" = ");
				ser+=_scenNavOptions[i].Serialize();
				ser+=System.Environment.NewLine;
			}

		}
		ser+=System.Environment.NewLine;
		return ser;
	}

	public NavOption GetOptionByIndex(int i){
		if(i>=0 && i<_navOptions.Count)
			return _navOptions[i];
		else 
			return null;
	}
}
