using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CircuitMenu : MonoBehaviour
{
	public RawImage _preview;
	public Text _label;
	CircuitManager _circuitMan;

	[System.Serializable]
	public struct Circuit{
		public string _name;
		public Texture2D _icon;
	}
	public Circuit[] _circuits;
	int _curCircuit;
	int _tmpCircuit;
	public Button _confirm;
	EcmoCart _cart;
	public Text _info;
	public Text _details;
	public Transform _buttonContainer;
	public Text _tabType;
    // Start is called before the first frame update
    void Start()
    {
		//get circuit manager
		_circuitMan=FindObjectOfType<CircuitManager>();

		_curCircuit=1;
		SetSelectedCircuit(_curCircuit);
		//mark circuit as selected among circuit options
		for(int i=0; i<_buttonContainer.childCount; i++){
			Transform t = _buttonContainer.GetChild(i);
			t.GetChild(2).gameObject.SetActive(_curCircuit==i);
		}
		_preview.texture=_circuits[_curCircuit]._icon;
		//_label.text=_circuits[_curCircuit]._name;
		_circuitMan.SetCurrentCircuit(_curCircuit,true);

		//get ecmo cart
		_cart = FindObjectOfType<EcmoCart>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

	public void ApplyCircuit(){
		_circuitMan._mqtt.ForceCircuitType(_curCircuit);
		_circuitMan.SetCurrentCircuit(_curCircuit);
		_tabType.text=_circuitMan._ecmoData.CircuitTypes[_curCircuit].name;
		for(int i=0; i<_buttonContainer.childCount; i++){
			Transform t = _buttonContainer.GetChild(i);
			t.GetChild(2).gameObject.SetActive(_curCircuit==i);
		}
		transform.parent.parent.GetComponent<LearnerPanels>().ShowPanel(0);
	}

	public void ShowMenu(){
		//check for flow
		bool flowing = _cart.IsFlowing();
		_confirm.interactable=!flowing;
		//_info.text=flowing? "Cannot change circuit while flowing" : "";
		_info.gameObject.SetActive(flowing);
		SetSelectedCircuit(_curCircuit);
	}

	public void SetCircuitLabel(int c){
		_tabType.text=_circuitMan._ecmoData.CircuitTypes[c].name;
	}
	//this doesn't set circuit, but just the active menu item
	public void SetSelectedCircuit(int c){
		_curCircuit=c;
		_preview.texture=_circuits[_curCircuit]._icon;
		//_label.text=_circuits[_curCircuit]._name;
		for(int i=0; i<_buttonContainer.childCount; i++){
			Transform t = _buttonContainer.GetChild(i);
			t.GetComponent<Button>().interactable=c!=i;
			t.GetComponent<EventTrigger>().enabled=c!=i;
			t.GetChild(0).gameObject.SetActive(c!=i);
			t.GetChild(1).gameObject.SetActive(c==i);
			if(c==i){
				_details.text=_circuitMan.GetDetails(c);
			}
		}
		if(Time.timeSinceLevelLoad>1f)
			_buttonContainer.GetComponent<AudioSource>().Play();
	}
}
