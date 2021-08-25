using UnityEngine;
public class SensorMenu : OffscreenMenu
{
	int _module;
	Invos _invos;
	//List<RoomConfig.IVBag> _bags;
	public Transform _tmpPrefab;

    // Start is called before the first frame update
    public override void Start()
    {
		base.Start();
		_invos = FindObjectOfType<Invos>();
    }

	public override void OpenMenu(int module){
		base.OpenMenu(module);
		_module=module;
	}

	public void ConfirmSelection(){
		int selection = _belt.GetSelectedObject();
		_invos.ConnectAmplifier(selection,_module);
		//hardcode to adult
		_mqtt.SetInvosSensors(0);
		HideMenu();
	}
}
