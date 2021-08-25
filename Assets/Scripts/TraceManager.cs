//TraceManager.cs
//
//Description: Renders vital traces to a render texture
//Uses text file data as a starting point but can be swapped out via mqtt
//Currently requires cycle data to be exactly 400 floats
//[something about rendering mode]
//

//

using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class TraceManager : MonoBehaviour
{
	public RenderTexture _slideBuffer;
	public RenderTexture _rtDraw;
	public Material _slideMat;
	RawImage _rawImg;
	Texture2D _buffer;
	Color32 [] _colorBuffer;
	Color32 [] _transBuffer;
	Color32 [] _blackBuffer;
	Color _transparent;
	float _scrollSpeed = 0.4f;//u space per second - this should be fixed
	float _cyclesPerSec=1.5f;//this is a conversion of heart_rate)
	float _cyclePeriod;
	//buffers are where the current frame's data get drawn to
	//dimensions are in units of pixels
	int _buffWidth=4;
	int _buffHeight=1000;
	float _buffHeightPerTrace;
	int _maxLineHeight=100;//prevents too many pixels from getting drawn and choking 
	//for calculating line thickness
	Dictionary<int,int> _prevValues = new Dictionary<int,int>();
	public float _thicknessMultiplier;
	
	//monitor audio
	PitchGenerator _pitch;
	AudioSource _beep;
	float _beepTimer;

	//monitor setting controls
	public Slider _slider;
	public Slider _speedSlider;
	public Toggle _toggle;
	public GameObject _monitorPanel;

	[System.Serializable]
	public class Trace {
		public Color _lineColor;
		public string _tempFile;//starting set of data
		public float _min;//used for auto scaling
		public float _max;//"
		public float[] _lastCycle;
		[HideInInspector]
		public Text _mainVal;
		[HideInInspector]
		public Text _diastole;
		[HideInInspector]
		public Text _systole;
		[HideInInspector]
		public Text _label;
		public bool _visible=true;
		[HideInInspector]
		public float _cyclesPerSec;
		[HideInInspector]
		public float _cyclePos=0;
		[HideInInspector]
		public float _xStart;
		[HideInInspector]
		public float _xEnd;
	}

	[System.Serializable]
	public struct TraceData{
		public float value;
		public float mean;
		public float diastole;
		public float systole;
		public float display_rate;
		public string color;
		public string trace_color;
		public float audio_level;
	}

	//trace colors and text fields should be set in inspector
	public Trace [] _traces;
	public Transform _statsContainer;
	MFloat [] _data;
	MyMQTT _mqtt;
	
	//these guys are supposed to help prevent changes on the material from being
	//detected by source control, but they don't always work
	//todo: investigate why
	float _initialOffset;
	Vector4 _initialVector;

	//render mode
	public enum RenderMode {SLIDE, SWEEP};
	public RenderMode _renderMode;
	float _sweepPos;

	// Start is called before the first frame update
	void Start()
	{
		_rawImg = GetComponent<RawImage>();
		_pitch = GetComponent<PitchGenerator>();
		//initialize and set up pixel buffers
		SetupPixelBuffers();
		_cyclePeriod = 1/_cyclesPerSec;
		//setup trace ui elements
		for(int i=0; i<_traces.Length; i++){
			Transform readout = _statsContainer.GetChild(i);
			_traces[i]._mainVal=readout.GetChild(0).GetComponent<Text>();
			_traces[i]._label = readout.GetChild(1).GetComponent<Text>();
			_traces[i]._label.text = _traces[i]._tempFile;
			_traces[i]._diastole = readout.GetChild(2).GetComponent<Text>();
			_traces[i]._systole = readout.GetChild(3).GetComponent<Text>();
			//set colors
			_traces[i]._mainVal.color = _traces[i]._lineColor;
			_traces[i]._label.color=_traces[i]._lineColor;
			//yes, hardcoded traces that use systole and diastole
			if(i==2 || i==3){
				_traces[i]._diastole.color=_traces[i]._lineColor;
				_traces[i]._systole.color=_traces[i]._lineColor;
			}
			else{
				Color bl = new Color(0,0,0,0);
				_traces[i]._diastole.color=bl;
				_traces[i]._systole.color=bl;
			}
			//tmp
			if(i==5)
				_traces[i]._cyclesPerSec=0.33333f;
			else
				_traces[i]._cyclesPerSec=_cyclesPerSec;
		}
		_data = new MFloat[10];
		_data[0] = new MFloat(_traces[0]._mainVal,"0");//0 is ekg
		_data[1] = new MFloat(_traces[1]._mainVal,"0");//1 is pulse ox
		_data[2] = new MFloat(_traces[2]._mainVal,"0");//2 is abp mean
		_data[3] = new MFloat(_traces[2]._diastole,"0");//3 is abp diastole
		_data[4] = new MFloat(_traces[2]._systole,"0");//4 is abp systole
		_data[5] = new MFloat(_traces[3]._mainVal,"0");//5 is pap mean
		_data[6] = new MFloat(_traces[3]._diastole,"0");//6 is pap diastole
		_data[7] = new MFloat(_traces[3]._systole,"0");//7 is pap systole
		_data[8] = new MFloat(_traces[4]._mainVal,"0");//8 is cvp
		_data[9] = new MFloat(_traces[5]._mainVal,"0");//9 is etco2

		_beep = GetComponent<AudioSource>();

		//get mqtt handle
		MyMQTT [] qts = FindObjectsOfType<MyMQTT>();
		foreach(MyMQTT qt in qts){
			if(qt.gameObject.tag=="GameController")
				_mqtt=qt;
		}
		//preload traces with initial data
		int index=0;
		foreach(Trace trace in _traces){
			string dataPath = _mqtt.GetWorkPath(trace._tempFile+".txt");
			string[] data = File.ReadAllLines(dataPath);
			float[] fData = new float[data.Length];
			for(int i=0; i<data.Length; i++){
				fData[i]=float.Parse(data[i]);
			}
			trace._lastCycle=fData;
			//prefill prevValues
			_prevValues.Add(index,0);
			index++;
		}
		_initialOffset = _slideMat.GetFloat("_ScrollOffset");
		_initialVector = _slideMat.GetVector("_MyST");

		if(_slider!=null && PlayerPrefs.HasKey("TraceThickness")){
			_thicknessMultiplier=PlayerPrefs.GetFloat("TraceThickness");
			_slider.value=_thicknessMultiplier;
		}
		if(_speedSlider!=null && PlayerPrefs.HasKey("ScrollSpeed")){
			_scrollSpeed=PlayerPrefs.GetFloat("ScrollSpeed");
			_speedSlider.value=_scrollSpeed;
		}
		if(_toggle!=null && PlayerPrefs.HasKey("MonitorMode")){
			_renderMode = (RenderMode)PlayerPrefs.GetInt("MonitorMode");
			_toggle.isOn=(int)_renderMode==1;
		}
	}

	void OnDestroy(){
		_slideMat.SetFloat("_ScrollOffset",_initialOffset);
		_slideMat.SetVector("_MyST",_initialVector);
	}

	// Update is called once per frame
	void Update()
	{
		switch(_renderMode){
			case RenderMode.SLIDE:
			default:
				//slide buffer before blitting
				_slideMat.SetFloat("_ScrollOffset",_scrollSpeed*Time.deltaTime);
				break;
			case RenderMode.SWEEP:
				//no sliding in sweep mode
				_slideMat.SetFloat("_ScrollOffset",0f);
				break;
		}
		Graphics.Blit(_slideBuffer,_rtDraw,_slideMat,1);

		//draw sliver for next frame
		DrawSliver(Time.deltaTime);

		//audio
		if(_beepTimer<_cyclePeriod)
		{
			_beepTimer+=Time.deltaTime;
		}
		else{
			_beepTimer=0;
			_beep.Play();
		}

		//floating point values
		foreach(MFloat mf in _data)
			mf.Update();
	}

	//clear pixel buffers to black
	void SetupPixelBuffers(){
		Texture2D blackness = new Texture2D(_slideBuffer.width,_slideBuffer.height,TextureFormat.ARGB32,false);
		for(int y=0; y<blackness.height;y++){
			for(int x=0; x<blackness.width; x++){
				blackness.SetPixel(x,y,Color.black);
			}
		}
		blackness.Apply();
		//clear buffer render texture
		Graphics.Blit(blackness,_slideBuffer);//,_slideMat,0);
		//clear draw render texture
		Graphics.CopyTexture(_slideBuffer,_rtDraw);
		//Setup _buffer a texture2D which gets blitted to the render textures
		_buffHeightPerTrace=_buffHeight/(float)_traces.Length;
		_buffer=Texture2D.blackTexture;
		_buffer.Resize(_buffWidth,_buffHeight);
		_buffer.wrapMode=TextureWrapMode.Clamp;
		//_buffer.
		_buffer.filterMode=FilterMode.Bilinear;
		_transparent = new Color(0,0,0,0);
		int numPix = _buffWidth*_buffHeight;
		//This buffer stores color data for traces
		_colorBuffer = new Color32[numPix];
		//A buffer filled with transparent pixels
		_transBuffer = new Color32[numPix];
		//A buffer filled with black pixels
		_blackBuffer = new Color32[numPix];
		//initialize black and transparent buffers
		for(int i=0; i<_transBuffer.Length; i++){
			_transBuffer[i]=_transparent;
			_blackBuffer[i] =Color.black; 
		}
	}
	
	//Draws a sliver of data by sampling the cycle data and blitting pixels
	//to the end of the slide buffer Texture
	void DrawSliver(float dt){
		//copy back draw texture to slide buffer tex
		Graphics.CopyTexture(_rtDraw,_slideBuffer);

		//clear color buffer to transparent
		System.Array.Copy(_blackBuffer,_colorBuffer,_buffWidth*_buffHeight);

		//figure out sliver bounds within cycle space 0<->1
		foreach(Trace t in _traces){
			t._xStart = t._cyclePos;
			t._xEnd = t._xStart+dt*t._cyclesPerSec;
			t._cyclePos=t._xEnd;
			if(t._cyclePos>1)
				t._cyclePos=0;
		}
		/*
		float xStart = _cyclePos;
		float xEnd = xStart+dt*_cyclesPerSec;
		_cyclePos=xEnd;
		if(_cyclePos>1)
			_cyclePos=0;
			*/

		//fill the color buffer
		int pixCount=0;
		for(int x=0; x<_buffWidth; x++){
			//col = Color.HSVToRGB(xPos,1,1);
			pixCount=0;
			//loop through each trace
			for(int t=0; t<_traces.Length; t++){
				//map cycle space to an x position along the pixel buffer
				float xPos = Mathf.Lerp(_traces[t]._xStart,_traces[t]._xEnd,x/(float)_buffWidth);
				//calculate vertical boundaries along pixel buffer's y axis
				float yMin =(_buffHeightPerTrace)*(_traces.Length-(t+1));
				float yMax =(_buffHeightPerTrace)*(_traces.Length-t);
				//draw white lines at top and bottom
				_colorBuffer[Mathf.FloorToInt(yMin)*_buffWidth+x]=Color.white;
				_colorBuffer[Mathf.FloorToInt(yMax-1)*_buffWidth+x]=Color.white;
				Trace trace = _traces[t];
				//calc x cord within the sliver
				int xCord = Mathf.RoundToInt(Mathf.Lerp(0,399,xPos));
				//sample cycle data and normalize it, and map it between trace's vertical bounds
				int y = Mathf.RoundToInt(Mathf.Lerp(yMin,yMax,Mathf.InverseLerp(trace._min,trace._max,trace._lastCycle[xCord])));
				//determine min and max with previous values
				int prevY = _prevValues[t];
				int min = Mathf.Min(y,prevY);
				int max = Mathf.Max(y,prevY);
				//draw line from min to max inclusively
				for(int j=min;j<max+1;j++){
					int pix = j*_buffWidth+x;
					//make line thicc as needed
					for(int k=0;k<_thicknessMultiplier;k++){
						if(pix+k<_colorBuffer.Length)
							_colorBuffer[pix+k]=trace._lineColor;
					}
					//check for big jumps that cause the renderer to slow
					pixCount++;
					if(pixCount>_maxLineHeight)
					{
						break;
					}
				}
				//save previous value
				_prevValues[t]=y;
			}
		}

		//apply colored pixels to buffer texture
		_buffer.SetPixels32(_colorBuffer);
		_buffer.Apply();

		//calculate sliver position
		float u = dt*_scrollSpeed;
		float scale=1/(u);
		float offset = 0f;
		switch(_renderMode){
			case RenderMode.SLIDE:
			default:
				offset = 1-u;
				_slideMat.SetVector("_MyST", new Vector4(scale,1f,offset*scale,0));
				break;
			case RenderMode.SWEEP:
				//offset = 1-u;
				_sweepPos+=u;
				if(_sweepPos>1f)
					_sweepPos=0f;
				//scale*=0.9f;
				//_sweepPos*=0.9f;
				_slideMat.SetVector("_MyST", new Vector4(scale,1f,_sweepPos*scale,0));
				break;
		}
		//set the coordinates of the sliver
		//_slideMat.SetVector("_MyST", new Vector4(scale,1f,offset*scale,0));
		//blit the sliver onto the slidebuffer
		Graphics.Blit(_buffer,_slideBuffer,_slideMat,0);
	}


	//Helper is used when processing incoming mqtt topics that point to the same method
	int GetTraceIndexFromString(string trace){
		int traceIndex=-1;
		switch(trace){
			case "Monitor/ekg/trace":
				traceIndex=0;
				break;
			case "Monitor/pulse_ox/right/trace":
				traceIndex=1;
				break;
			case "Monitor/abp/trace":
				traceIndex=2;
				break;
			case "Monitor/pap/trace":
				traceIndex=3;
				break;
			case "Monitor/cvp/trace":
				traceIndex=4;
				break;
			case "Monitor/etco2/trace":
				traceIndex=5;
				break;
		}
		return traceIndex;
	}

	//data is an array of bytes representing 400 floats
	//assigns the trace's data array accordingly
	//todo: handle lengths other than 400
	public void ConvertCycleData(string trace,byte[] data){
		if(data.Length!=1600)
		{
			Debug.Log("Length (bytes) "+data.Length);
			return;
		}
		float[] floatArr = new float[data.Length / 4];

		float min=1000;
		float max = -1000;
		int j=0;
		for (int i = 0; i < data.Length; i += 4)
		{
			float convertedFloat = System.BitConverter.ToSingle(data, i);
			if(convertedFloat<min)
				min=convertedFloat;
			if(convertedFloat>max)
				max=convertedFloat;
			floatArr[j] = convertedFloat;
			j++;
		}
		
		//if index = 2 (abp), apply auto scaling
		//todo: maybe we don't hardcode trace 2 to get auto scaled
		int index = GetTraceIndexFromString(trace);
		_traces[index]._lastCycle=floatArr;
		if(index==2){
			float mMin = Mathf.FloorToInt(min/100f)*100;
			_traces[index]._min=mMin;
			float mMax = Mathf.CeilToInt(max/100f)*100;
			if(mMax==mMin)
				mMax+=100;
			_traces[index]._max=mMax;
		}
	}

	//color change handler for traces
	//col is in html format
	public void UpdateColor(string trace, string col){
		Color color;
		string colHex = "#"+col;
		if(!ColorUtility.TryParseHtmlString(colHex, out color))
			return;
		switch(trace){
			case "Monitor/ekg/":
				_traces[0]._lineColor=color;
				break;
			case "Monitor/pulse_ox/right/":
				_traces[1]._lineColor=color;
				break;
			case "Monitor/abp/":
				_traces[2]._lineColor=color;
				break;
			case "Monitor/pap/":
				_traces[3]._lineColor=color;
				break;
			case "Monitor/cvp/":
				_traces[4]._lineColor=color;
				break;
			case "Monitor/etco2/":
				_traces[5]._lineColor=color;
				break;
		}
	}


	public void UpdateTraceData(int i, float mv, float cps,float dia, float sys, string col, string tc){
		//set colors
		Color color = Color.white;
		string colHex = "#"+col;
		ColorUtility.TryParseHtmlString(colHex, out color);
		if(color==Color.black)
			color.a=0;
		_traces[i]._mainVal.color=color;
		if(i==2||i==3){
			_traces[i]._diastole.color=color;
			_traces[i]._systole.color=color;
		}
		_traces[i]._label.color=color;
		colHex = "#"+tc;
		ColorUtility.TryParseHtmlString(colHex, out color);
		_traces[i]._lineColor=color;
		//set display rates
		_traces[i]._cyclesPerSec=cps/60f;
		switch(i){
			case 0:
				_data[0]._target=mv;
				UpdateCycleWidth(cps);
				break;
			case 1:
				_data[1]._target=mv;
				break;
			case 2:
				_data[2]._target=mv;
				_data[3]._target=dia;
				_data[4]._target=sys;
				break;
			case 3:
				_data[5]._target=mv;
				_data[6]._target=dia;
				_data[7]._target=sys;
				break;
			case 4:
				_data[8]._target=mv;
				break;
			case 5:
				_data[9]._target=mv;
				break;
		}
	}

	//update float value targets
	/*
	public void UpdateVal(string trace, int val){
		switch(trace){
			case "Monitor/heart_rate/calc":
				_data[0]._target=val;//value displayed numerically on monitor
				break;
			case "Monitor/heart_rate/display":
				UpdateCycleWidth(val);//scroll rate
				break;
			case "Monitor/pulse_ox/right":
				_data[1]._target=val;
				break;
			case "Monitor/abp/mean":
				_data[2]._target=val;
				break;
			case "Monitor/abp/diastole":
				_data[3]._target=val;
				break;
			case "Monitor/abp/systole":
				_data[4]._target=val;
				break;
			case "Monitor/pap/mean":
				_data[5]._target=val;
				break;
			case "Monitor/pap/diastole":
				_data[6]._target=val;
				break;
			case "Monitor/pap/systole":
				_data[7]._target=val;
				break;
			case "Monitor/cvp":
				_data[8]._target=val;
				break;
		}
	}
	*/

	/*
	public void SetVolume(float vol){
		_beep.volume=vol;
	}

	public void SetFrequency(int freq){
		_pitch.GeneratePitch(freq);
	}
	*/

	public void SetAudio(float vol, int freq){
		_beep.volume=vol;
		_pitch.GeneratePitch(freq);
	}

	//called via changes in heart rate
	private void UpdateCycleWidth(float val){
		_cyclesPerSec=val/60f;
		_cyclePeriod = 1/_cyclesPerSec;
	}

	public void ToggleRenderMode(Toggle t){
		if(t.isOn)
			_renderMode=RenderMode.SWEEP;
		else
			_renderMode=RenderMode.SLIDE;
		PlayerPrefs.SetInt("MonitorMode",(int)_renderMode);
	}

	public void AdjustLineThickness(Slider s){
		_thicknessMultiplier=s.value;
		PlayerPrefs.SetFloat("TraceThickness",_thicknessMultiplier);
	}

	public void AdjustScrollSpeed(Slider s){
		_scrollSpeed=s.value;
		PlayerPrefs.SetFloat("ScrollSpeed",_scrollSpeed);
	}

	public void ShowMonitorPanel(bool show){
		_monitorPanel.SetActive(show);
		TestCam foo = FindObjectOfType<TestCam>();
		if(foo!=null)
			foo.enabled=!show;
		GameObject block = GameObject.FindGameObjectWithTag("Blocker");
		if(block!=null)
			block.GetComponent<BoxCollider>().enabled=show;
	}
}
