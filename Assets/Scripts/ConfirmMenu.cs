using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ConfirmMenu : MonoBehaviour
{
	CanvasGroup _cg;
	public Button _cancel;
	public Button _confirm;
	public Text _prompt;
    // Start is called before the first frame update
    void Start()
    {
		_cg = GetComponent<CanvasGroup>();
		Activate(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

	public void ConfirmRequest(string msg){
		//activate canvas
		Activate(true);
		//set text
		_prompt.text=msg;
		//clear button listeners
		_cancel.onClick.RemoveAllListeners();
		_confirm.onClick.RemoveAllListeners();
		//add close menu to both
		_cancel.onClick.AddListener(delegate {CloseConfirmMenu();});
		_confirm.onClick.AddListener(delegate {CloseConfirmMenu();});
	}

	void CloseConfirmMenu(){
		Activate(false);
	}

	void Activate(bool act){
		_cg.alpha = act? 1: 0;
		_cg.interactable=act;
		_cg.blocksRaycasts=act;
	}
}
