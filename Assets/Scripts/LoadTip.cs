using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class LoadTip : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
		string tipPath="";
		MyMQTT [] qts = FindObjectsOfType<MyMQTT>();
		foreach(MyMQTT qt in qts){
			if(qt.gameObject.tag=="GameController")
				tipPath=qt.GetWorkPath("tips.txt");
		}
		string [] tips = File.ReadAllLines(tipPath);
		GetComponent<Text>().text="Tip: "+tips[Random.Range(0,tips.Length)];
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
