using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConveyorBelt : MonoBehaviour
{

	public float _maxOffset;
	public float _scaleMultiplier;
	public float _minScale;
	public float _targetAngle;
	public float _defaultAngle;

	public float _itemSpacing;
	public float _minX, _maxX;
	Vector3 _targetPosition;
	Vector3 _rootPosition;
	public float _lerpSpeed;
	public Vector3 _eul, _axis,_pos;
	float _min,_max;
	float _parentX;
    // Start is called before the first frame update
    void Start()
    {
		_rootPosition=transform.position;
		_parentX=transform.parent.position.x;
		Configure();
    }

	public void Configure(){
		_targetPosition = _rootPosition;
		_minX=transform.position.x;
		_maxX=_minX+(transform.childCount-1)*_itemSpacing;
	}

	Transform tmp;
    // Update is called once per frame
    void Update()
    {
		transform.position=Vector3.Lerp(transform.position,_targetPosition,_lerpSpeed*Time.deltaTime);
		for(int i=0; i<transform.childCount; i++){
			tmp=transform.GetChild(i);
			float offset = Mathf.Abs(tmp.position.x-_parentX);
			//float offset01 = 1-Mathf.InverseLerp(0,_itemSpacing,offset);
			float offset01 = Mathf.Clamp01(1-offset/_itemSpacing);
			tmp.localScale = Vector3.one*Mathf.Lerp(_minScale,_scaleMultiplier,offset01);
			float xAng = Mathf.Lerp(_defaultAngle,_targetAngle,offset01);
			tmp.localEulerAngles = _eul+_axis*xAng;//new Vector3(0,xAng,0);
			tmp.localPosition=Vector3.right*_itemSpacing*i+offset01*_pos;
		}
    }

	public int GetSelectedObject(){
		int closestIndex=-1;
		float minDist=100f;
		foreach(Transform t in transform){
			if(Mathf.Abs(t.position.x-transform.parent.position.x)<minDist)
			{
				minDist=Mathf.Abs(t.position.x-transform.parent.position.x);
				closestIndex=t.GetSiblingIndex();
			}
		}
		return closestIndex;
	}

	public void SetSelected(int sibling){
		if(sibling<0)
			return;
		Vector3 pos = transform.position;
		pos.x=_rootPosition.x+_itemSpacing*sibling;
		transform.position=pos;
		_targetPosition=pos;
	}

	public void CycleOptions(float dir){
		Vector3 clamped=_targetPosition+Vector3.right*dir*_itemSpacing;
		if(dir>0)
			clamped.x = Mathf.Min(_maxX,clamped.x);
		else
			clamped.x = Mathf.Max(_minX,clamped.x);
		_targetPosition = clamped;
	}

	public void SetRange(List<int> range){
		if(range.Count==0){
			_min=0;
			_max=0;
		}
		else{
			_min=1000;
			_max=-1;
			foreach(int i in range){
				if(i>_max)
					_max=i;
				if(i<_min)
					_min=i;
			}
		}
		_minX=_rootPosition.x+_min*_itemSpacing;
		_maxX=_rootPosition.x+_max*_itemSpacing;
	}
}
