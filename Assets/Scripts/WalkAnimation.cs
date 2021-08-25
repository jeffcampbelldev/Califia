using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WalkAnimation : MonoBehaviour
{
	public float _walkSpeed;
	public Transform _animObj;
	public float _turnSpeed;
	Quaternion _targetRotation;
	public Animator _handsAnimLeft;
	public Animator _handsAnimRight;
	public Animator _camAnim;
	public CanvasGroup _cg;
    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
		_animObj.rotation=Quaternion.Slerp(_animObj.rotation,_targetRotation,_turnSpeed*Time.deltaTime);
    }

	[ContextMenu("Play animation")]
	public void PlayAnim(){
		StartCoroutine(PlayAnimR());
	}
	IEnumerator FadeR(){
		float timer=0f;
		_cg.alpha=1f;
		while(timer<1f){
			timer+=Time.deltaTime;
			_cg.alpha=1-timer;
			yield return null;
		}
		_cg.alpha=0f;

	}
	IEnumerator PlayAnimR(){
		float timer=0f;
		StartCoroutine(FadeR());
		yield return null;
		Vector3 startPos;
		Vector3 target; 
		float dist;
		float time;
		int childIndex=0;
		_handsAnimLeft.SetBool("walk",true);
		_handsAnimRight.SetBool("walk",true);
		//_camAnim.SetBool("walk",true);
		while(childIndex<transform.childCount-1){
			startPos=transform.GetChild(childIndex).position;
			childIndex++;
			target = transform.GetChild(childIndex).position;
			Quaternion cur = _animObj.rotation;
			_animObj.LookAt(target);
			_targetRotation=_animObj.rotation;
			_animObj.rotation=cur;
			dist=(target-startPos).magnitude;
			time=dist/_walkSpeed;
			timer=0;
			while(timer<time){
				timer+=Time.deltaTime;
				_animObj.position=Vector3.Lerp(startPos,target,timer/time);
				yield return null;
			}
			_animObj.position=target;
		}
		_handsAnimLeft.SetBool("walk",false);
		_handsAnimRight.SetBool("walk",false);
		_camAnim.SetBool("walk",false);
	}

	void OnDrawGizmos(){
		for(int i=0; i<transform.childCount; i++){
			if(i==0)
				Gizmos.color=Color.green;
			else
				Gizmos.color=Color.magenta;
			Gizmos.DrawSphere(transform.GetChild(i).position,.1f);
			if(i<transform.childCount-1)
				Gizmos.DrawLine(transform.GetChild(i).position,transform.GetChild(i+1).position);
		}
		Gizmos.DrawSphere(_animObj.position,.5f);
	}
}
