using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class Patient : MonoBehaviour
{
	Animator _anim;
	//public Animator _vent;
	public float _myHeight;
	public float _myGender;
	public float _myAge;
	public float _myWeight;
	Tube[] _tubes;
	BedManager _bed;
	Vector3 _femLeft;//fem left = 9875
	Vector3 _femRight;//fem right = 4676
	Vector3 _jug;//jug index = 2696
	public Transform [] _sites;
	public Transform [] _sensors;
	public Transform _cloth;
	public Transform _femCloth;
	public Transform _mouthPiece;
	[HideInInspector]
	public Mesh _mesh;
	[HideInInspector]
	public SkinnedMeshRenderer _smr;
	public int _testIndex=0;
	string _adultFile="adultHeightWeight.txt";
	string _childFile="childHeightWeight.txt";
	Blanket _blanket;
	CannulaMenu _cannulas;
	CircuitManager _circuit;
	// Start is called before the first frame update
	void Start()
	{
		_anim=GetComponent<Animator>();
		_tubes = FindObjectsOfType<Tube>();
		_bed = FindObjectOfType<BedManager>();
		_smr = GetComponent<SkinnedMeshRenderer>();
		_cannulas = FindObjectOfType<CannulaMenu>();
		_circuit = FindObjectOfType<CircuitManager>();
		//todo add blanket
		//clipboard, and blanket button
		_blanket = _bed.transform.GetComponentInChildren<Blanket>();
		_mesh = new Mesh();
		SetSensorsAndCannulas();
		//defaults
		UpdateVal("Patient/gender",0);
		UpdateVal("Patient/weight",75);
		UpdateVal("Patient/age",40);
		UpdateVal("Patient/height",175);
		SetFeetVisible(true);
	}

	public void UpdateVal(string topic, float val){
		switch(topic){
			case "Patient/gender":
				_myGender=val;
				break;
			case "Patient/age":
				//use min age slider for under 16
				if(val<=16){
					_smr.SetBlendShapeWeight(4,100*(1-Mathf.InverseLerp(0,16,val)));
					_smr.SetBlendShapeWeight(5,0);
				}
				//max age slider for over 16
				else{
					_smr.SetBlendShapeWeight(4,0);
					_smr.SetBlendShapeWeight(5,100*Mathf.InverseLerp(16,130,val));
				}
				_myAge=val;
				break;
			case "Patient/height":
				_myHeight=val;
				break;
			case "Patient/weight":
				_myWeight=val;
				break;
			default:
				break;
		}
		//check bed
		_bed.SetBed(_myAge<1f && _myHeight<60f? 1 : 0 );

		//check height and weight
		float avgWeight=0;
		float avgHeight=0;
		float ageMultiplier=1f;
		float pubertyMult=Mathf.Min(1,(_myAge/15f));
		if(_myAge<=12){//child
			float [] vals = GetValsNearKey(_myAge,_childFile);
			if(_myGender<0.5f){
				avgWeight=vals[0];
				avgHeight=vals[1];
			}
			else{
				avgWeight=vals[2];
				avgHeight=vals[3];
			}
			ageMultiplier=0.5f;
		}
		else{//adult
			//check adult file for height entry
			float [] vals = GetValsNearKey(_myHeight,_adultFile);
			if(vals.Length>0){
				if(_myGender<0.5)//male
				{
					avgWeight = (vals[2]+vals[3])*.5f;
					avgHeight = 175.4f;//average male height in us
				}
				else//female
				{
					avgWeight = (vals[0]+vals[1])*.5f;
					avgHeight = 162.56f;//average female height in us
				}
			}
		}
		//set weight
		if(_myWeight<avgWeight){
			_smr.SetBlendShapeWeight(0,100*
					(1-Mathf.InverseLerp(avgWeight*.5f,avgWeight,_myWeight))*ageMultiplier);
			_smr.SetBlendShapeWeight(1,0);
			_bed.SetBedWidth(1f);
		}
		else{
			_smr.SetBlendShapeWeight(0,0);
			float maxWeightSlide=100*
					Mathf.InverseLerp(avgWeight,avgWeight*2f,_myWeight)*ageMultiplier;
			_smr.SetBlendShapeWeight(1,maxWeightSlide);
			bool wideBed = maxWeightSlide>37f && ageMultiplier>0.5f;
			_bed.SetBedWidth(wideBed?1.4f : 1f);
		}
		//set height
		if(_myHeight<avgHeight){
			_smr.SetBlendShapeWeight(6,100*
					(1-Mathf.InverseLerp(avgHeight*0.8f,avgHeight,_myHeight))*ageMultiplier);
			_smr.SetBlendShapeWeight(7,0);
		}
		else{
			_smr.SetBlendShapeWeight(6,0);
			_smr.SetBlendShapeWeight(7,Mathf.Min(23f,
					50*
					(Mathf.InverseLerp(avgHeight,avgHeight*1.2f,_myHeight))*ageMultiplier));
		}
		//set gender
		if(ageMultiplier>0.5f){
			if(_myGender>0.5){
				_smr.SetBlendShapeWeight(2,0);
				_smr.SetBlendShapeWeight(3,100*_myGender*pubertyMult);
			}
			else{
				_smr.SetBlendShapeWeight(2,100*(1-_myGender)*pubertyMult);
				_smr.SetBlendShapeWeight(3,0);
			}
		}
		else{//gender neutral until 12
			_smr.SetBlendShapeWeight(2,0);
			_smr.SetBlendShapeWeight(3,0);
		}
		SetSensorsAndCannulas();
		_blanket.CheckBlanket();
	}

	void SetSensorsAndCannulas(){
		_smr.BakeMesh(_mesh);
		_mesh.RecalculateBounds();
		//_smr.sharedMesh.RecalculateBounds();
		if(_sites[0]!=null)//fem left - 14086 - 5408
			_sites[0].position = transform.TransformPoint(_mesh.vertices[5408]);
		if(_sites[3]!=null)//fem right - 6112 - 6102
			_sites[3].position = transform.TransformPoint(_mesh.vertices[6121]);
		if(_sites[7]!=null)//jugular - 
			_sites[7].position = transform.TransformPoint(_mesh.vertices[2696]);
		float childVal = _smr.GetBlendShapeWeight(4)*.01f;
		float girth = Mathf.Lerp(30,20,childVal);
		Vector3 scale = new Vector3(girth,girth,Mathf.Lerp(30f,10f,childVal));
		foreach(Transform s in _sites){
			if(s!=null)
				s.localScale=scale;
		}

		if(_sensors[0]!=null)//forehead L 823
		{
			_sensors[0].position = transform.TransformPoint(_mesh.vertices[832]);
			_sensors[0].position-=_sensors[0].up*0.1f;
			_sensors[0].position+=_sensors[0].forward*0.01f;
		}
		if(_sensors[1]!=null)//forehead R 1009
		{
			_sensors[1].position = transform.TransformPoint(_mesh.vertices[1009]);
			_sensors[1].position-=_sensors[1].up*0.1f;
			_sensors[1].position+=_sensors[1].forward*0.01f;
		}
		if(_sensors[2]!=null)//Knee L 5340
		{
			_sensors[2].position = transform.TransformPoint(_mesh.vertices[5340]);
			_sensors[2].position-=_sensors[2].up*0.1f;
			_sensors[2].position+=_sensors[2].forward*0.01f;
		}
		if(_sensors[3]!=null)//Knee R 6340
		{
			_sensors[3].position = transform.TransformPoint(_mesh.vertices[6340]);
			_sensors[3].position-=_sensors[3].up*0.1f;
			_sensors[3].position+=_sensors[3].forward*0.01f;
		}
		//modesty clothes
		if(_myAge>6f)
		{
			_cloth.position = transform.TransformPoint(_mesh.vertices[6226]);//6226
			_cloth.transform.gameObject.SetActive(true);
		}
		else
			_cloth.transform.gameObject.SetActive(false);

		if(_myGender>0.5f && _myAge>10f){
			//place female cloth at 3876
			_femCloth.gameObject.SetActive(true);
			_femCloth.position = transform.TransformPoint(_mesh.vertices[4480]);//4480
		}
		else
			_femCloth.gameObject.SetActive(false);
		_mouthPiece.position = transform.TransformPoint(_mesh.vertices[41]);
		_cannulas.ResetCannulaRoots();
		_circuit.RefreshCircuit();
	}

	float[] GetValsNearKey(float key,string fn){
		string[] lines = File.ReadAllLines(Application.streamingAssetsPath+"/"+fn);
		List<float> tmpList = new List<float>();
		float tmp=0;
		float minDst=1000;
		string closestFit="";
		foreach(string l in lines){
			if(l.Length>=0 && l[0]!=';'){
				string[] parts = l.Split('%');
				if(parts.Length>1){
					float.TryParse(parts[0],out tmp);
					if(Mathf.Abs(tmp-key)<minDst){
						minDst=Mathf.Abs(tmp-key);
						closestFit=l;
					}
				}
			}
		}
		if(minDst<1000){
			string[] parts = closestFit.Split('%');
			for(int i=1; i<parts.Length; i++){
				float.TryParse(parts[i],out tmp);
				tmpList.Add(tmp);
			}
		}
		return tmpList.ToArray();
	}

	public void SetFeetVisible(bool vis){
		if(_smr==null)
			return;
		Material m = _smr.material;
		Bounds b = _mesh.bounds;
		float height = b.extents.y*2;
		m.SetVector("_ZBounds", vis? new Vector4(height*1.5f,0,0,0) : new Vector4(height*0.5f,0,0,0));
	}

	void OnDrawGizmos(){
		if(_mesh!=null && _testIndex<_mesh.vertices.Length){
			Gizmos.color = Color.yellow;
			Gizmos.DrawWireSphere(transform.TransformPoint(_mesh.vertices[_testIndex]),0.02f);
		}
	}
}
