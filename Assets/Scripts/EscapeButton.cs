//EscapeButton.cs
//
//Description: Shows escape menu
//@todo
//Create a generic helper class that takes 2 params - class name + method name as strings
//that can find object by type, and call methods with no params
//

//

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EscapeButton : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        
    }

	public void ShowEscapeMenu(){
		FindObjectOfType<SceneSelector>().ShowEscapeMenu();
	}
}
