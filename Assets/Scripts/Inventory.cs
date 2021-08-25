using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Inventory : MonoBehaviour
{
	public Item [] _items;
	Transform _layoutGroup;
	Color _inactiveCol;
	ItemStand [] _stands;
	bool _isAnimating = false;

	public class Hand {
		[HideInInspector]
		public bool _hasItem;
		public Transform _transform;
		public Item _item;
		private Inventory _inv;

		public Hand(Transform t,Inventory i){
			_transform=t;
			_item=null;
			_inv=i;
		}

		public void AcquireItem(Item i){
			if (!_inv._isAnimating)
			{
				_item = i;
				_inv._isAnimating = true;
				_inv.StartCoroutine(_inv.EquipAnim(this));
				//activate obj mesh
				Transform p = _transform.GetChild(0);
				foreach (Transform t in p){
					t.gameObject.SetActive(t.name.Contains(i._name));
				}
			}
		}

		public bool HasItem(string name){
			if(_item!=null && _item._name==name)
				return true;
			return false;
		}

		public void UseItem(){
			if(!_inv._isAnimating)
			{
				_item = null;
				_inv._isAnimating = true;
				_inv.StartCoroutine(_inv.UnequipAnim(this));
				//deactivate obj mesh
				Transform p = _transform.GetChild(0);
				foreach (Transform t in p){
					t.gameObject.SetActive(false);
				}
			}
		}
		
		public void ResetHand(){
			_inv._isAnimating = true;
			_inv.StartCoroutine(_inv.UnequipAnim(this));
		}
	}

	public Hand _leftHand, _rightHand;

	public AnimationCurve _equipCurve;


	// Start is called before the first frame update
	void Start()
	{
		_leftHand = new Hand(transform.Find("LeftHand"),this);
		_rightHand = new Hand(transform.Find("RightHand"),this);
	}

	void OnEnable(){
		if(_stands!=null)
		{
			_leftHand.ResetHand();
			_rightHand.ResetHand();
		}
	}

	IEnumerator EquipAnim(Hand h){
		float timer=0;
		float dur=0.4f;
		while(timer<dur){
			timer+=Time.deltaTime;
			h._transform.localEulerAngles = Vector3.right*Mathf.LerpUnclamped(25f,0,_equipCurve.Evaluate(timer/dur));
			yield return null;
		}
		_isAnimating = false;
	}

	IEnumerator UnequipAnim(Hand h){
		float timer=0;
		float dur=0.2f;
		while(timer<dur){
			timer+=Time.deltaTime;
			h._transform.localEulerAngles = Vector3.right*Mathf.LerpUnclamped(25f,0,_equipCurve.Evaluate(1-timer/dur));
			yield return null;
		}
		_isAnimating = false;
	}

	// Update is called once per frame
	void Update()
	{
	}

	public void AddItem(Item i){
		if(_leftHand._item==null){
			//add item to left hand
			_leftHand.AcquireItem(i);
		}
		else if(_rightHand._item==null){
			//add item to right hand
			_rightHand.AcquireItem(i);
		}
	}

	public bool HasItem(string name){
		if(_leftHand==null)
			return false;
		if(_isAnimating)
			return false;
		if(_leftHand.HasItem(name))
			return true;
		if(_rightHand.HasItem(name))
			return true;
		return false;
	}

	public void UseItem(string name){
		if(_rightHand.HasItem(name))
			_rightHand.UseItem();
		else if(_leftHand.HasItem(name))
			_leftHand.UseItem();
	}

	public bool HasAnyItem(){
		return _rightHand._item!=null || _leftHand._item!=null;
	}

	public bool HasFreeHand(){
		if(_rightHand._item==null || _leftHand._item==null)
			return true;
		return false;
	}

	public void ReturnAllItems(){
		//in order for this to work, we need to know where items are to be returned to
		//check if hands are holding anything
		//if so return items to holders
		//loop through all itemStands
		//	returnItem x2 (this does nothing if player does not have item
		_stands = FindObjectsOfType<ItemStand>();
		foreach(ItemStand stand in _stands){
			stand.ReturnItem();
			stand.ReturnItem();
		}
	}

	public void ReturnLeftHand(){
		_stands = FindObjectsOfType<ItemStand>();
		foreach(ItemStand stand in _stands){
			stand.ReturnItemLeft();
		}
	}
	public void ReturnRightHand(){
		_stands = FindObjectsOfType<ItemStand>();
		foreach(ItemStand stand in _stands){
			stand.ReturnItemRight();
		}
	}
}
