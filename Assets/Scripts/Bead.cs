using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bead : MonoBehaviour
{
	public float _minY, _maxY;
	public float _lerpRate;
	public enum NumberModes {WHOLE, FRAC, MIXED};
	public NumberModes _numberMode;
	Vector3 _targetPos;
	public Knob _knob;
    // Start is called before the first frame update
    void Start()
    {
		_targetPos=transform.localPosition;
    }

    // Update is called once per frame
    void Update()
    {
		transform.localPosition = Vector3.Lerp(transform.localPosition,_targetPos,_lerpRate*Time.deltaTime);
    }

	public void KnobValChanged(){
		float val01=0f;
		switch(_numberMode){
			case NumberModes.WHOLE:
				val01 = Mathf.InverseLerp(_knob._minVal,_knob._maxVal,_knob._val);
				break;
			case NumberModes.FRAC:
				val01 = _knob._val%1f;
				break;
			case NumberModes.MIXED:
				val01 = Mathf.InverseLerp(_knob._minVal,_knob._maxVal,_knob._val);
				break;
			default:
				break;
		}
		_targetPos.y = Mathf.Lerp(_minY,_maxY,val01);
	}
}
