using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BedManager : MonoBehaviour
{
	public GameObject[] _beds;
	public Transform _patient;
	public Transform _clipboard;
	public Transform [] _patientTargets;
	public Transform [] _clipboardTargets;
	public Transform [] _blanketTargets;
	public Transform _blanket;
    // Start is called before the first frame update
    void Start()
    {
		SetBed(0);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

	public void SetBed(int index){
		if(index>=_beds.Length)
			return;
		for(int i=0; i<_beds.Length; i++){
			_beds[i].SetActive(i==index);
			if(index==i)
			{
				_patient.SetParent(_patientTargets[i]);
				_patient.localPosition = Vector3.zero;
				_clipboard.SetParent(_clipboardTargets[i]);
				_clipboard.localPosition= Vector3.zero;
				_blanket.SetParent(_blanketTargets[i]);
				_blanket.localPosition = Vector3.zero;
			}
		}
	}

	public void SetBedWidth(float f){
		Vector3 scale = new Vector3(f,1,1);
		Vector3 inverseScale = new Vector3(1/f,1,1);
		foreach(GameObject g in _beds){
			g.transform.localScale=scale;
			foreach(Transform t in g.transform)
				t.localScale=inverseScale;
		}
	}
}
