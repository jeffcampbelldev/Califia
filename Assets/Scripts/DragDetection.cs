using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class DragDetection : MonoBehaviour
{

	bool _dragging=false;
	Vector3 _leftMost, _rightMost;
	float _maxDist;
	[HideInInspector]
	public float _horDrag;
	public UnityEvent _onHorizontalDrag;
	public UnityEvent _onDragOver;
	public LayerMask _layer;
	TestCam _tCam;
	Camera _mainCam;
	public float _maxDistToInteract;
	Vector3 _debugPoint=Vector3.zero;
    // Start is called before the first frame update
    void Start()
    {
		_leftMost = transform.position-transform.right*transform.localScale.x*.5f;
		_rightMost = transform.position+transform.right*transform.localScale.x*.5f;
		_maxDist=(_rightMost-_leftMost).magnitude;
    }

    // Update is called once per frame
    void Update()
    {
		if(_dragging) 
		{
			if(Input.GetMouseButtonUp(0))
			{
				EndDrag();
			}
			else{
				//drag in progress
				//cast mouse to box
				RaycastHit hit;
				Vector3 ray = (Vector3)Input.mousePosition;
				//clamp on
				if(Physics.Raycast(_mainCam.transform.position,_mainCam.ScreenPointToRay(ray).direction,out hit,_maxDistToInteract,_layer)&&hit.transform.name==transform.name){
					_debugPoint=hit.point;
					float dist = (_leftMost-hit.point).magnitude;
					float frac = dist/_maxDist;
					_horDrag=frac;
					_onHorizontalDrag.Invoke();
				//calc distance from leftmost point
				//calc fraction of sqrDist to maxDist(sqr)
				//publish dragging
				}
			}
		}
    }

	void OnMouseExit(){
		if(_dragging)
			EndDrag();
	}

	void OnMouseDown(){
		if(_mainCam==null){
			_tCam = FindObjectOfType<TestCam>();
			if(_tCam!=null)
				_mainCam = _tCam.GetComponent<Camera>();
			else
				return;
		}
		if((_mainCam.transform.position-transform.position).magnitude<=_maxDistToInteract)
			StartDrag();
	}

	void StartDrag(){
		_dragging=true;
		//disable testCam
		if(_tCam==null)
		{
			_tCam = FindObjectOfType<TestCam>();
			_mainCam = _tCam.GetComponent<Camera>();
		}
		_tCam.enabled=false;
	}

	void EndDrag(){
		_dragging = false;
		//enable test cam
		_tCam.enabled=true;
		_onDragOver.Invoke();
	}

	void OnDrawGizmos(){
		Gizmos.color=Color.red;
		Gizmos.DrawSphere(_debugPoint,.02f);
	}
}
