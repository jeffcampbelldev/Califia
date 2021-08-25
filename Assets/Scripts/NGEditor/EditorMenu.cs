using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EditorMenu : MonoBehaviour
{
	NGInspector [] _inspectors;
	CanvasGroup _cg;
    // Start is called before the first frame update
    void Start()
    {
		_inspectors = GetComponentsInChildren<NGInspector>();
		_cg = GetComponent<CanvasGroup>();
		SetMode(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

	public void SetMode(bool instructor){
		if(!instructor){
			foreach(NGInspector ngi in _inspectors){
				ngi.ShowInspector(false);
			}
		}
		_cg.alpha = instructor? 1f : 0f;
		_cg.interactable=instructor;
		_cg.blocksRaycasts=instructor;
	}
}
