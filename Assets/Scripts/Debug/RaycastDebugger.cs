using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class RaycastDebugger : MonoBehaviour
{
	Camera _main;
	public LayerMask _layer;
	Vector3 _point;
    // Start is called before the first frame update
    void Start()
    {
		_main = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
		CheckUI();
		CheckColliders();
    }

	void CheckUI(){
		PointerEventData ped = new PointerEventData(EventSystem.current);
		ped.position = Input.mousePosition;
		List<RaycastResult> rr = new List<RaycastResult>();
		EventSystem.current.RaycastAll(ped,rr);
		for(int i=0; i<rr.Count; i++){
			Debug.Log("UI hit: "+rr[i].gameObject.name);
		}
	}

	void CheckColliders(){
		RaycastHit hit;
		Vector3 ray = (Vector3)Input.mousePosition;
		//clamp on
		if(Physics.Raycast(_main.transform.position,_main.ScreenPointToRay(ray).direction,out hit,4f,_layer)){
			Debug.Log("Collision hit: "+hit.transform.name);
			_point = hit.point;
		}
	}
	void OnDrawGizmos(){
		Gizmos.color=Color.yellow;
		Gizmos.DrawWireSphere(_point, .1f);
	}
}
