//todo comment
//and add volume
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
#if UNITY_STANDALONE_WIN || UNITY_EDITOR
	using Microsoft.CognitiveServices.Speech;
	using Microsoft.CognitiveServices.Speech.Audio;
#endif
using UnityEngine;

public class AzureSpeechService : MonoBehaviour
{

#if UNITY_STANDALONE_WIN || UNITY_EDITOR

	SpeechConfig _speechConfig;
	SpeechSynthesizer _speechSynth;
	string [] _template;
    // Start is called before the first frame update
    void Start()
    {
		_speechConfig = SpeechConfig.FromSubscription("970a11214e134604a96094a2fcd03e02",
				"westus2");
		_speechSynth = new SpeechSynthesizer(_speechConfig);
		_template = File.ReadAllLines(Application.streamingAssetsPath+"/AzureSpeechSsmlTemplate.xml");
    }

    // Update is called once per frame
    void Update()
    {
        
    }

	public void Speak(string msg,string voice = "en-US-JennyNeural"){
		string ssml="";
		for(int i=0; i<_template.Length; i++){
			if(i==5)
				ssml+=msg;
			else if(i==2)
				ssml+=voice;
			else
				ssml+=_template[i];
		}
		Task foo = SynthesizeAudioAsync(ssml);
	}

	[ContextMenu("Cancel Speech")]
	public void CancelSpeak(){
		_speechSynth.StopSpeakingAsync();
	}

	async Task SynthesizeAudioAsync(string msg){
		await _speechSynth.SpeakSsmlAsync(msg);
	}
#else

		/*
	public void Speak(string msg,string voice = "en-US-JennyNeural"){
		string ssml="";
		for(int i=0; i<_template.Length; i++){
			if(i==5)
				ssml+=msg;
			else if(i==2)
				ssml+=voice;
			else
				ssml+=_template[i];
		}
		Debug.Log("no speak on Android");
	}
		*/

#endif
}
