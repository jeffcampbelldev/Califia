using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class IVmenu : OffscreenMenu
{	
	int _module;
	Alaris _alaris;
	List<RoomConfig.IVBag> _bags;
	public Transform _bagPrefab;
	RoomConfig _conf;
	System.DateTime _origin;
	Action<RoomConfig.IVBag> _onSelected;

    // Start is called before the first frame update
    public override void Start()
    {
		base.Start();
		//configure children
		//get fluids
		_conf = FindObjectOfType<RoomConfig>();
		_origin = new System.DateTime(1904, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
		ReloadFluids(-1);
    }

	public void ReloadFluids(long sec){
		//3693235010.12819
		StartCoroutine(ReloadFluidsR(sec));
	}

	IEnumerator ReloadFluidsR(long sec){
		//wait for timestamps to match
		yield return null;
		if(sec==-1){
		}
		else{
			System.DateTime lwt = File.GetLastWriteTime(_conf._catalogPath);
			System.TimeSpan diff = lwt.ToUniversalTime()-_origin;
			long dInt=(long)diff.TotalSeconds;
			Debug.Log("diff: "+dInt);
			Debug.Log("stamp: "+sec);
			float dur=0;
			while(System.Math.Abs(dInt-sec)>5 && dur<5f){
				Debug.Log("diff: "+dInt);
				Debug.Log("stamp: "+sec);
				yield return new WaitForSeconds(.1f);
				dur+=0.1f;
				lwt = File.GetLastWriteTime(_conf._catalogPath);
				diff = lwt.ToUniversalTime()-_origin;
				dInt=(long)diff.TotalSeconds;
			}
			if(dur>=5f){
				Debug.Log("Failed syncing file: "+_conf._catalogPath);
			}
			else{
				Debug.Log("Files synced !");
			}
		}

		foreach(Transform t in _belt.transform)
			Destroy(t.gameObject);
		_bags = _conf.LoadFluids();
		foreach(RoomConfig.IVBag bag in _bags){
			Transform t = Instantiate(_bagPrefab,_belt.transform);
			t.GetComponent<LiquidHelper>()._targetBag=bag;
		}
		//set bounds etc/ based on number of children
		_belt.Configure();
	}

	public override void OpenMenu(int module){
		base.OpenMenu(module);
		_module=module;
	}

	public void OpenMenuFromAvatar(int module, Action<RoomConfig.IVBag> onSelected){
		_onSelected = onSelected;
		OpenMenu(module);
	}

	public void ConfirmSelection(){
		int selection = _belt.GetSelectedObject();
		Debug.Log($"Module {_module} connected to fluid index {_bags[selection].fluid_name}");
		if(_onSelected == null){
			//alaris set module fluid
			if(_alaris==null)
				_alaris = FindObjectOfType<Alaris>();
			_alaris.SetChannelFluid(_module,_bags[selection]);
		}
		else{
			_onSelected(_bags[selection]);
		}
		_onSelected = null;
		HideMenu();
	}

	public void SelectForAvatar(){
		int selection = _belt.GetSelectedObject();
		Debug.Log($"Module {_module} connected to fluid index {_bags[selection].fluid_name}");
		
	}
}
