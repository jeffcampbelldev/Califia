using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ARController : MonoBehaviour
{
	CanvasGroup _splashCanvas;
	InputField _prefixField;
	InputField _addressField;
	MyMQTT _mqtt;
    // Start is called before the first frame update
    void Start()
    {
		//get references
		_splashCanvas = GameObject.Find("SplashCanvas").GetComponent<CanvasGroup>();
		_prefixField = GameObject.Find("PrefixField").GetComponent<InputField>();
		_addressField = GameObject.Find("AddressField").GetComponent<InputField>();
		_mqtt = FindObjectOfType<MyMQTT>();

		//hookup events
		_prefixField.onEndEdit.AddListener(delegate {ChangePrefix();});
		_addressField.onEndEdit.AddListener(delegate {ChangeAddress();});

		//start splash fade
		StartCoroutine(SplashFadeR());
    }

	IEnumerator SplashFadeR(){
		_splashCanvas.alpha=1f;
		yield return new WaitForSeconds(2.5f);
		float timer = 0;
		while(timer<1f){
			timer+=Time.deltaTime;
			_splashCanvas.alpha=1-timer;
			yield return null;
		}
		Destroy(_splashCanvas.gameObject);
		
		//fill data fields
		_prefixField.text=_mqtt._prefix;
		_addressField.text=_mqtt._brokerAddress;
	}

	void ChangePrefix(){
		Debug.Log("Prefix changed to: "+_prefixField.text);
		_mqtt.ChangePrefix(_prefixField.text);
		_mqtt.Init(true);
	}

	void ChangeAddress(){
		Debug.Log("Address changed to: "+_addressField.text);
		_mqtt.ChangeBrokerAddress(_addressField.text);
		_mqtt.Init(true);
	}

    // Update is called once per frame
    void Update()
    {
        
    }
}
