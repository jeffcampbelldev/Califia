using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MoveCameraVertical : MonoBehaviour
{
	public float _minHeight;
	public float _maxHeight;
	[SerializeField] private Slider _heightSlider;
	private TestCam _cam;

	private void Start()
	{
		_cam = FindObjectOfType<TestCam>();
	}

	public void UpdateCamHeight(float value)
	{
		if(_cam!=null)
			_cam.SetHeight(Mathf.Lerp(_minHeight,_maxHeight,value));
	}

	public void UpdateSlider(float f){
		_heightSlider.value=Mathf.InverseLerp(_minHeight,_maxHeight,f);
	}

}
