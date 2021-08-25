using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PerformanceMonitor : MonoBehaviour
{
	float _samplePeriod=0.1f;
	float _updatePeriod=1f;
	float _sampleTimer;
	float _updateTimer;
	Queue<float> _lastFrames;
	public Text _avgFramesText;
    // Start is called before the first frame update
    void Start()
    {
		_lastFrames = new Queue<float>(Mathf.RoundToInt(_updatePeriod/_samplePeriod));
    }

	void OnEnable(){
		//Debug.Log("we live");
	}
	void OnDisable(){
		//Debug.Log("we dead");
	}

    // Update is called once per frame
    void Update()
    {
		_sampleTimer+=Time.deltaTime;
		_updateTimer+=Time.deltaTime;
		if(_sampleTimer>_samplePeriod){
			_lastFrames.Enqueue(1f/Time.deltaTime);
			_sampleTimer=0;
		}
		if(_updateTimer>_updatePeriod){
			float avg=0;
			int count=0;
			while(_lastFrames.Count>0)
			{
				avg+=_lastFrames.Dequeue();
				count++;
			}
			avg/=count;
			_avgFramesText.text=avg.ToString("0.0");
			_updateTimer=0;
		}
    }
}
