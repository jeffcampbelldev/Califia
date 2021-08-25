using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InfoBubble : MonoBehaviour
{
	public bool _snoozed;
	Vector3 _rootPos;
	public AnimationCurve _anim;
	float _animAmp=0.02f;
	public CanvasGroup _icon;
	public CanvasGroup _label;
	EcmoCart _ecmo;
	float _flowDelayTimer=0;
    // Start is called before the first frame update
    void Start()
    {
		_rootPos = transform.position;
		_icon.alpha=1f;
		_label.alpha= 1-_icon.alpha;
		_ecmo = FindObjectOfType<EcmoCart>();
    }

	void OnEnable(){
		_snoozed=false;
		_flowDelayTimer=0;
	}

    // Update is called once per frame
    void Update()
    {
		if(!_snoozed){
			transform.position = _rootPos + Vector3.up*_anim.Evaluate(Mathf.PingPong(Time.time,1f))*_animAmp;
		}
		//this may not be great because there will be other types of info bubbles
		//that are dependent on other factors for disappearing but for now
		//maybe a big switch statement will do
		if(_flowDelayTimer<5f)
			_flowDelayTimer+=Time.deltaTime;
		else if(_ecmo._pd._flowRate>0)
			gameObject.SetActive(false);
    }

	public void ShowLabel(bool show){
		/*
		StopAllCoroutines();
		StartCoroutine(ShowLabelR(show));
		*/
	}

	IEnumerator ShowLabelR(bool s){
		if(s){
			while(_icon.alpha>0f){
				_icon.alpha-=Time.deltaTime;
				_label.alpha=1-_icon.alpha;
				yield return null;
			}
		}
		else{
			while(_icon.alpha<1f){
				_icon.alpha+=Time.deltaTime;
				_label.alpha=1-_icon.alpha;
				yield return null;
			}
		}
		_icon.alpha=s?0f : 1f;
		_label.alpha = 1-_icon.alpha;
	}

	public void Snooze(){
		_snoozed=true;
		transform.position = _rootPos;
	}
}
