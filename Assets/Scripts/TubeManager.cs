//Tube Manager
//
//Description: Generates and destroys, and maybe connects tubing of various types
//
 and Tim Kwon

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TubeManager : MonoBehaviour
{
	public Transform [] _tubePrefabs;
	public enum TubeTypes {DEFAULT, OXYGEN, AIR, DRAINAGE, DELIVERY, WATER, MIXED_AIR};
	[Tooltip("How close to the target does pathfinding need to get")]
    public float _proximity = .075f;
    public float _forwardIncrement = .025f;
    public float _rotationIncrement = .075f;
	[Tooltip("How close the tube can get to objects")]
	public float _hitRadius = 0.05f;
    public List<Vector3> positionsGizmosRaw = new List<Vector3>();
	Vector3 _nextPositionGizmo=Vector3.zero;
	public AnimationCurve _curve;
	public LayerMask _layerMask;
	[Tooltip("How rotation increments are tested before skipping to next node")]
	public int _maxIter;
	[Tooltip("How many steps can pathfinding take before it fails")]
	public int _maxIterOuter;
	[Tooltip("Only keeps every nth point from the initial pathfinding pass")]
	public int _decimationNumber;

    // Start is called before the first frame update
    void Start()
    {
		foreach (Transform child in transform)
			Destroy(child.gameObject);
		positionsGizmosRaw.Clear();
    }

	Tube GenerateTubeSimple(Transform root, Transform target, TubeTypes t,float radius=-1){
		//instance the tube
		Transform tubeTransform;
		int index = (int)t;
		tubeTransform = Instantiate(_tubePrefabs[index],transform);
		Tube tube = tubeTransform.GetComponent<Tube>();
		//set radius
		if(radius!=-1)
			tube._radius=radius;
		//set endpoints
		tube._root=root;
		tube._target=target;
		return tube;
	}

	IEnumerator DebugPathR(Vector3 outwardPosR, Vector3 outwardPosT, float gravity){
		positionsGizmosRaw.Clear();

		Transform nextT = new GameObject("Next").transform;
		Transform curT = new GameObject("Cur").transform;
		curT.position = outwardPosR;
		curT.LookAt(outwardPosT);

		int iterInner = 0;
		int iterOuter = 0;

		//continue until reach target
		while (Vector3.Distance(curT.position, outwardPosT) > _proximity && iterOuter < _maxIterOuter) {
			nextT.position = curT.position + curT.forward * _forwardIncrement;
			_nextPositionGizmo = nextT.position;

			//check if next position will have a collision and rotate if necessary
			iterInner = 0;
			while (Physics.OverlapSphere(nextT.position, _hitRadius, _layerMask).Length > 0 && iterInner < _maxIter) {
				_nextPositionGizmo = nextT.position;
				nextT.RotateAround(curT.position, curT.up, _rotationIncrement);
				iterInner++;
				yield return null;
			}

			//advance current node
			curT.position = nextT.position;
			//determine angle between current heading and ideal heading
			Vector3 dir = (outwardPosT - curT.position).normalized;
			//rotate towards target
			float amount = Vector3.Angle(dir, curT.forward);
			curT.RotateAround(curT.position, -curT.up, amount);

			//save raw positions
			positionsGizmosRaw.Add(curT.position);
			iterOuter++;
			yield return null;
			
		}
		Debug.Log("success");
	}

	Tube GenerateTubeAndFindPath(Transform root, Transform target, TubeTypes t,string name="", float radius = -1, float gravity = 0,bool ibga=false){
		Transform tubeTransform;
		int index = (int)t;
		tubeTransform = Instantiate(_tubePrefabs[index], transform);
		Tube tube = tubeTransform.GetComponent<Tube>();
		if(name!="")
			tubeTransform.name=name;

		//temp
		bool debug=tubeTransform.name.Contains("pumpToOxy");
		//if(debug)
		//	Debug.Log("Water");

		if (radius != -1)
			tube._radius = radius;
		tube._ibga=ibga;
		tube._sizeLock=ibga;
		tube._root = root;
		tube._target = target;
		Vector3 outwardPosR = root.position + root.forward * .05f;
		Vector3 outwardPosT = target.position + target.forward * .05f;

#if UNITY_EDITOR
		//#todo find another way to debug - this don't work when real data is passed in
		//because the tube gets destroyed, and the ienumerator still runs
		//use debug hose for debugging
		//if(debug)
		//	StartCoroutine(DebugPathR(outwardPosR,outwardPosT,gravity));
#endif

		//return tube;

		//make target little forward along the blue axis

		//positionsGizmosRaw.Clear();

		List<Vector3> positions = new List<Vector3>();
		//create temp cur / next trackers
		GameObject next = new GameObject("Next");
		Transform nextT = next.transform;
		GameObject cur = new GameObject("Cur");
		Transform curT = cur.transform;

		//get initial position and direction
		curT.position = outwardPosR;
		curT.LookAt(outwardPosT);
		positions.Add(curT.position);

		int iterInner = 0;
		int iterOuter = 0;

		//continue until reach target
		while (Vector3.Distance(curT.position, outwardPosT) > _proximity && iterOuter < _maxIterOuter) {
			nextT.position = curT.position + curT.forward * _forwardIncrement;

			//check if next position will have a collision and rotate if necessary
			iterInner = 0;
			while (Physics.OverlapSphere(nextT.position, _hitRadius, _layerMask).Length > 0 && iterInner < _maxIter) {
				nextT.RotateAround(curT.position, curT.up, _rotationIncrement);
				iterInner++;
			}

			//advance current node
			curT.position = nextT.position;
			Vector3 dir = (outwardPosT - curT.position).normalized;
			curT.RotateAround(curT.position, -curT.up, Vector3.Angle(dir, curT.forward));

			//save raw positions
			positions.Add(curT.position);
			iterOuter++;
		}

		//once we get path, simplify list of points to smooth tube
		Vector3 prevPoint=Vector3.zero;
		for (int i = 0; i < positions.Count; i += _decimationNumber){
			if((positions[i]-prevPoint).sqrMagnitude<_forwardIncrement*_forwardIncrement)
				continue;
			GameObject midPoint = new GameObject("midpoint");
			midPoint.transform.position = positions[i];

			if (gravity!=0) {
				//get position/total length of tube and lower the position based on the anim curve
				float ratio = Vector3.Distance(root.position, midPoint.transform.position) / Vector3.Distance(root.position, target.position);
				midPoint.transform.position = new Vector3(midPoint.transform.position.x, 
												Mathf.Lerp(midPoint.transform.position.y,
													Mathf.Lerp(midPoint.transform.position.y, 0.11f, _curve.Evaluate(ratio)),
													gravity),
												midPoint.transform.position.z);
			}

			midPoint.transform.SetParent(tubeTransform);
			prevPoint=midPoint.transform.position;
		}

		GameObject endPoint = new GameObject("midpoint");
		endPoint.transform.position = outwardPosT;
		endPoint.transform.SetParent(tubeTransform);

		Destroy(next);
		Destroy(cur);

		return tube;
    }

    public void Hide(Tube t, bool show){
		t.gameObject.SetActive(show);
	}

	public Tube.TubeData Destroy(Tube t){
		Tube.TubeData td=new Tube.TubeData();
		if(t!=null)
		{
			td = t._data;
			Destroy(t.gameObject);
			return td;
		}
		td.segment=-1;
		return td;
	}

	public Tube Connect(Transform r, Transform t, TubeTypes type,string name="",float radius=-1, float gravity = 0,bool ibga = false){
		return GenerateTubeAndFindPath(r,t,type,name,radius, gravity,ibga);
	}

	void OnDrawGizmos() {
        // Draw a yellow sphere at every position
        foreach(Vector3 v in positionsGizmosRaw)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(v, .02f);
        }

		Gizmos.color=Color.red;
		Gizmos.DrawSphere(_nextPositionGizmo, 0.02f);
    }

	
	
	[ContextMenu("TestGenTube")]
	public void TestGenTube() {
		Transform a = GameObject.Find("portA").transform;
		Transform b = GameObject.Find("portB").transform;
		Debug.Log(a.name);
		Debug.Log(b.name);

		foreach (Transform child in transform)
			DestroyImmediate(child.gameObject);

		GenerateTubeAndFindPath(a, b, TubeTypes.WATER);
	}

	[ContextMenu("Clear gizmos")]
	public void ClearGizmos() {
		positionsGizmosRaw.Clear();
	}

}
