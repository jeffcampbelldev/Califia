using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class LineTester : MonoBehaviour
{
	string _traceFileTesting = "EKG";
	List<Vector3> _pos = new List<Vector3>();
	public Vector2 _scaling;
	// Start is called before the first frame update
	void Start()
	{
		Redraw();
	}

	[ContextMenu("F")]
	public void Redraw(){
		string[] data = File.ReadAllLines(Application.streamingAssetsPath+"/"+_traceFileTesting+".txt");
		float[] fData = new float[data.Length];
		_pos.Clear();
		for(int i=0; i<data.Length; i++){
			fData[i]=float.Parse(data[i]);
			_pos.Add(new Vector3(i*_scaling.x,fData[i]*_scaling.y,0));
		}
	}

	// Update is called once per frame
	void Update()
	{
		Redraw();

	}

	void OnDrawGizmos(){
		Gizmos.color=Color.green;
		foreach(Vector3 p in _pos){
			Gizmos.DrawSphere(p,.75f);
		}
	}
}
