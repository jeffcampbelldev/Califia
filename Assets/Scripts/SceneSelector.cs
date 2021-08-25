using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SceneSelector : MonoBehaviour
{
	public CanvasGroup _menu;
	//ok this stuff that gets turned off and on everytime a menu pops up on screen
	//really should be centralized somewhere post-demo
	BoxCollider _blocker;
	TestCam _cam;
	[SerializeField] private InputControl _control;
	public Slider _graphicsSlider;
	public Text _graphicsLabel;
	string[] _graphicsNames;
	int _curQuality;
	public Button _yes;
	public Button _no;
	public Button _exit;
	public Toggle _stats;
	public Toggle _fullScreen;
	public GameObject _statsCanvas;
	LearnerPanels _panels;
	//public CanvasGroup _confirm;
	Inventory _inventory;
	public CanvasGroup _gloveCanvas;
	public Button _gloves;
	public Camera _glovCam;
	private bool _gloveEnabled = false; 

	// Start is called before the first frame update
	void Start()
	{
		if(transform.childCount>0){
			if(_menu==null)
				Debug.Log("menu null");
			NoReturn();
		}
		_cam = FindObjectOfType<TestCam>();
		if(_cam)
			_blocker = _cam.transform.Find("Blocker").GetComponent<BoxCollider>();

		_graphicsNames = QualitySettings.names;
		_curQuality = QualitySettings.GetQualityLevel();
		_graphicsLabel.text=_graphicsNames[_curQuality];
		_graphicsSlider.maxValue = _graphicsNames.Length-1;
		_graphicsSlider.value=_curQuality;

		_statsCanvas.SetActive(false);

		SceneManager.sceneLoaded += OnSceneLoaded;
		SceneManager.sceneUnloaded += OnSceneUnloaded;
		_panels = FindObjectOfType<LearnerPanels>();
		_inventory = FindObjectOfType<Inventory>();

		//ToggleConfirmMenu(false);
	}

	void OnSceneLoaded(Scene scene, LoadSceneMode mode){
		_cam = FindObjectOfType<TestCam>();
		if(_cam)
			_blocker = _cam.transform.Find("Blocker").GetComponent<BoxCollider>();
		_panels = FindObjectOfType<LearnerPanels>();
		_inventory = FindObjectOfType<Inventory>();
	}

	void OnSceneUnloaded(Scene scene){
		_panels = FindObjectOfType<LearnerPanels>();
		_inventory = FindObjectOfType<Inventory>();
	}

	// Update is called once per frame
	void Update()
	{
		if(_menu!=null){
			if(Input.GetButtonDown("Menu")){
				if(_panels!=null && _panels.IsAnyPanelActive())
					_panels.HideAllPanels();
				else if(_inventory!=null && _inventory.HasAnyItem())
					_inventory.ReturnAllItems();
				else{
					if (_menu.alpha==1f || _gloveCanvas.alpha == 1f)
						NoReturn();
					else
						ShowEscapeMenu();
				}
			}
		}
	}

	public void ShowEscapeMenu(){
		_menu.alpha=1f;
		_menu.blocksRaycasts=true;
		_menu.interactable=true;
		if(_blocker)
			_blocker.enabled=true;
		if(_cam)
			_cam.enabled=false;

		//_cam.CheckInputType(false);

		_control.isEnabled = false;
		_yes.interactable=SceneManager.GetActiveScene().buildIndex>1;
		_exit.interactable=_fullScreen.isOn;
		_exit.transform.GetChild(0).GetComponent<Text>().enabled=_exit.interactable;

		if(TestCam._useController)
			_no.Select();
	}

	public void NoReturn(){
		//#todo this null check is called for a quick fix but will probably come back to bite us later
		if(_menu==null)
			return;
		if (_gloveEnabled){
			HideGloves();
			_gloveEnabled = false;
		}

		_menu.alpha = 0f;
		_menu.blocksRaycasts = false;
		_menu.interactable = false;

		if (_blocker)
			_blocker.enabled=false;
		if(_cam)
			_cam.enabled=true;
		_control.isEnabled = true;
	}

	public void GoToScene(int index){
		SceneManager.LoadScene(index);
		_cam = FindObjectOfType<TestCam>();
		if(_cam!=null)
			_blocker = _cam.transform.Find("Blocker").GetComponent<BoxCollider>();
		MyMQTT mqtt = FindObjectOfType<MyMQTT>();
		//#why is this hardcoded to go to room 0?
		mqtt.SendPub("Room",0);
		mqtt._sceneCode=1;
		AnimationPath._isRunning=false;
		NoReturn();
	}

	public void ChangeGraphicsQuality(){
		int level = Mathf.RoundToInt(_graphicsSlider.value);
		_graphicsLabel.text=_graphicsNames[level];
		//at start the slider is set to match the current quality level from player prefs
		//no need to set again
		if(Time.timeSinceLevelLoad>1f)
			QualitySettings.SetQualityLevel(level);
	}

	public void ToggleShowStats(){
		//Debug.Log("Showing stats: "+_stats.isOn);
		_statsCanvas.SetActive(_stats.isOn);
	}

	public void ToggleFullScreen(){
		DisplayManager dm = FindObjectOfType<DisplayManager>();
		dm.SetStyle(_fullScreen.isOn ? 0x80000000 : 0x00CF0000);
		dm.TrySetDisplay(99);
		_exit.interactable=_fullScreen.isOn;
		_exit.transform.GetChild(0).GetComponent<Text>().enabled=_exit.interactable;
	}

	/*
	public void ToggleConfirmMenu(bool on){
		_confirm.alpha = on? 1f : 0f;
		_confirm.interactable = on;
		_confirm.blocksRaycasts = on;
	}
	*/

	public void ShowGlovesForModule(){
		ToggleGloveMenu(true);
	}

	public void HideGloves(){
		ToggleGloveMenu(false);
	}

	public void ToggleGloveMenu(bool on){
		//Debug.Log("glove button clicked");
		_glovCam.enabled = on;
		_gloveCanvas.alpha = on ? 1f : 0f;
		_gloveCanvas.interactable = on;
		_gloveCanvas.blocksRaycasts = on;
		_gloveEnabled = true;

		_menu.alpha = on ? 0f : 1f;
		_menu.interactable = !on;
		_menu.blocksRaycasts = !on;
	}

	public void ExitApp(){
#if UNITY_EDITOR
		UnityEditor.EditorApplication.isPlaying=false;
#else
		Application.Quit();
#endif
	}
}
