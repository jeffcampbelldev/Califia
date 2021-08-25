//MFloat.cs
//
//Description: Helper class to facilitate floating point
//text displays on various hardware in the scene
//Data received via mqtt can be sparse so this class helps animate those values
//by Lerping to a target at a fixed lerpRate across all floating point values
//

//

using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class MFloat
{
	public Text[] _text;
	CanvasGroup[] _cgs;
	public string _textConversion;
	[HideInInspector]
	public float _val;
	[HideInInspector]
	public float _target;
	static float _lerpRate=1f;
	float _timer=0;

	public MFloat(Text[] txt, string tc){
		_text=txt;
		_cgs = new CanvasGroup[_text.Length];
		for(int i=0; i<_text.Length; i++){
			CanvasGroup cg = _text[i].transform.GetComponentInParent<CanvasGroup>();
			if(cg != null)
				_cgs[i]=cg;
			else
				_cgs[i]=null;
		}
		_textConversion=tc;
		_val=0;
		_target=0;
	}

	public MFloat(Text txt, string tc){
		_text=new Text[]{txt};
		_cgs = new CanvasGroup[_text.Length];
		for(int i=0; i<_text.Length; i++){
			CanvasGroup cg = _text[i].transform.GetComponentInParent<CanvasGroup>();
			if(cg != null)
				_cgs[i]=cg;
			else
				_cgs[i]=null;
		}
	   	_textConversion=tc;
		_val=0;
		_target=0;
	}

	public void SnapTo(float f){
		_val=f;
		_target=f;
		for(int i=0; i<_text.Length; i++){
			_text[i].text=_val.ToString(_textConversion);
			//if(_cgs[i]!=null && _cgs[i].alpha>0)
		}
	}

	public void Update(){
		if(Mathf.Abs(_target-99999)<0.1f){
			_val=_target;
			foreach(Text t in _text){
				t.text="---";
			}
			return;
		}
		if(Mathf.Abs(_val-_target)>1000f){
			SnapTo(_target);
		}
		else if(Mathf.Abs(_val-_target)>0.001){
			_val = Mathf.Lerp(_val,_target,_lerpRate*Time.deltaTime);
			_timer+=Time.deltaTime;
			if(_timer>1f)
			{
				foreach(Text t in _text){
					t.text=_val.ToString(_textConversion);
				}
				_timer=0f;
			}
		}
		else
		{
			SnapTo(_target);
		}
	}
}
