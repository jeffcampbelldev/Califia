//sections
//gui_structs
//Start
//Update
//SelectionButtonPress
//ChannelSelect
//gui_screens
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class Alaris : MonoBehaviour
{
	RawImage _screen;
	RawImage _blocker;
	public Texture2D _powerScreen;
	public Texture2D _templateBlue;
	public Text _chId;
	RawImage _chIdBg;
	public Text _header;
	public Text _subHeader;
	public Text _prompt;
	public Color _dark;
	public Color _light;
	public Color _grey;
	public CanvasGroup _textOptionsParent;
	string _profile;
	public Font _outline;
	public Font _regular;
	public CanvasGroup _channelInfoParent;
	AudioSource _beep;
	public CanvasGroup _bottomOptionsParent;
	int _selectedChannel;
	public CanvasGroup _rightOptionsParent;
	//pharmacy / iv fluids list stuff goes here
	//but for now tmp vars
	int _pharmaOffset=0;
	string _lastFluid;
	string _lastFluidType;
	RoomConfig _room;
	List<RoomConfig.IVBag> _bags;
	int _bagIndex;
	List<string> _pharma;
	public CanvasGroup _fluidSetupScreen;
	Text _fluidSetupTitle;
	public Texture2D _fill;
	public Texture2D _noFill;
	bool _canStart;
	float _fvTimer;
	float _fvDur;
	bool _fv;
	float _fTimer;
	float _fDur;
	float _iTimer;
	public CanvasGroup _bolusMenu;
	Text _patientWeight;
	Text _totalDose;
	Text _bolusVolume;
	public Color _blue;
	public Color _green;
	MyMQTT _mqtt;

	//*gui_structs*
	[System.Serializable]
	public struct TextOption{
		public RawImage _bg;
		public Text _txt;
		public Text _ns;

		public void Select(bool sel,Alaris a){
			_bg.enabled=sel;
			_txt.color=sel? a._light : a._dark;
		}

		public void SetText(string t){
			_txt.text=t;
			if(t=="")
				_bg.enabled=false;
			_ns.enabled=t.Length>0 && t[t.Length-1]==' ';
		}

		public string GetText(){
			return _txt.text;
		}
	}
	List<TextOption> _textOptions = new List<TextOption>();

	[System.Serializable]
	public class ChannelInfo{
		public Text _label;
		public Text _rateModule;
		public Text _fluidModule;
		public Text _labelModule;
		public string _fluid;
		public string _fluidActual;
		public Text _fluidText;
		public Text _volumeText;
		public bool _running;
		public bool _paused;
		public bool _setup;
		public GameObject _fillBar;
		public Image _fill;
		public float _origVol;
		public float _primVol;
		public float _primRate;
		public float _bolusVol;
		public float _bolusDur;
		public float _bolusDose;
		public float _bolusRate;
		public bool _bolus;
		public LiquidHelper _lh;
		IEnumerator _infusionR;
		public ParticleSystem _drip;
		public Tube[] _tubes;

		public void Init(Alaris a){
			SetFilled(false,a);
			SetRate("");
			SetFluid("");
			SetRunning(false,a);
			_fluidText.enabled=false;
			_volumeText.enabled=false;
		}
		public void SetFilled(bool fill, Alaris a){
			_label.font=fill? a._regular : a._outline;
		}
		public void SetRate(string s){
			_rateModule.text=s;
			float f=0;
			float.TryParse(s, out f);
			_primRate=f;
		}
		public void SetVolume(string s){
			_volumeText.text="VTBI = <b>"+s+"</b> mL";
			float f=0;
			float.TryParse(s, out f);
			_primVol=f;
			_origVol=f;
		}
		public void Infuse(float f){
			_primVol-=f;
			_volumeText.text="VTBI = <b>"+_primVol.ToString("0.0")+"</b> mL";
			_fill.fillAmount=1-(_primVol/_origVol);
			_lh.Infuse(f);
		}
		public void RevertContinuous(){
			SetRate(_primRate.ToString());
			SetVolume(_primVol.ToString());
		}
		public void SetBolusRate(string s){
			_bolus=true;
			_rateModule.text=s;
			float f=0;
			float.TryParse(s, out f);
			_bolusRate=f;
		}
		public void SetBolusVolume(string s){
			_volumeText.text="BOLUS VTBI = <b>"+s+"</b> mL";
			float f=0;
			float.TryParse(s, out f);
			_bolusVol=f;
		}
		public void SetBolusDose(string s){
			_fluidText.text=_fluid+"\n"+s+" unit/kg Bolus ";
			_fluidModule.text=_fluid+" "+s+" unit/kg";
			float f=0;
			float.TryParse(s, out f);
			_bolusDose=f;
		}
		public void SetFluid(string s){
			_fluid=s;
			_fluidModule.text=s;
			_fluidText.text=s;
		}
		public void SetIVBag(RoomConfig.IVBag bag)
		{
			_lh.SetBag(bag);
			_fluidActual=bag.fluid_name;
		}
		public void ToggleFV(bool f){
			if(_running){
				_fluidText.enabled=f;
				_volumeText.enabled=!f;
			}
		}
		public void ToggleId(){
			if(_setup){
				_labelModule.enabled=!_labelModule.enabled;
			}
		}
		public void Setup(bool s){
			_setup=s;
			if(!s)
				_labelModule.enabled=true;
		}
		public void AnimateFluidText(float f){
			if(_running){
				_fluidModule.transform.localPosition = Vector3.right*Mathf.Lerp(650f,-650,f);
			}
		}
		public void SetRunning(bool run,Alaris a,bool paused=false){
			_running=run;
			_paused=paused;
			_fillBar.SetActive(run);
			SetFilled(run,a);
			//this may change
			if(run){
				_infusionR = a.PrimaryInfusion(this);
				a.StartCoroutine(_infusionR);
			}
			else{
				if(_infusionR!=null)
				{
					a.StopCoroutine(_infusionR);
					_drip.Stop();
					ToggleFV(true);
				}
				_fluidModule.text="";
			}
		}

		public void UpdateColor(){
			var main = _drip.main;
			main.startColor=_lh._color;
			foreach(Tube t in _tubes)
				t._meshR.material.color=_lh._color;
		}

		public void ClearColor(){
			foreach(Tube t in _tubes)
				t._meshR.material.color=new Color(0.83f,0.92f,0.90f,0.1f);
		}
	}
	List<ChannelInfo> _channelInfo = new List<ChannelInfo>();

	[System.Serializable]
	public struct BottomOption{
		public Text _label;
		public RawImage _img;
		
		public void Enabled(bool enable, Alaris a){
			_label.color=enable? a._dark : a._grey;
			_img.color=enable? a._dark : a._grey;
		}
		public void SetVisible(bool vis){
			_label.enabled=vis;
			_img.enabled=vis;
		}
		public void SetText(string t){
			_label.text=t;
		}
		public void Setup(bool vis, bool en, string t,Alaris a){
			Enabled(en,a);
			SetVisible(vis);
			SetText(t);
		}
	}

	List<BottomOption> _bottomOptions = new List<BottomOption>();

	[System.Serializable]
	public struct RightOption{
		public Text _label;
		public RawImage _img;
		
		public void SetVisible(bool vis){
			_label.enabled=vis;
			_img.enabled=vis;
		}
		public void SetText(string t){
			_label.text=t;
		}
		public void Setup(bool vis,string t){
			SetVisible(vis);
			SetText(t);
		}
	}

	List<RightOption> _rightOptions = new List<RightOption>();

	[System.Serializable]
	public struct SetupOption{
		public RawImage _img;
		public Text _txt;
		public RawImage _cursor;
		public Text _val;
		
		public void Show(bool show){
			_img.enabled=show;
			_txt.enabled=show;
			_val.enabled=show;
			if(!show)
				_cursor.enabled=false;
			_val.transform.GetChild(0).GetComponent<Text>().enabled=show;
		}

		public void Select(bool sel,Alaris a){
			_img.texture=sel? a._fill : a._noFill;
			_img.color = sel? a._dark : Color.black;
			_txt.color = sel? a._light : a._dark;
			if(sel)
				ClearVal();
			_cursor.enabled=sel;
		}
		public void DialValue(char v){
			string val = _val.text+v;
			if(val.Length>7){
				val = val.Substring(1,7);
			}
			_val.text=val;
		}
		public void SetVal(string v){
			_val.text=v;
		}
		public void ClearVal(){
			_val.text="";
			_cursor.enabled=false;
		}
		public bool HasVal(){
			return !(_val.text==""||_val.text=="0");
		}
	}

	List<SetupOption> _setupOptions = new List<SetupOption>();


	int _state=0;
    // Start is called before the first frame update
    void Start()
    {
		_fvDur=2f;
		_fDur=6f;
		_room = FindObjectOfType<RoomConfig>();
		_beep=GetComponent<AudioSource>();
		//init screen
		_screen = transform.GetChild(0).GetChild(0).GetChild(1).GetComponent<RawImage>();
		_blocker = transform.GetChild(0).GetChild(0).GetChild(2).GetComponent<RawImage>();
		_screen.color = _light;
		_chId.text="";
		_chIdBg=_chId.transform.parent.GetComponent<RawImage>();
		_chIdBg.enabled=false;
		_header.text="";
		_subHeader.text="";
		_prompt.text="";
		//init text options
		foreach(Transform t in _textOptionsParent.transform){
			TextOption to;
			to._bg=t.GetComponent<RawImage>();
			to._bg.color=_dark;
			to._txt=t.GetChild(0).GetComponent<Text>();
			to._ns=t.GetChild(1).GetComponent<Text>();
			to.Select(false,this);
			_textOptions.Add(to);
		}
		_textOptionsParent.alpha=0;
		//init channel info
		foreach(Transform t in _channelInfoParent.transform){
			ChannelInfo ci = new ChannelInfo();
			ci._label=t.GetChild(0).GetComponent<Text>();
			ci._fluidText=t.GetChild(3).GetComponent<Text>();
			ci._volumeText=t.GetChild(2).GetComponent<Text>();
			ci._fillBar=t.GetChild(1).gameObject;
			ci._fill=ci._fillBar.transform.GetChild(0).GetComponent<Image>();
			Transform pmc = transform.Find(ci._label.text).GetChild(0).GetChild(0);
			ci._fluidModule=pmc.GetChild(1).GetChild(0).GetComponent<Text>();
			ci._rateModule=pmc.GetChild(0).GetComponent<Text>();
			ci._labelModule=pmc.GetChild(2).GetComponent<Text>();
			ci._lh = pmc.parent.parent.GetComponentInChildren<LiquidHelper>();
			ci._drip=ci._lh.transform.GetComponentInChildren<ParticleSystem>();
			ci._tubes = pmc.parent.parent.GetComponentsInChildren<Tube>();
			ci.Init(this);
			_channelInfo.Add(ci);
		}
		_channelInfoParent.alpha=0;
		//init bottom options
		foreach(Transform t in _bottomOptionsParent.transform){
			BottomOption b;
			b._img=t.GetComponent<RawImage>();
			b._label=t.GetChild(0).GetComponent<Text>();
			b.SetVisible(false);
			_bottomOptions.Add(b);
		}
		_bottomOptionsParent.alpha=0f;
		//init right options
		foreach(Transform t in _rightOptionsParent.transform){
			RightOption r;
			r._img=t.GetComponent<RawImage>();
			r._label=t.GetChild(0).GetComponent<Text>();
			_rightOptions.Add(r);
		}
		_rightOptionsParent.alpha=0f;
		//init fluid setup options
		Transform sop = _fluidSetupScreen.transform.GetChild(1);
		foreach(Transform t in sop){
			SetupOption so;
			so._img=t.GetComponent<RawImage>();
			so._txt=t.GetChild(0).GetComponent<Text>();
			so._cursor = t.GetChild(1).GetComponent<RawImage>();
			so._val = t.GetChild(2).GetComponent<Text>();
			so.Show(false);
			_setupOptions.Add(so);
		}
		_fluidSetupScreen.alpha=0f;
		_fluidSetupTitle=_fluidSetupScreen.transform.GetChild(0).GetComponent<Text>();
		//init bolus menu
		_patientWeight=_bolusMenu.transform.GetChild(0).GetChild(0).GetComponent<Text>();
		_totalDose=_bolusMenu.transform.GetChild(1).GetChild(0).GetComponent<Text>();
		_bolusVolume=_bolusMenu.transform.GetChild(2).GetChild(0).GetComponent<Text>();
		_bolusMenu.alpha=0f;

		MyMQTT [] qts = FindObjectsOfType<MyMQTT>();
		foreach(MyMQTT qt in qts){
			if(qt.gameObject.tag=="GameController")
				_mqtt=qt;
		}
    }

    // Update is called once per frame
    void Update()
    {
		switch(_state){
			case 4:
				//rotate between fluid and volume
				_fvTimer+=Time.deltaTime;
				if(_fvTimer>_fvDur)
				{
					_fvTimer=0;
					_fv=!_fv;
					foreach(ChannelInfo ci in _channelInfo){
						ci.ToggleFV(_fv);
					}
				}
				break;
			case 8:
			case 9:
			case 10:
			case 11:
			case 12:
			case 13:
				//blink label
				_iTimer+=Time.deltaTime;
				if(_iTimer>.5f){
					_iTimer=0;
					foreach(ChannelInfo ci in _channelInfo){
						ci.ToggleId();
					}
				}
				break;
			default:
				break;
		}
		_fTimer+=Time.deltaTime;
		if(_fTimer>_fDur)
			_fTimer=0;
		foreach(ChannelInfo ci in _channelInfo){
			if(ci._running)
			{
				ci.AnimateFluidText(_fTimer/_fDur);
			}
		}
    }

	public void PowerOff(){
		_state=1;
		Power();
	}

	public void Power(){
		if(_state==0)
		{
			StartCoroutine(PowerOnR());
			_blocker.enabled = false;
			_screen.texture=_powerScreen;
		}
		else{
			_blocker.enabled=true;
			_state=0;
			foreach(ChannelInfo ci in _channelInfo){
				ci.Init(this);
			}
			_channelInfoParent.alpha=0f;
			_rightOptionsParent.alpha=0f;
			_fluidSetupScreen.alpha=0f;
			_bolusMenu.alpha=0f;
			_textOptionsParent.alpha=0f;
			_bottomOptionsParent.alpha=0f;
			_chId.text="";
			_chIdBg.enabled=false;
		}
	}

	IEnumerator PowerOnR(){
		_state=1;
		_screen.color = _light;
		yield return new WaitForSeconds(.5f);
		_screen.texture=_templateBlue;
		_screen.color = _blue;
		_state=2;
		//setup profile select
		_prompt.text="> Select a Profile and Confirm";
		foreach(TextOption to in _textOptions)
			to.Select(false,this);
		_textOptions[0].SetText("Adult ICU v9A");
		_textOptions[1].SetText("LABOR & DELIVERY v9A ");
		_textOptions[2].SetText("MEDICAL/SURGICAL v9A ");
		_textOptions[3].SetText("NICU v9A ");
		_textOptions[4].SetText("ONCOLOGY v9A ");
		_textOptionsParent.alpha=1;
		_bottomOptions[2].Setup(true,false,"CONFIRM",this);
		_bottomOptions[3].Setup(true,true,"PAGE\nDOWN",this);
		_bottomOptionsParent.alpha=1f;
	}

	public void SelectionButtonPress(int selectionIndex){
		if(_state!=0)
			_beep.Play();
		switch(_state){
			case 0://off
				break;
			case 1://bootup
				break;
			case 2://select profile
				if(selectionIndex<5){
					SelectTextOption(selectionIndex);
					_profile=_textOptions[selectionIndex]._txt.text;
					//_screen.texture=_profileConfScreen;
					_state=3;
					_prompt.text="> Press CONFIRM";
					_bottomOptions[2].Enabled(true,this);
				}
				break;
			case 3://confirm profile
				if(selectionIndex<5){
					SelectTextOption(selectionIndex);
					_profile=_textOptions[selectionIndex]._txt.text;
				}
				if(selectionIndex==12){
					GoHome();
				}
				break;
			case 4://home screen
				break;
			case 5://infusion menu
				if(selectionIndex==11){//exit
					_channelInfo[_selectedChannel].SetRate("");
					GoHome();
				}
				else if(selectionIndex==1){
					GotoPharmacy(selectionIndex);
				}
				break;
			case 6://pharmacy
				if(selectionIndex==11){
					_channelInfo[_selectedChannel].SetRate("");
					GoHome();
				}
				else if(selectionIndex<_pharma.Count-_pharmaOffset){
					PharmacyConfirm(selectionIndex);
				}
				else if(selectionIndex>=5 && selectionIndex<=9){
					Debug.Log("Jumping to section alpha");
					//jump to list alphabetically
				}
				else if(selectionIndex==13){
					//page down in list
					if(_pharmaOffset<_pharma.Count-5)
						_pharmaOffset+=5;
					else
						_pharmaOffset=0;
					RenderFluidList();
				}
				break;
			case 7://pharma confirm
				if(selectionIndex==5){
					//yes confirm
					RoomConfig.IVBag tmp = _bags[_bagIndex];
					Debug.Log("Confirmed fluid: "+tmp.fluid_name);
					Debug.Log("Capacity: "+tmp.capacity);
					Debug.Log("Target infusion rate: "+tmp.target_infusion_rate);
					FluidSetup();
				}
				else if(selectionIndex==6){
					//no confirm
					GotoPharmacy(1);
				}
				break;
			case 8://fluid setup
				//in 8, 9, or 10 support the ability to go back via channel input button
				if(selectionIndex==1){
					//setup rate
					SetupRate();
				}
				else if(selectionIndex==2){
					SetupVolume();
				}
				else if(_channelInfo[_selectedChannel]._running && selectionIndex==11){
					Debug.Log("User requested to stop");
					StopInfusionMenu();
				}
				else if(_channelInfo[_selectedChannel]._running && selectionIndex==12){
					BolusMenu();
				}
				else if(_canStart && selectionIndex==13){
					GoHome();
					SetupPrimaryInfusion();
				}
				break;
			case 9://rate input
				if(selectionIndex==2){
					//setup volume
					SetupVolume();
				}
				//num input
				else if(selectionIndex>=14 && selectionIndex<=23){
					int num = selectionIndex-14;
					DialNumForOption(num,0,0,1);
				}
				else if(_canStart && selectionIndex==13){
					GoHome();
					SetupPrimaryInfusion();
				}
				break;
			case 10://volume input
				if(selectionIndex==1){
					SetupRate();
				}
				//num input
				else if(selectionIndex>=14 && selectionIndex<=23){
					int num = selectionIndex-14;
					DialNumForOption(num,1,0,1);
				}
				else if(_canStart && selectionIndex==13){
					GoHome();
					SetupPrimaryInfusion();
				}
				break;
			case 11:
				if(selectionIndex==1){
					SetupDose();
				}
				else if(selectionIndex==3){
					//setup volume
					SetupDuration();
				}
				else if(_channelInfo[_selectedChannel]._bolus && selectionIndex==13){
					GoHome();
					SetupBolusInfusion();
				}
				else if(_channelInfo[_selectedChannel]._bolus && selectionIndex==11){
					StopBolusMenu();
				}
				break;
			case 12://dose input
				if(selectionIndex==3){
					//setup volume
					SetupDuration();
				}
				//num input
				else if(selectionIndex>=14 && selectionIndex<=23){
					int num = selectionIndex-14;
					DialNumForOption(num,2,2,3);
				}
				else if(_canStart && selectionIndex==13){
					GoHome();
					SetupBolusInfusion();
				}
				break;
			case 13://duration input
				if(selectionIndex==1){
					SetupDose();
				}
				//num input
				else if(selectionIndex>=14 && selectionIndex<=23){
					int num = selectionIndex-14;
					DialNumForOption(num,3,2,3);
				}
				else if(_canStart && selectionIndex==13){
					GoHome();
					SetupBolusInfusion();
				}
				break;
			case 14://stop bolus menu
				if(selectionIndex==5){
					//stop bolus
					_screen.color=_blue;
					_chId.color=_blue;
					StopBolus();
				}
				else if(selectionIndex==6){
					//resume bolus
					BolusMenu();
					_screen.color=_blue;
					_chId.color=_blue;
				}
				break;
			case 15://stop infusion menu
				if(selectionIndex==5){
					//stop bolus
					Debug.Log("Yes - stop infusion");
					StopInfusion();
					_screen.color=_blue;
					_chId.color=_blue;
					//StopBolus();
				}
				else if(selectionIndex==6){
					//resume bolus
					Debug.Log("sike, keep infusing");
					FluidSetup();
					_screen.color=_blue;
					_chId.color=_blue;
					//BolusMenu();
					//_screen.color=_blue;
					//_chId.color=_blue;
				}
				break;
			default:
				break;
		}
	}

	void SelectTextOption(int index){
		for(int i=0; i<_textOptions.Count; i++)
			_textOptions[i].Select(i==index,this);
	}

	void ClearTextOptions(){
		for(int i=0; i<_textOptions.Count; i++)
			_textOptions[i].Select(false,this);
	}

	public void ChannelSelect(int index){
		_beep.Play();
		if(_state==4){
			_selectedChannel=index;
			_channelInfoParent.alpha=0f;
			if(_channelInfo[_selectedChannel]._running || _channelInfo[_selectedChannel]._paused){
				_chIdBg.enabled=true;
				_chId.text=_channelInfo[index]._label.text;
				if(_channelInfo[_selectedChannel]._bolus)
					BolusMenu();
				else
					FluidSetup();
			}
			else{
				_channelInfo[index].SetRate("----");
				_header.text="Infusion Menu";
				_prompt.text="> Select an Option or EXIT";
				_textOptionsParent.alpha=1f;
				ClearTextOptions();
				_textOptions[0].SetText("Guardrails Drugs ");
				_textOptions[1].SetText("Guardrails IV Fluids");
				_textOptions[2].SetText("Basic Infusion ");
				_textOptions[3].SetText("");
				_textOptions[4].SetText("");
				_chIdBg.enabled=true;
				_chId.text=_channelInfo[index]._label.text;
				_bottomOptions[0].SetVisible(false);
				_bottomOptions[1].Setup(true,true,"EXIT",this);
				_bottomOptions[2].SetVisible(false);
				_bottomOptions[3].SetVisible(false);
				_state=5;
			}
		}
	}
	
	//*gui_screens*
	void GoHome(){
		_state=4;
		_prompt.text="> Select Channel";
		_textOptionsParent.alpha=0f;
		_chIdBg.enabled=false;
		_chId.text="";
		_header.text="NOT FOR HUMAN USE";
		_subHeader.text=_profile;
		_channelInfoParent.alpha=1f;
		_bottomOptions[1].SetVisible(false);
		_bottomOptions[3].SetVisible(false);
		_bottomOptions[0].Setup(true,true,"VOLUME\nINFUSED",this);
		_bottomOptions[2].Setup(true,true,"AUDIO\nADJUST",this);
		_fluidSetupScreen.alpha=0;
	}
	void GotoPharmacy(int index){
		_state=6;
		//determine list based on index
		_header.text=_textOptions[index].GetText();
		_bottomOptionsParent.alpha=1;
		_bottomOptions[3].Setup(true,true,"PAGE\nDOWN",this);
		_prompt.text="> Select IV Fluid";
		_lastFluidType="Guardrails Fluid";
		//enable alpha buttons
		OfferAlphaOptions();
		_rightOptionsParent.alpha=1f;
		//set up initial pharma library
		//load these from ini
		_bags = _room.LoadFluids();
		_pharma = new List<string>();
		foreach(RoomConfig.IVBag b in _bags){
			_pharma.Add(b.fluid_name);
		}
		_pharmaOffset=0;
		RenderFluidList();
	}

	void RenderFluidList(){
		for(int i=_pharmaOffset;i<_pharmaOffset+5; i++){
			if(i<_pharma.Count)
				_textOptions[i-_pharmaOffset].SetText(_pharma[i]);
			else
				_textOptions[i-_pharmaOffset].SetText("");
		}
	}

	void OfferAlphaOptions(){
		_rightOptions[0].Setup(true,"A-E");
		_rightOptions[1].Setup(true,"F-J");
		_rightOptions[2].Setup(true,"K-O");
		_rightOptions[3].Setup(true,"P-T");
		_rightOptions[4].Setup(true,"U-Z");
	}

	void OfferYesNoOptions(){
		_rightOptions[0].Setup(true,"Yes");
		_rightOptions[1].Setup(true,"No");
		_rightOptions[2].Setup(false,"K-O");
		_rightOptions[3].Setup(false,"P-T");
		_rightOptions[4].Setup(false,"U-Z");
	}
	void PharmacyConfirm(int index){
		_state=7;
		//determine list based on index
		_bottomOptionsParent.alpha=0;
		_prompt.text="> Press Yes or No";
		//enable yes/no buttons
		_rightOptionsParent.alpha=1f;
		OfferYesNoOptions();
		//load these from ini
		_lastFluid=_textOptions[index].GetText();
		string prompt = _lastFluid+"\nwas selected.\nIs this correct?";
		_textOptions[0].SetText(prompt);
		_textOptions[1].SetText("");
		_textOptions[2].SetText("");
		_textOptions[3].SetText("");
		_textOptions[4].SetText("");
		_bagIndex = index+_pharmaOffset;
	}

	void FluidSetup(){
		_state=8;
		ChannelInfo ci = _channelInfo[_selectedChannel];
		bool run = ci._running || ci._paused;
		EnableStart(run);
		//get module label to blink
		_channelInfo[_selectedChannel].Setup(true);
		_header.text=_lastFluidType+" Setup";
		_bottomOptionsParent.alpha=1;
		//fluid setup screen
		_fluidSetupScreen.alpha=1f;
		_fluidSetupTitle.text="PRIMARY INFUSION"; //later this may actually be secondary
		_bolusMenu.alpha=0f;
		_rightOptionsParent.alpha=0f;
		//clear left options
		_textOptions[0].SetText("");
		_textOptions[1].SetText("");
		_textOptions[2].SetText("");
		_textOptions[3].SetText("");
		_textOptions[4].SetText("");
		//clear values and deselect rate and vtbi
		_setupOptions[0].Show(true);
		_setupOptions[1].Show(true);
		_setupOptions[2].Show(false);
		_setupOptions[3].Show(false);
		_setupOptions[0].Select(false,this);
		_setupOptions[1].Select(false,this);
		//if not running prefil defaults
		if(!run){
			float cap = _bags[_bagIndex].capacity;
			float tif = _bags[_bagIndex].target_infusion_rate;
			float weight=100;
			try{
				weight=_mqtt._patient._myWeight;
			}
			catch(System.Exception e){
				Debug.Log("patient weight not received. "+e.Message);
			}
			float tr = tif*weight;
			ci.SetVolume(cap.ToString("0"));
			ci.SetRate(tr.ToString("0"));
			_canStart=true;
		}
		_setupOptions[0].SetVal(ci._primRate.ToString());
		_setupOptions[1].SetVal(ci._primVol.ToString());

		if(run){
			//prompt is already set from EnableStart(bool)
			_subHeader.text=ci._fluid;
		}
		else{
			_prompt.text="> Enter Rate";
			_subHeader.text=_lastFluid;
		}
		//bottom options
		_bottomOptions[0].Setup(true,false,"DELAY\nOPTIONS",this);
		if(run)
			_bottomOptions[1].Setup(true,true,"STOP", this);
		else
			_bottomOptions[1].Setup(true,false,"SETUP",this);
		_bottomOptions[2].Setup(true,true,"BOLUS",this);
		_bottomOptions[3].Setup(true,true,"START",this);
	}

	void SetupRate(){
		_state=9;
		_setupOptions[0].Select(true,this);
		_setupOptions[1].Select(false,this);
		EnableStart(false);
		_prompt.text="> Enter Rate";
	}
	void SetupVolume(){
		_state=10;
		_setupOptions[0].Select(false,this);
		_setupOptions[1].Select(true,this);
		EnableStart(false);
		_prompt.text="> Enter VTBI";
	}

	void DialNumForOption(int num,int index,int requiredA,int requiredB){
		_setupOptions[index].DialValue(num.ToString()[0]);
		bool canStart=_setupOptions[requiredA].HasVal()&&_setupOptions[requiredB].HasVal();
		EnableStart(canStart);
		if(index>1){
			CalculateBolus();
		}
	}

	void EnableStart(bool en){
		_bottomOptions[0].Setup(en,false,"DELAY\nOPTIONS", this);
		_bottomOptions[1].Setup(true,false,"VOLUME\nDURATION",this);
		_bottomOptions[2].Setup(en,false,"SECOND-\nARY",this);
		_bottomOptions[3].Setup(en,true,"START",this);
		_canStart=en;
		_prompt.text="> Press START";
	}

	void SetupPrimaryInfusion(){
		ChannelInfo ci = _channelInfo[_selectedChannel];
		ci.Setup(false);
		ci.SetRate(_setupOptions[0]._val.text);
		ci.SetFluid(_lastFluid);
		ci.SetVolume(_setupOptions[1]._val.text);
		ci.SetRunning(true,this);
	}

	void SetupBolusInfusion(){
		ChannelInfo ci = _channelInfo[_selectedChannel];
		ci.SetFilled(true,this);
		ci.Setup(false);
		//set dose
		ci.SetFluid(_lastFluid);
		ci.SetBolusDose(_setupOptions[2]._val.text);
		ci._bolusDur=float.Parse(_setupOptions[3]._val.text);
		//calculate rate
		//set rate
		float r = float.Parse(_bolusVolume.text)/(float.Parse(_setupOptions[3]._val.text)/60f);
		ci.SetBolusRate(r.ToString("#.#"));
		//set volume
		ci.SetBolusVolume(_bolusVolume.text);
		_setupOptions[2].ClearVal();
		_setupOptions[3].ClearVal();
		ci.SetRunning(true,this);
	}

	void BolusMenu(){
		_state=11;
		//setup bolus screen
		ChannelInfo ci = _channelInfo[_selectedChannel];
		_header.text=_lastFluidType+" Setup";
		_subHeader.text=ci._fluid;
		//this sets prompt text
		EnableStart(true);
		//blink channel label
		ci.Setup(true);
		_fluidSetupScreen.alpha=1f;
		_fluidSetupTitle.text="BOLUS DOSE";
		//show patient weight total dose and bolus vtbi
		_bolusMenu.alpha=1f;
		_setupOptions[2]._val.text=ci._bolusDose.ToString();
		_setupOptions[3]._val.text=ci._bolusDur.ToString();
		bool canStart=_setupOptions[2].HasVal()&&_setupOptions[3].HasVal();
		EnableStart(canStart);
		//displays total dose and bolus volume
		CalculateBolus();
		_patientWeight.text="85";
		//hide rate and volume
		//show dose, duration
		_setupOptions[0].Show(false);
		_setupOptions[1].Show(false);
		_setupOptions[2].Show(true);
		_setupOptions[3].Show(true);
		_setupOptions[2].Select(false,this);
		_setupOptions[3].Select(false,this);
		if(canStart){
			//prompt
			_prompt.text="> Press START to Continue\nInfusing Bolus Dose";
			//bottom options
			_bottomOptions[0].Setup(true,true,"PAUSE", this);
			_bottomOptions[1].Setup(true,true,"STOP\nBOLUS",this);
			_bottomOptions[2].SetVisible(false);
			_bottomOptions[3].Setup(true,true,"START", this);
		}
		else{
			//prompt
			_prompt.text="> Enter Dose";
			//bottom options
			_bottomOptions[0].SetVisible(false);
			_bottomOptions[1].Setup(true,false,"SETUP",this);
			_bottomOptions[2].Setup(true,true,"CONTI-\nNUOUS",this);
			_bottomOptions[3].SetVisible(false);
		}
		//hide text options
		_textOptionsParent.alpha=0;
		//hide right options
		_rightOptionsParent.alpha=0;
	}
	void SetupDose(){
		_state=12;
		_setupOptions[2].Select(true,this);
		_setupOptions[3].Select(false,this);
		EnableStart(false);
		_prompt.text="> Enter Dose";
	}
	void SetupDuration(){
		_state=13;
		_setupOptions[3].Select(true,this);
		_setupOptions[2].Select(false,this);
		EnableStart(false);
		_prompt.text="> Enter Duration";
	}

	void CalculateBolus(){
		//calcualte total dose
		float td = float.Parse(_setupOptions[2]._val.text)*float.Parse(_patientWeight.text);
		_totalDose.text=td.ToString();
		//calculate vtbi
		_bolusVolume.text=(td*.01f).ToString("#.#");
	}

	void StopBolusMenu(){
		//hide fluids menu
		_fluidSetupScreen.alpha=0;
		//enable text option
		_textOptionsParent.alpha=1;
		//enable right option
		_rightOptionsParent.alpha=1;
		_state=14;
		_screen.color=_green;
		_chId.color=_green;
		_textOptions[0].SetText("Stop Bolus and Start\nContinuous Infusion?");
		_prompt.text="> Press Yes or No";
		OfferYesNoOptions();
	}

	void StopInfusionMenu(){
		//hide fluids menu
		_fluidSetupScreen.alpha=0;
		//enable text option
		_textOptionsParent.alpha=1;
		//enable right option
		_rightOptionsParent.alpha=1;
		_state=15;
		_screen.color=_green;
		_chId.color=_green;
		_textOptions[0].SetText("Stop Continuous\nInfusion?");
		_prompt.text="> Press Yes or No";
		OfferYesNoOptions();
	}

	void StopBolus(){
		GoHome();
		ChannelInfo ci = _channelInfo[_selectedChannel];
		ci._bolus=false;
		ci.Setup(false);
		ci.RevertContinuous();
		ci.SetFluid(_lastFluid);
		ci.SetRunning(true,this);
		//disable right option
		_rightOptionsParent.alpha=0;
	}

	void StopInfusion(){
		GoHome();
		ChannelInfo ci = _channelInfo[_selectedChannel];
		ci.Init(this);
		ci.Setup(false);
		_rightOptionsParent.alpha=0;
	}

	public void SetChannelFluid(int ch, RoomConfig.IVBag bag){
		_channelInfo[ch].SetIVBag(bag);
	}

	public int GetFreeChannelFluid(){
		ChannelInfo targetChannel = _channelInfo.Where(x => String.IsNullOrEmpty(x._fluidActual)).FirstOrDefault();
		if(targetChannel != null)
			return _channelInfo.IndexOf(targetChannel);
		else
			return -1;
	}

	IEnumerator PrimaryInfusion(ChannelInfo ci){
		//while volume > 0
		//	infuse some jaunt
		//	yield return a sec
		//	change this to only send when difference between volume and last sent
		//	is > 1
		float lastVol=ci._primVol;
		ci._drip.Play();
		ci.UpdateColor();
		float tmpTracker=0;
		while(ci._primVol>0){
			float amt = ci._primRate/3600;
			ci.Infuse(amt);
			//only send infuse topics when delta is > 1mL
			if(lastVol-ci._primVol>1f)
			{
				//make sure there's actually some fluid there + 0.01 fudge factor
				if(ci._lh._targetHeight>=-.01f && ci._lh._capacity>=-.01f){
					_mqtt.Infuse(ci._fluidActual,lastVol-ci._primVol);
					tmpTracker+=lastVol-ci._primVol;
				}
				lastVol=ci._primVol;
			}
			yield return new WaitForSeconds(1f);
		}
		ci.ClearColor();
		ci._primVol=0f;
		ci.SetVolume("0");
		ci.SetRunning(false,this);
		ci._drip.Stop();
	}

	public void PauseInfusion(int index){
		_channelInfo[index].SetRunning(false,this,true);
	}

	public void ToggleVis(bool vis){
		transform.parent.GetComponent<MeshRenderer>().enabled=vis;
		transform.parent.GetComponent<ClickDetection>().enabled=vis;
		foreach(Transform t in transform)
			t.gameObject.SetActive(vis);
	}

	public void OnSelectIvBag(int module){
		OffscreenMenuManager.Instance.OpenIVmenu(module);
	}
}
