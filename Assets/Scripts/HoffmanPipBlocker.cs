using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HoffmanPipBlocker : MonoBehaviour
{
	BoxCollider _blocker;
    // Start is called before the first frame update
    void Start()
    {
        
    }

	void OnEnable(){
		if(_blocker==null)
			_blocker = GameObject.Find("HoffmanBlocker").GetComponent<BoxCollider>();
		_blocker.enabled=true;
	}

	void OnDisable(){
		if(_blocker==null)
			_blocker = GameObject.Find("HoffmanBlocker").GetComponent<BoxCollider>();
		_blocker.enabled=false;
	}

    // Update is called once per frame
    void Update()
    {
        
    }
}
