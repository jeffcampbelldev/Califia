using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;


public class TouchJoystick : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private Vector2 _originalPos;
	Vector3 _diff;
	public float _radius;
    private bool isDragging = false;
    private RectTransform _transform = null;
	TestCam _cam;
	public float _sens = 0.001f;
	public bool _isRot;

    private void Start()
    {
        _transform = GetComponent<RectTransform>();
		_cam = FindObjectOfType<TestCam>();
    }

    private void Update()
    {
        if (isDragging)
        {
			if(!_isRot)
				_cam.AddTranslation(_diff,_diff.magnitude*_sens);
			else
				_cam.AddRotation(_diff,_diff.magnitude*_sens);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
		_diff = Input.mousePosition-transform.position;
		Vector3 norm = _diff.normalized;
		float mag = _diff.magnitude;
		if(mag>_radius){
			_diff=norm*_radius;
		}
		//_diff.x = Mathf.Clamp(_diff.x,_xMin,_xMax);
		//_diff.y = Mathf.Clamp(_diff.y,_yMin,_yMax);
		transform.localPosition=_diff;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        transform.localPosition = Vector3.zero;
        isDragging = false;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying)
        {
			Gizmos.DrawWireSphere(transform.position,_radius);
            //Gizmos.DrawWireCube(transform.position, new Vector3(Mathf.Abs(_xMax-_xMin), Mathf.Abs(_yMax-_yMin)));
            
        }
    }
#endif
}
