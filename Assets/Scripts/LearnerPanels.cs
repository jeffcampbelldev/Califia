using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LearnerPanels : MonoBehaviour
{
	[System.Serializable]
	public class LearnerPanel{
		public RectTransform _rect;
		CanvasGroup _cg;
		Vector3 _showLocal;

		public void Init(LearnerPanels lp){
			_showLocal=_rect.localPosition;
			_cg=_rect.GetComponent<CanvasGroup>();
			CanvasGroup tab = _cg.transform.GetChild(0).GetComponent<CanvasGroup>();
			tab.alpha=1f;
			Hide(lp);
		}

		public void Hide(LearnerPanels lp){
			if(_cg==null)
				return;
			Vector3 p = _rect.localPosition;
			p.y=(-540-_rect.sizeDelta.y*.5f);
			_rect.localPosition=p;
			_cg.alpha=0f;
			_cg.interactable=false;
			_cg.blocksRaycasts=false;
			bool allhide=true;
			foreach(LearnerPanel l in lp._lp){
				if(l._cg!=null && l._cg.alpha>0){
					allhide=false;
				}
				if(l._rect.GetComponentInChildren<NavPanel>()!=null)
					l._rect.GetComponentInChildren<NavPanel>().HidePopups();

			}
			if(allhide)
				lp._blocker.enabled=false;
			lp._cam._inputEnabled=true;
		}

		public void Show(LearnerPanels lp){
			if(_cg.alpha==1f)
				Hide(lp);
			else{
				_rect.localPosition=_showLocal;
				_cg.alpha=1f;
				_cg.interactable=true;
				_cg.blocksRaycasts=true;
				lp._blocker.enabled=true;
				lp._cam._inputEnabled=false;
				//lp._blurBounds = new Vector4(0,0,1,_rect.sizeDelta.x/1920f);
				//lp._blurMat.SetVector("_Bounds", lp._blurBounds);
			}
		}

		public bool IsShowing(){
			return _cg.alpha>0.5f && _rect.localPosition==_showLocal;
		}
	}
	public LearnerPanel [] _lp;
	public GameObject _clickAway;
	public Material _blurMat;
	Vector4 _blurBounds;
	BoxCollider _blocker;
	TestCam _cam;
    // Start is called before the first frame update
    void Start()
    {
		_clickAway.SetActive(false);
		_blurBounds = new Vector4(0,0,0,0);
		_blurMat.SetVector("_Bounds", _blurBounds);
		_cam = FindObjectOfType<TestCam>();
		_blocker=_cam.transform.Find("Blocker").GetComponent<BoxCollider>();
		for(int i=0; i<_lp.Length; i++){
			_lp[i].Init(this);
		}
    }

    // Update is called once per frame
    void Update()
    {
        
    }

	public void ShowPanel(int pan){
		_lp[pan].Show(this);
		_clickAway.SetActive(true);
	}
	public void HidePanel(int pan){
		_lp[pan].Hide(this);
		//_clickAway.SetActive(true);
	}
	public void HideAllPanels(){
		for(int i=0; i<_lp.Length; i++){
			_lp[i].Hide(this);
		}
		_clickAway.SetActive(false);
		//_blurBounds = new Vector4(0,0,1,0);
		//_blurMat.SetVector("_Bounds", _blurBounds);
	}

	public bool IsAnyPanelActive(){
		for(int i=0; i<_lp.Length; i++){
			if(_lp[i].IsShowing())
				return true;
		}
		return false;
	}
}
