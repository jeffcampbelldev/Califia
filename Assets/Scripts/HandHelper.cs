using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandHelper : MonoBehaviour
{

	public bool _leftHand;
	Inventory _inventory;
	Material _highlightMat;
	public int _highlightMatIndex;
	public float _outlineThickness;
    // Start is called before the first frame update
    void Start()
    {
		_inventory = transform.parent.parent.GetComponent<Inventory>();
		_highlightMat=GetComponent<MeshRenderer>().materials[_highlightMatIndex];
    }

	void OnEnable(){
		if(_inventory!=null)
		{
			_inventory.ReturnLeftHand();
			_inventory.ReturnRightHand();
		}
	}

    // Update is called once per frame
    void Update()
    {
        
    }

	void OnMouseEnter(){
		_highlightMat.SetFloat("_OutlineThickness",_outlineThickness);
	}

	void OnMouseExit(){
		_highlightMat.SetFloat("_OutlineThickness",0);
	}

	void OnMouseUpAsButton(){
		if(_leftHand)
			_inventory.ReturnLeftHand();
		else
			_inventory.ReturnRightHand();
	}
}
