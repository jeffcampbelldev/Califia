using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PitchGenerator : MonoBehaviour
{
	public AnimationCurve _envelope;
	public float _dur;
	public int _sampleRate = 44100;
	public int _startFreq = 2000;
	// Start is called before the first frame update
	void Start()
	{
		GeneratePitch(_startFreq);
	}

	public void GeneratePitch(int freq){
		//generate float array
		float [] samples = new float[(int)(_dur*_sampleRate)];
		float max=-1f;
		float min = 1f;
		for(int i=0; i<samples.Length; i++){
			float t = i/(float)_sampleRate;
			float e = i/(float)samples.Length;
			float samp = Mathf.Cos(t*2*Mathf.PI*freq)*_envelope.Evaluate(e);
			if(t>max)
				max=t;
			if(t<min)
				min=t;
			samples[i]=samp;
		}
		AudioClip clip = AudioClip.Create("beep"+freq,samples.Length,1,_sampleRate,false);
		clip.SetData(samples,0);
		AudioSource ais = GetComponent<AudioSource>();
		ais.clip=clip;
	}

	// Update is called once per frame
	void Update()
	{

	}
}
