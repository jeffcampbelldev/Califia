using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AirMixer : MonoBehaviour
{
	public Text _fio2;
	public Text _airRate;
	public Knob _knobFio2;
	public Knob _knobAir;
	public Knob _knobAirMillis;

	public GameObject[] _digis;

	public Transform _outflowNode;
	public Transform _outflowFx;
	Vector3 _outflowDefault;
	public AudioClip _pop, _air;
	AudioSource _audio;
	ParticleSystem _airP;
	[HideInInspector]//assigned in ecmo cart
	public ClickDetection _airClick;
	EcmoCart _cart;
	MyMQTT _mqtt;
    // Start is called before the first frame update
    void Start()
    {
		//to prevent making scene changes just get this thing by name
		_knobAirMillis = transform.Find("knobmL").GetComponent<Knob>();
		_outflowDefault = _outflowNode.position;
		_audio = _outflowFx.GetComponent<AudioSource>();
		_airP = _outflowFx.GetComponent<ParticleSystem>();
		_airClick.enabled=false;
		MyMQTT [] qts = FindObjectsOfType<MyMQTT>();
		foreach(MyMQTT qt in qts){
			if(qt.gameObject.tag=="GameController")
				_mqtt=qt;
		}
		_cart = FindObjectOfType<EcmoCart>();
		if(_cart!=null)
			SyncVals(_cart._airData);
    }

    // Update is called once per frame
    void Update()
    {
    }

	public void SetFio2(float val){
		_fio2.text=val.ToString("0");
		_knobFio2.SetValue(val);
	}
	public void SetAirRate(float val){
		_airRate.text=val.ToString("0.000");
		_knobAir.SetValue(val);
		_knobAirMillis.SetValue(0f);
	}

	public void UpdateAirRate(){
		_cart._airData._rate=_knobAir._val+_knobAirMillis._val;
	}

	public void UpdateFio2(){
		_cart._airData._fio2=_knobFio2._val;
	}

	public void ToggleDigi(int val){
		bool active = val==1;
		foreach(GameObject go in _digis)
			go.SetActive(active);
	}

	public void DisconnectOutflow(){
		//start a coroutine
		StartCoroutine(DisconnectR());
	}

	public void ManualConnectOutflow(){
		ConnectOutflow();
	}

	public void ConnectOutflow(){
		float f;
		float.TryParse(_airRate.text, out f);
		_mqtt.ReconnectGasOutflow(f);
		_airClick.enabled=false;
		StartCoroutine(ConnectR());
	}

	IEnumerator DisconnectR(){
		_outflowNode.position = _outflowDefault;
		Vector3 outPos = _outflowNode.position+Vector3.down*.4f;
		_audio.clip=_pop;
		_audio.loop=false;
		_audio.Play();
		_airP.Play();
		float timer=0;
		while(timer<1f){
			timer+=Time.deltaTime;
			_outflowNode.position=Vector3.Lerp(_outflowDefault,outPos,timer);
			yield return null;
		}
		_outflowNode.position=outPos;
		_audio.clip=_air;
		_audio.loop=true;
		_audio.Play();
		_airClick.enabled=true;
	}

	IEnumerator ConnectR(){
		Vector3 startPos = _outflowNode.position;
		float timer=0;
		while(timer<1f){
			timer+=Time.deltaTime;
			_outflowNode.position=Vector3.Lerp(startPos,_outflowDefault,timer);
			yield return null;
		}
		_outflowNode.position=_outflowDefault;
		_audio.loop=false;
		_audio.Stop();
		_airP.Stop();
	}

	public void ToggleVis(bool vis){
		//toggle renderers
		MeshRenderer[] meshes = transform.GetComponentsInChildren<MeshRenderer>();
		foreach(MeshRenderer mr in meshes)
			mr.enabled=vis;
		//enable knobs
		Knob[] k = transform.GetComponentsInChildren<Knob>();
		foreach(Knob kn in k)
			kn.enabled=vis;
		//enable click detections
		ClickDetection [] cd = transform.GetComponentsInChildren<ClickDetection>();
		foreach(ClickDetection c in cd)
			c.enabled=vis;
	}

	public void SyncVals(EcmoCart.AirData ad){
		SetFio2(ad._fio2);
		SetAirRate(ad._rate);
	}
}
