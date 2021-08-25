//Clock.cs
//
//Description: Polls local system time for use on ui components
//

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Clock : MonoBehaviour
{
	//output fields
	Text _time;
	Text _date;
	float _clock;
	//modifiers
	public bool _seconds;
	public bool _militaryTime;
	public bool _numDate;
	float _timeFactor=1f;
	System.DateTime _rootTime;
	//gametime may be offset and/or accelerated
	System.DateTime _gameTime;
	System.TimeSpan _offset;
	public bool _controlsWindow;
	public Material _windowMat;
    // Start is called before the first frame update
    void Start()
    {
		if(transform.Find("Time")!=null)
			_time=transform.Find("Time").GetComponent<Text>();
		else
			_time=GetComponent<Text>();
		if(transform.Find("Date")!=null)
			_date=transform.Find("Date").GetComponent<Text>();
		_rootTime = System.DateTime.Now;
		_gameTime=_rootTime;
		_offset = new System.TimeSpan(0);
		UpdateClock();
    }

    // Update is called once per frame
    void Update()
    {
		_clock+=Time.deltaTime*_timeFactor;
		if(!_seconds && _clock>=60f || _seconds && _clock>1f)
		{
			UpdateClock();
		}
    }

	void UpdateClock(){
		//calc number of ticks since last root was established
		System.DateTime cur = System.DateTime.Now;
		System.TimeSpan dur = cur-_rootTime; 
		long ticks = dur.Ticks;
		//update gametime using offset and timefactor
		_gameTime=_rootTime+_offset+new System.TimeSpan((long)(ticks*_timeFactor));
		if(_date!=null)
		{
			if(_numDate)
				_date.text=_gameTime.ToString("MM-dd-yyyy");
			else
				_date.text=_gameTime.ToString("MMM-dd-yyyy");
		}
		if(!_militaryTime){
			if(_seconds)
				_time.text=_gameTime.ToString("h:mm:ss tt");
			else
			{
				_time.text=_gameTime.ToString("h:mm tt");
			}
		}
		else{
			if(_seconds){
				_time.text=_gameTime.ToString("HH:mm:ss");
			}
			else{
				_time.text=_gameTime.ToString("HH:mm");
			}
		}
		_clock=0f;

		//determine brightness
		//
		if(_controlsWindow){
			int hour = _gameTime.Hour;
			int minute = _gameTime.Minute;
			float brightness = (hour*60+minute)/(float)1440;
			brightness = 1-(Mathf.Abs(.5f-brightness)*2f);
			_windowMat.SetFloat("_Brightness",brightness);
		}
	}

	public void SetSeconds(bool sec){
		_seconds=sec;
		UpdateClock();
	}

	public void SetTimeFactor(float f){
		_offset = _gameTime-_rootTime;
		_rootTime = System.DateTime.Now;
		_timeFactor=f;
		UpdateClock();
	}

	public void AdvanceTime(float min){
		_offset+=new System.TimeSpan((long)(min*System.TimeSpan.TicksPerMinute));
		UpdateClock();
	}
}
