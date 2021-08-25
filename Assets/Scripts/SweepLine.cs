using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SweepLine : MonoBehaviour
{
	public Vector4 _params;
	float timer = 0;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
		timer+=Time.deltaTime*_params.z;
		transform.localPosition = Vector3.right*Mathf.Lerp(_params.x,_params.y,timer)+Vector3.up*_params.w;
		//note this NOT a perfect timer, and should NOT be used or relied on as 
		//an accurate measurement of time.
		//Over time this system accumulates error that can ADD UP!!
		if(timer>1)
			timer=0;
    }
}
