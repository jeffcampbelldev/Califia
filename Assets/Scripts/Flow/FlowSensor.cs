//FlowSensor.cs
//
//Description: Attached to the Flow Sensor box of the Ecmo Cart
//Manages the "Item Stand" for the clip-on flow sensor as well as
//text output for the sensor
//

//

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FlowSensor : MonoBehaviour
{
	Inventory _inv;
	public ItemStand _stand;
	public Text _flowReading;
	public Text _status;
	MFloat _flow;
    // Start is called before the first frame update
    void Start()
    {
		_inv = FindObjectOfType<Inventory>();
		_flow = new MFloat(_flowReading,"0.000");
    }

    // Update is called once per frame
    void Update()
    {
		_flow.Update();
    }

	public void TryReturnItem(){
		//if holding flow sensor
		//item stand return
		if(_inv.HasItem("FlowSensor"))
			_stand.ReturnItem();
	}

	public void TryHideSensor(){
		_stand.HideItem();
	}

	public void SetTarget(float f){
		_flow._target=f;
	}
}
