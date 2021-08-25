using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Blanket : MonoBehaviour
{
	List<Vector3> _points;
	List<Vector3> _pPoints;
	List<Vector3> _nPoints;
	List<Vector3> _iPoints;
	Vector3 _min;
	Vector3 _max;
	public Patient _patient;
	public int _xPoints;
	public int _zPoints;
	Transform _blanket;
	MeshFilter _meshF;
	MeshRenderer _meshR;
	Mesh _mesh;
	float _toeOverhang = 0.05f;
	float _sideOverhang = 0.03f;
	float _headHeight = 0.75f;
	bool _blanketed;
	ClickDetection _cd;
	List<int> _patientVertexCache;

    // Start is called before the first frame update
    void Start()
    {
		_patientVertexCache = new List<int>();
		_points = new List<Vector3>();
		_pPoints = new List<Vector3>();
		_nPoints = new List<Vector3>();
		_iPoints = new List<Vector3>();
		_mesh = new Mesh();
		_blanket = transform.GetChild(0);
		_meshF = _blanket.GetComponent<MeshFilter>();
		_meshR = _blanket.GetComponent<MeshRenderer>();
		_blanketed=true;
		_cd = GetComponent<ClickDetection>();
		CheckBlanket();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

	public void ToggleBlanket(){
		if(!_blanketed)
		{
			StopAllCoroutines();
			StartCoroutine(PlaceBlanketR());
		}
		else
		{
			_meshR.enabled=false;
			_patient.SetFeetVisible(true);
		}
		_blanketed=!_blanketed;
	}

	public void Remove(){
		_blanketed=false;
		_meshR.enabled=false;
		_patient.SetFeetVisible(true);
	}

	public void CheckBlanket(){
		if(_blanketed){
			StopAllCoroutines();
			StartCoroutine(PlaceBlanketR());
		}
		_patient.SetFeetVisible(!_blanketed);
	}

	//todo - optimize this bad boy with a compute shader
	public IEnumerator PlaceBlanketR(){
		int nullCheck=0;
		while(_patient._mesh==null)
		{
			yield return null;
			nullCheck++;
			if(nullCheck>20)
				break;
		}
		if(_patient._mesh!=null){
			Vector3 max = _patient.transform.TransformPoint(_patient._mesh.bounds.min);
			Vector3 min = _patient.transform.TransformPoint(_patient._mesh.bounds.max);
			Vector3 center = _patient.transform.TransformPoint(_patient._mesh.bounds.center);
			float top = max.y;
			float minX = min.x;
			float maxX = max.x;
			float minZ = min.z;
			float maxZ = max.z;
			float bottom = Mathf.LerpUnclamped(min.y,top,-.2f);
			_min=new Vector3(Mathf.LerpUnclamped(minX,maxX,-_sideOverhang),top,Mathf.LerpUnclamped(minZ,maxZ,-_toeOverhang));
			_max=new Vector3(Mathf.LerpUnclamped(minX,maxX,1f+_sideOverhang),top,Mathf.Lerp(minZ,maxZ,_headHeight));
			float xCell = (_max.x-_min.x)/(float)(_xPoints-1);
			float zCell = (_max.z-_min.z)/(float)(_zPoints-1);
			int cols=0;
			int rows=0;
			_points.Clear();
			//generate flat grid above patient
			for(int z=0;z<_zPoints;z++){
				for(int x=0; x<_xPoints; x++){
					_points.Add(new Vector3(_min.x+x*xCell,top,_min.z+z*zCell));
					if(rows==0)
						cols++;
				}
				rows++;
			}
			_pPoints.Clear();
			_nPoints.Clear();
			_iPoints.Clear();
			float zFlip = Mathf.Sign(_max.z-_min.z);
			if(_patientVertexCache.Count==0){
				//access a subset of vertices from the patient mesh
				for(int i=2600; i<_patient._mesh.vertices.Length; i+=5){
					Vector3 pos = _patient.transform.TransformPoint(_patient._mesh.vertices[i]);
					Vector3 norm = _patient._mesh.normals[i];
					//not sure why but normals seem flipped / transposed or something
					if(zFlip*pos.z<zFlip*_max.z && norm.z<0)
					{
						bool tooClose=false;
						foreach(Vector3 v in _pPoints){
							if((v-pos).sqrMagnitude<0.001)
							{
								tooClose=true;
								break;
							}
						}
						if(!tooClose){
							_pPoints.Add(pos);
							_patientVertexCache.Add(i);
						}
					}
					if(i%100==0)
						yield return null;
				}
			}
			else{
				foreach(int i in _patientVertexCache){
					_pPoints.Add(_patient.transform.TransformPoint(_patient._mesh.vertices[i]));
				}
			}

			//determine blanket height based on patient mesh
			for(int b=0; b<_points.Count; b++){
				float minSqr=1000;
				Vector3 bl=_points[b];
				//if blanket is off the toes, clamp to bottom of mesh
				if(zFlip*bl.z<zFlip*min.z || zFlip*bl.x <=zFlip*min.x || zFlip*bl.x>=zFlip*max.x)
				{
					bl.y=bottom;
				}
				else
				{
					float y=0;
					Vector2 flat = new Vector2(bl.x,bl.z);
					foreach(Vector3 pp in _pPoints){
						Vector2 flat2 = new Vector2(pp.x,pp.z);
						float ms = (flat2-flat).sqrMagnitude;
						if(ms<minSqr){
							minSqr=ms;
							y=pp.y;
						}
					}
					if(minSqr<0.01f)
						bl.y=y;
					else
						bl.y=bottom;
				}
				_iPoints.Add(transform.InverseTransformPoint(Vector3.Lerp(_points[b],bl,1f)));
			}

			//smooth mesh
			for(int i=0; i<_iPoints.Count; i++){
				Vector3 minXn = Vector3.zero;
				Vector3 maxXn = Vector3.zero;
				Vector3 minZn = Vector3.zero;
				Vector3 maxZn = Vector3.zero;
				if(i%cols==0){//first column
					minXn=_iPoints[i];
					maxXn=_iPoints[i+1];
				}
				else if(i%cols==cols-1){//last column
					minXn=_iPoints[i-1];
					maxXn=_iPoints[i];
				}
				else{
					minXn=_iPoints[i-1];
					maxXn=_iPoints[i+1];
				}
				if(i/cols<1){//first row
					minZn=_iPoints[i];
					maxZn=_iPoints[i+cols];
				}
				else if(i/cols>=rows-1){//last row
					minZn=_iPoints[i-cols];
					maxZn=_iPoints[i];
				}
				else{
					minZn=_iPoints[i-cols];
					maxZn=_iPoints[i+cols];
				}
				Vector3 avg = maxZn+minZn+maxXn+minXn;
				avg*=0.25f;
				_nPoints.Add(Vector3.Lerp(_iPoints[i],avg,1f));
			}

			//second smoothing pass
			for(int i=0; i<_nPoints.Count; i++){
				Vector3 minXn = Vector3.zero;
				Vector3 maxXn = Vector3.zero;
				Vector3 minZn = Vector3.zero;
				Vector3 maxZn = Vector3.zero;
				if(i%cols==0){//first column
					minXn=_nPoints[i];
					maxXn=_nPoints[i+1];
				}
				else if(i%cols==cols-1){//last column
					minXn=_nPoints[i-1];
					maxXn=_nPoints[i];
				}
				else{
					minXn=_nPoints[i-1];
					maxXn=_nPoints[i+1];
				}
				if(i/cols<1){//first row
					minZn=_nPoints[i];
					maxZn=_nPoints[i+cols];
				}
				else if(i/cols>=rows-1){//last row
					minZn=_nPoints[i-cols];
					maxZn=_nPoints[i];
				}
				else{
					minZn=_nPoints[i-cols];
					maxZn=_nPoints[i+cols];
				}
				Vector3 avg = maxZn+minZn+maxXn+minXn;
				avg*=0.25f;
				_iPoints[i]=Vector3.Lerp(_iPoints[i],avg,0.75f);
			}

			//create mesh
			_mesh.vertices = _iPoints.ToArray();
			int[] tris = new int[(rows-1)*(cols-1)*6];
			Vector3[] norms = new Vector3[_mesh.vertices.Length];
			Vector2[] uvs = new Vector2[_mesh.vertices.Length];
			int triIndex=0;
			for(int i=0; i<_mesh.vertices.Length; i++){
				if((i+1)%cols!=0 && i/cols<rows-1){
					tris[triIndex]  =i;
					tris[triIndex+1]=i+cols;
					tris[triIndex+2]=i+1;
					tris[triIndex+3]=i+1;
					tris[triIndex+4]=i+cols;
					tris[triIndex+5]=i+cols+1;
					triIndex+=6;
				}
				Vector3 n = Vector3.up;
				Vector3 minXn = Vector3.zero;
				Vector3 maxXn = Vector3.zero;
				Vector3 minZn = Vector3.zero;
				Vector3 maxZn = Vector3.zero;
				if(i%cols==0){//first column
					minXn=_mesh.vertices[i];
					maxXn=_mesh.vertices[i+1];
				}
				else if(i%cols==cols-1){//last column
					minXn=_mesh.vertices[i-1];
					maxXn=_mesh.vertices[i];
				}
				else{
					minXn=_mesh.vertices[i-1];
					maxXn=_mesh.vertices[i+1];
				}
				if(i/cols<1){//first row
					minZn=_mesh.vertices[i];
					maxZn=_mesh.vertices[i+cols];
				}
				else if(i/cols>=rows-1){//last row
					minZn=_mesh.vertices[i-cols];
					maxZn=_mesh.vertices[i];
				}
				else{
					minZn=_mesh.vertices[i-cols];
					maxZn=_mesh.vertices[i+cols];
				}
				norms[i]=Vector3.Cross(maxZn-minZn,maxXn-minXn);
				uvs[i] = new Vector2((i%cols)/(float)(cols-1),(i/cols)/(float)(rows-1));
			}
			_mesh.normals=norms;
			_mesh.triangles=tris;
			_mesh.uv=uvs;
			_mesh.RecalculateBounds();
			_meshF.sharedMesh=_mesh;
			_patient.SetFeetVisible(false);
			_blanket.localPosition = Vector3.up*Mathf.Lerp(0.04f,0.1f,_patient._myAge/12f);
			_meshR.enabled=true;
		}
	}

	/*
	void OnDrawGizmos(){
		if(_points!=null){
			Gizmos.color = Color.blue;
			Gizmos.DrawSphere(_min,0.1f);
			Gizmos.color = Color.red;
			Gizmos.DrawSphere(_max,0.1f);
			Gizmos.color = Color.magenta;
			for(int i=0; i<_points.Count; i++){
				//Gizmos.DrawSphere(_points[i],(i+1)/10f);
				Gizmos.DrawSphere(_points[i],0.02f);
			}
			for(int i=0; i<_pPoints.Count; i++){
				//Gizmos.color=new Color(_nPoints[i].x,_nPoints[i].y,_nPoints[i].z);
				Gizmos.color=Color.green;
				Gizmos.DrawSphere(_pPoints[i],0.02f);
			}
			Gizmos.color=Color.cyan;
			for(int i=0; i<_iPoints.Count; i++){
				Gizmos.DrawSphere(transform.TransformPoint(_iPoints[i]),0.01f);
			}
		}
	}
	*/
}
