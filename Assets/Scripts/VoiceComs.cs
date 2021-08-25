using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoiceComs : MonoBehaviour
{
	bool _recording;
	MyMQTT _mqtt;
	int _sampleRate=8000;
	AudioSource _audio;
    // Start is called before the first frame update
    void Start()
    {
		//get mqtt handle
		MyMQTT [] qts = FindObjectsOfType<MyMQTT>();
		foreach(MyMQTT qt in qts){
			if(qt.gameObject.tag=="GameController")
				_mqtt=qt;
		}
		_audio = transform.GetChild(0).GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
		if(Input.GetKeyDown(KeyCode.Space)){
			StartCoroutine(TestRecordR());
		}
		else if(Input.GetKeyUp(KeyCode.Space))
			_recording=false;
    }

	IEnumerator TestRecordR(){
		_recording=true;
		AudioSource audioSource = transform.GetChild(0).GetComponent<AudioSource>();
		AudioClip clip;
		while(_recording)
		{
			clip=Microphone.Start("", false, 1, _sampleRate);
			yield return new WaitForSeconds(1f);
			SendClip(clip);
		}
	}

	void SendClip(AudioClip c){
		//do some stuff
		//use channel 0 (mono audio)
		float[] fData = new float[c.samples * c.channels];
		c.GetData(fData, 0);

		byte[] bData = new byte[fData.Length * 4];
		Buffer.BlockCopy(fData, 0, bData, 0, bData.Length);

		_mqtt.SendClip(bData);
	}

	public void PlayClip(byte[] data){
		//#todo in future - audio clips and sources received should be pooled or created, and destroyed to account for N concurrent voices
		//create temporary audio clip
		AudioClip c = AudioClip.Create("foo", _sampleRate,1,_sampleRate,false);
		float[] samples = new float[data.Length/4];
		int counter=0;
		for(int i=0; i<data.Length; i+=4){
			float f = System.BitConverter.ToSingle(data, 0);
			samples[counter]=System.BitConverter.ToSingle(data,i);
			counter++;
		}
		c.SetData(samples,0);
		_audio.clip=c;
		_audio.Play();
	}
}
