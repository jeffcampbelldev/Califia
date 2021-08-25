using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class NpcNavHelper : MonoBehaviour
{
	public Vector3 _entrance;
	public Vector3 _patient;
	[System.Serializable]
	public struct Waypoint{
		public Vector3 _pos;
		public string _label;
	}
	public Waypoint [] _locs;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
#if UNITY_EDITOR
	void OnDrawGizmos(){
		Gizmos.color = Color.green;
		Gizmos.DrawSphere(_entrance,.2f);
		Gizmos.color = Color.red;
		Gizmos.DrawSphere(_patient,.2f);
		Gizmos.color = Color.yellow;
		foreach(Waypoint loc in _locs){
			Gizmos.DrawSphere(loc._pos,.2f);
			Handles.Label(loc._pos+Vector3.up*.3f,loc._label);
		}
	}
#endif
}
