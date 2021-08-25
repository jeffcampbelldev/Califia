using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Splash: MonoBehaviour
{
	public Material _logoMat;
	float _startSweep;
	public AudioSource _pulse;
	public AudioSource _chime;
	public AudioSource _sweep;

   // Start is called before the first frame update
    void Start()
    {
		_startSweep = _logoMat.GetFloat("_SweepAmount");
		StartCoroutine(SplashRoutine());
		StartCoroutine(Pulse());
    }

    void Update()
    {
    }

	IEnumerator SplashRoutine(){
#if UNITY_ANDROID
		yield return new WaitForSeconds(2f);
#else
		yield return new WaitForSeconds(4f);
#endif
		float timer=0;
		while(timer<1f){
			timer+=Time.deltaTime;
			_logoMat.SetFloat("_SweepAmount",timer);
			yield return null;
		}

		yield return new WaitForSeconds(4);

		if(FindObjectOfType<MyMQTT>()!=null)
			FindObjectOfType<MyMQTT>().SendPub("Room",0);
		SceneManager.LoadScene(1);
	}

	IEnumerator Pulse(){
		yield return new WaitForSeconds(.1f);
		for(int i=0; i<3; i++){
			_pulse.Play();
			yield return new WaitForSeconds(1);
		}
		_chime.Play();
		yield return new WaitForSeconds(.6f);
		_sweep.Play();
	}

	void OnDestroy(){
		_logoMat.SetFloat("_SweepAmount",_startSweep);
	}
}
