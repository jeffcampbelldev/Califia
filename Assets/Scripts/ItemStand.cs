//ItemStand.cs
//
//Description: Acts as link between hold-able items, and the inventory (player's hands)
//An ItemStand can hold one specific type of item, but may hold multiple instances of that item
//A good example is the clamp stand, which by default holds 6 clamps
//


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ItemStand : MonoBehaviour
{
	List<GameObject> _items = new List<GameObject>();
	Inventory _inventory;
	public Item _item;
	public int _numItems;
	int _count;
	public bool _limitedItems;
	public UnityEvent _onReturnItem;

	// Start is called before the first frame update
	void Start()
	{
		_count=_numItems;
		ClickDetection [] cds = GetComponentsInChildren<ClickDetection>();
		foreach(ClickDetection cd in cds){
			if(cd.transform!=transform)
			{
				_items.Add(cd.transform.gameObject);
			}
		}

		_inventory = FindObjectOfType<Inventory>();
	}

	public void TakeItem(){
		if(!_limitedItems || (_limitedItems&&_numItems>0)){
			if(_inventory.HasFreeHand()){
				_inventory.AddItem(_item);
				foreach(GameObject go in _items){
					if(go.activeSelf){
						//Debug.Log("disabling a thing?: "+go.name);
						go.SetActive(false);
						if(_limitedItems)
							_numItems--;
						return;
					}
				}
			}
		}
	}

	public void HideItem(){
		foreach(GameObject go in _items){
			if(go.activeSelf){
				go.SetActive(false);
				return;
			}
		}
	}
	public void ShowItem(){
		foreach(GameObject go in _items){
			if(!go.activeSelf){
				go.SetActive(true);
				return;
			}
		}
	}

	public void ShowAll(){
		foreach(GameObject go in _items){
			if(!go.activeSelf){
				go.SetActive(true);
			}
		}
	}

	public void ReturnItem(){
		if(_limitedItems)
			_numItems++;
		if(_inventory.HasItem(_item._name)){
			_inventory.UseItem(_item._name);
			foreach (GameObject go in _items)
			{
				if (!go.activeSelf)
				{
					_onReturnItem.Invoke();
					go.SetActive(true);
					return;
				}
			}
		}
	}

	public void ResetItems(){
		foreach (GameObject go in _items){
			if(!go.activeSelf)
			{
				go.SetActive(true);
			}
		}
	}

	public void SetNumItemsRemoved(int i){
		foreach(GameObject go in _items)
			go.SetActive(false);
		int count=0;
		int num = _items.Count-i;
		foreach (GameObject go in _items){
			if(count<num){
				count++;
				go.SetActive(true);
			}
		}
	}

	public void SetNumItems(int i){
		foreach(GameObject go in _items)
			go.SetActive(false);
		int count=0;
		foreach (GameObject go in _items){
			if(count<i){
				count++;
				go.SetActive(true);
			}
		}
	}

	public void ReturnItemLeft(){
		if(_inventory._leftHand.HasItem(_item._name)){
			_inventory._leftHand.UseItem();
			foreach(GameObject go in _items){
				if(!go.activeSelf){
					go.SetActive(true);
					_onReturnItem.Invoke();
					return;
				}
			}
		}
	}

	public void ReturnItemRight(){
		if(_inventory._rightHand.HasItem(_item._name)){
			_inventory._rightHand.UseItem();
			foreach(GameObject go in _items){
				if(!go.activeSelf){
					go.SetActive(true);
					_onReturnItem.Invoke();
					return;
				}
			}
		}
	}

}
