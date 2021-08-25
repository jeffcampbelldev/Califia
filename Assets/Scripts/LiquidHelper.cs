using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LiquidHelper : MonoBehaviour
{
	Transform _cap;
	public int _matIndex;
	Material _mat;
	Material _capMat;
	public Vector2 _heightBounds;
	private float _normHeight=0;
	[HideInInspector]
	public float _targetHeight;
	public float _lerpRate;
	[HideInInspector]
	public RoomConfig.IVBag _targetBag;
	[HideInInspector]
	public float _capacity;
	public AnimationCurve _curve;
	[HideInInspector]
	public Color _color;
	private bool _init;
    // Start is called before the first frame update
    void Start()
    {
		Initialize();
    }

	public void Initialize(){
		if(_init)
			return;
		if(_curve.keys.Length==0)
			_curve = AnimationCurve.Linear(0,0,1,1);
		_cap = transform.GetChild(0);
		_mat=GetComponent<MeshRenderer>().materials[_matIndex];
		if(_cap.GetComponent<MeshRenderer>()!=null)
			_capMat=_cap.GetComponent<MeshRenderer>().material;
		if(_targetBag.fluid_name!="")
			SetBag(_targetBag);
		_targetHeight=_normHeight;
		SetHeight();
		_init = true;
	}

    // Update is called once per frame
    void Update()
    {
		if(Mathf.Abs(_targetHeight-_normHeight)>.001f){
			_normHeight = Mathf.Lerp(_normHeight,_targetHeight,_lerpRate*Time.deltaTime);
			SetHeight();
		}
    }

	public void SetColor(Color c){
		_color=c;
		if(_mat == null)
			_mat = GetComponent<MeshRenderer>().materials[_matIndex];
		_mat.SetColor("_Color",c);
		if(_capMat!=null)
			_capMat.SetColor("_Color",c);
	}
	public void SetHeight(){
		Vector3 oldPos = _cap.position;
		oldPos.y = Mathf.Lerp(_heightBounds.x,_heightBounds.y,_curve.Evaluate(_normHeight));
		_cap.position=oldPos;
		//adjust side clipping
		_mat.SetFloat("_Level",oldPos.y);
	}
	
	void OnDestroy(){
		if(_mat!=null)
			_mat.SetFloat("_Level",_heightBounds.x);
	}

	//used for iv bags but not other liquids oops
	public void SetBag(RoomConfig.IVBag bag){
		Color color;
		string colHex = "#"+bag.fluid_color;
		if(!ColorUtility.TryParseHtmlString(colHex, out color))
			color = new Color(1,0,1,1);
		SetColor(color);
		_targetHeight=1;
		_normHeight=1;
		SetHeight();
		transform.GetChild(1).GetChild(0).GetComponent<Text>().text=bag.fluid_name;
		transform.GetChild(1).GetChild(1).GetComponent<Text>().text=bag.capacity+" cc";
		transform.GetChild(1).GetChild(2).GetComponent<Text>().text=bag.target_infusion_rate+" cc/kg/h";
		_targetBag=bag;
		_capacity=bag.capacity;
	}

	public void Infuse(float amount){
		_capacity-=amount;
		if(_targetBag.capacity<=0)
		{
			Debug.Log("Error, trying to infuse empty bag");
			return;
		}
		_targetHeight=_capacity/_targetBag.capacity;
	}
}
