using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SplashScreen : MonoBehaviour
{
	public float _introDelay;
	public Transform _pointTransform;
	public float _pointLightRange;
	public float _minLightRange;
	Light _pointLight;
	public Vector4 _range;
	public AnimationCurve _ekgCurve;
	public float _numPulses;
	public float _pulseDur;
	public float _pulseDelay;
	public float _spotDur;
	public Light _spotLight;
	public float _maxIntensity;
	public AnimationCurve _spotCurve;
	public float _spotDelay;
	public PitchGenerator _pitch;
	AudioSource _pitchAudio;
	public int _pitchFreq;
	public int _thirdFreq;
	public int _fifthFreq;
	public float _pitchDelay;
	public AudioSource _chime;
	public Color _redLight;
	public Material _logoMat;
	public MeshRenderer _screenMesh;
	public Material _heartMat;
	public CanvasGroup _cg;
	float z;
    // Start is called before the first frame update
    void Start()
    {
		_pitchAudio = _pitch.GetComponent<AudioSource>();
		_pointLight = _pointTransform.GetComponent<Light>();
		z = _pointTransform.position.z;
		//_heartMat.SetFloat("_Intensity",0);
		StartCoroutine(Pulse());
    }

    // Update is called once per frame
    void Update()
    {
        
    }

	IEnumerator Pulse(){
		yield return new WaitForSeconds(_introDelay);
		for(int i=0; i<_numPulses; i++){
			float timer=0;
			switch(i){
				case 0:
				default:
					_pitch.GeneratePitch(_pitchFreq);
					break;
				case 1:
					_pitch.GeneratePitch(_thirdFreq);
					break;
				case 2:
					_pitch.GeneratePitch(_fifthFreq);
					break;
			}
			_pointLight.range = Mathf.Lerp(_minLightRange,_pointLightRange,((i+1)/(float)_numPulses));
			_pitchAudio.PlayDelayed(_pulseDur*_pitchDelay);
			while(timer<_pulseDur){
				float timeFrac = timer/_pulseDur;
				_pointTransform.position = new Vector3(Mathf.Lerp(_range.x,_range.y,timeFrac),
						Mathf.Lerp(_range.z,_range.w,_ekgCurve.Evaluate(timeFrac)),z);
				timer+=Time.deltaTime;
				//_heartMat.SetFloat("_Intensity",Mathf.Lerp(0,2f,Mathf.InverseLerp(.7f,1f,1f-Mathf.Abs(.7f-timeFrac))));
				yield return null;
			}
			yield return new WaitForSeconds(_pulseDelay);
			//use two frames to bypass the line renderer behind the main screen
			_pointTransform.position = new Vector3(_range.y,0,-20f);
			yield return null;
			_pointTransform.position = new Vector3(_range.x,0,-20f);
			yield return null;
		}
		StartCoroutine(Spot());
	}
	IEnumerator Spot(){
		_chime.Play();
		//_screenMesh.material=_logoMat;
		float timer=0;
		while (timer < _spotDur){
			float timeFrac = timer/_spotDur;
			float cur = _spotCurve.Evaluate(timeFrac);
			//_spotLight.intensity = Mathf.LerpUnclamped(0,_maxIntensity,cur);
			//_spotLight.color = Color.Lerp(_redLight,Color.white,cur);
			_cg.alpha=cur;
			timer+=Time.deltaTime;
			yield return null;
		}
		/*
		//_spotLight.intensity=_maxIntensity;
		yield return new WaitForSeconds(_spotDelay);
		timer=0;
		while(timer<_spotDur){
			float timeFrac = timer/_spotDur;
			float cur = _spotCurve.Evaluate(timeFrac);
			//_spotLight.intensity = Mathf.LerpUnclamped(_maxIntensity,0,cur);
			timer+=Time.deltaTime;
			yield return null;
		}
		//_spotLight.intensity=0;
		SceneManager.LoadScene(1);
		*/
	}
}
