using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaterialManager : MonoBehaviour
{
	public Material [] _flooring;
	public MeshRenderer _floor;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

	public void SetFloor(int index){
		if(_floor==null)
		{
			Debug.Log("Error no floor found in current scene");
			return;
		}
		if(index<_flooring.Length)
			_floor.material=_flooring[index];
		else
			Debug.Log("Error floor index out of bounds");
	}

	public string SerializeFlooring(){
		string floorStr = "";
		for(int i=0; i<_flooring.Length; i++){
			floorStr+=(i+" = "+_flooring[i].name+"\n");
		}
		floorStr+="\n";
		return floorStr;
	}
}
