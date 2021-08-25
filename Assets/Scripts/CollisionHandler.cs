using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class CollisionHandler : MonoBehaviour
{

	public UnityEvent _triggerEnter;
    // Start is called before the first frame update
    void Start()
    {
        
    }

	void OnTriggerEnter(Collider other){
		_triggerEnter.Invoke();
	}
}
