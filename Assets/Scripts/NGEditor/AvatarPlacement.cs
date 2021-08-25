//AvatarPlacement.cs
//
//Description: to-do describe
//

//

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class AvatarPlacement : NGInspector
{
	public GameObject _inspector;
	Transform _activeWidget;
	public Transform _widgetPrefab;
	public LayerMask _floor;
	public LayerMask _widgets;
	Transform _mainCam;
	TestCam _cam;
	//what the heck is this
	List<Transform> _saved = new List<Transform>();
	public delegate void EventHandler();
	public event EventHandler OnPlacementChanged;
	public InputField _name;
	BoxCollider _blocker;
	Transform _curSelected;
	public Material _widgBlue;
	public Material _widgGreen;
	public GameObject _error;
    // Start is called before the first frame update
    void Start()
    {
		_inspector.SetActive(false);
		_activeWidget = Instantiate(_widgetPrefab,Vector3.down*5f,Quaternion.identity,transform);
		_activeWidget.GetComponent<MeshRenderer>().material=_widgGreen;
		_cam = FindObjectOfType<TestCam>();
		_mainCam = _cam.transform;
		_blocker=_cam.transform.Find("Blocker").GetComponent<BoxCollider>();
    }

    // Update is called once per frame
    void Update()
    {
		if(_inspector.activeSelf){
			//cast active widget
			if(_curSelected==null)
			{
				RaycastHit hit;
				if(Physics.Raycast(_mainCam.position,_mainCam.forward,out hit,10f,_floor))
				{
					_activeWidget.position=hit.point;
				}
			}

			//check for click on existing
			if(Input.GetMouseButtonDown(0)){
				RaycastHit hit2;
				Vector3 ray = (Vector3)Input.mousePosition;
				if(Physics.Raycast(_mainCam.position,Camera.main.ScreenPointToRay(ray).direction,out hit2,10f,_widgets))
				{
					bool validClick=false;
					//skip 0 because it is NOT TO BE CHANGED
					for(int i=1; i<_saved.Count; i++){
						if(_saved[i].name==hit2.transform.name)
							validClick=true;
					}
					if(validClick)
						EditOption(hit2.transform);
				}
			}
		}
    }

	public override void ShowInspector(bool show){
		if(show)
			AddPlacement();
		else
			StopPlacement();
	}

	public void AddPlacement(){
		_inspector.SetActive(true);
		//activate inspector window
		//editing = true
		//enable all widgets
		foreach(Transform t in _saved){
			t.gameObject.SetActive(true);
		}
		_blocker.enabled=true;
		_error.SetActive(false);
	}

	public void StopPlacement(){
		_inspector.SetActive(false);
		//disable all widgets
		foreach(Transform t in _saved){
			t.gameObject.SetActive(false);
		}
		//disable active widget
		if(_activeWidget!=null)
			_activeWidget.position = Vector3.down*5f;
		if(_blocker!=null)
			_blocker.enabled=false;
	}

	public void StartEdit(){
		_cam.enabled=false;
	}

	public void EndEdit(){
		_cam.enabled=true;
	}

	public void Cancel(){
		if(_curSelected==null)
			StopPlacement();
		else{
			_curSelected.GetComponent<MeshRenderer>().material=_widgBlue;
			_curSelected=null;
		}
	}
	public void Save(){
		if(_activeWidget.position.y>-2f||_curSelected!=null)
		{
			//check for dupes
			foreach(Transform t in _saved){
				if(t.name==_name.text)
				{
					//set error message
					_error.SetActive(true);
					return;
				}
			}
			_error.SetActive(false);
			//edit
			if(_curSelected!=null){
				_curSelected.name=_name.text;
				_curSelected.GetComponent<MeshRenderer>().material=_widgBlue;
				_curSelected=null;
			}
			//new
			else
			{
				Transform spot=Instantiate(_widgetPrefab,_activeWidget.position,Quaternion.identity,transform);
				spot.name=_name.text;
				_saved.Add(spot);
				//StopPlacement();
			}
			OnPlacementChanged.Invoke();
		}
		else{
			Debug.Log("Could not save widget, position out of bounds");
		}
	}

	public string SerializePlacements(){
		string ser="";
		for(int i=0; i<_saved.Count; i++){
			Transform spot = _saved[i];
			ser+=i+" = "+spot.name+","+spot.position.x+","+spot.position.y+","+spot.position.z;
			ser+=System.Environment.NewLine;
		}
		ser+=System.Environment.NewLine;
		return ser;
	}

	public void LoadSpot(string name, float x, float y, float z){
		Transform spot=Instantiate(_widgetPrefab,
				new Vector3(x,y,z), Quaternion.identity,transform);
		spot.name=name;
		spot.gameObject.SetActive(false);
		_saved.Add(spot);
	}

	public void EditOption(Transform s){
		if(_curSelected!=null)
			_curSelected.GetComponent<MeshRenderer>().material=_widgBlue;
		Debug.Log("editing "+s.name+"?");
		_name.text=s.name;
		EndEdit();
		_activeWidget.position=Vector3.down*5f;
		_curSelected=s;
		//set color
		s.GetComponent<MeshRenderer>().material=_widgGreen;
	}

	public void DeleteOption(){
		if(_curSelected!=null){
			_saved.Remove(_curSelected);
			Destroy(_curSelected.gameObject);
			OnPlacementChanged.Invoke();
			_curSelected=null;
		}
	}

	public Vector3 GetPositionByIndex(int index){
		if(index>=0 && index<_saved.Count)
			return _saved[index].position;
		else 
			return Vector3.one*-1000f;
	}
}
