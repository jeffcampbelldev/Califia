using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class EquipmentManager : MonoBehaviour
{
	public GameObject[] _hlm;
	List<string> _hlms = new List<string>();
    // Start is called before the first frame update
    void Start()
    {
		string configPath = transform.GetComponent<RoomConfig>()._catalogPath;
		string ini = File.ReadAllText(configPath);
		string[] hlmRaw = IniHelper.GetSection(ini,"HLM");
		foreach(string v in hlmRaw){
			string [] parts = v.Split('=');
			string name = parts[1].Trim();
			_hlms.Add(name);
		}
    }

    // Update is called once per frame
    void Update()
    {
        
    }

	public void SetHLM(int index){
		string nm = _hlms[index];
		Debug.Log("hlm name: "+nm);
		for(int i=0; i<_hlm.Length; i++){
			_hlm[i].SetActive(_hlm[i].name==nm);
		}
	}
}
