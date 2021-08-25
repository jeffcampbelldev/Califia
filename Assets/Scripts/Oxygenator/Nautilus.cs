using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Nautilus : MonoBehaviour
{
	public Image _startButtonRing;
	public GameObject _startScreen;
	public GameObject _startButton;
	float _ringFill;
	bool _startButtonPressed;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
		if(_startButtonPressed){
			_ringFill+=Time.deltaTime*0.333f;
			_startButtonRing.fillAmount=_ringFill;
			if(_ringFill>=1f){
				Activate();
			}
		}
    }

	public void StartButtonPressed(){
		_startButtonPressed=true;
	}
	public void StartButtonReleased(){
		_startButtonPressed=false;
		_ringFill=0;
		_startButtonRing.fillAmount=0;
	}

	public void Activate(){
		_startButtonPressed=false;
		_startScreen.SetActive(false);
		_startButton.SetActive(false);
	}

	public void HoldingPlay(){
	}
}
