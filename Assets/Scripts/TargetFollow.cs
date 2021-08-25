using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetFollow : MonoBehaviour
{
	public Transform _target;
	public float _lerpRate;
	Transform _follow;
	bool _tracking=false;
    // Start is called before the first frame update
    void Start()
    {
		_follow = transform.GetChild(0);
		_follow.gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
		//game object activation
		if(!_tracking){
			if(_target.gameObject.activeInHierarchy)
			{
				_tracking=true;
				_follow.gameObject.SetActive(true);
				//on tracking found, snap to target location
				_follow.position=_target.position;
				_follow.rotation=_target.rotation;
			}
		}
		else{
			//position and rotation tracking
			_follow.position = Vector3.Lerp(_follow.position,_target.position,Time.deltaTime*_lerpRate);
			_follow.rotation = Quaternion.Slerp(_follow.rotation,_target.rotation,Time.deltaTime*_lerpRate);

			//check for loss of tracking
			if(!_target.gameObject.activeInHierarchy){
				_tracking=false;
				_follow.gameObject.SetActive(false);
			}

		}

    }
}
