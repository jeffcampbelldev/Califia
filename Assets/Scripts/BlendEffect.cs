using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlendEffect : MonoBehaviour
{
	public Material _blendMat;
	public static float _blinkDur=0.4f;
    // Start is called before the first frame update
    void Start()
    {
		if(gameObject.tag=="MainCamera")
			Unblink();
    }

	public void Blink(){
		StartCoroutine(BlinkR());
	}

	public void Unblink(){
		StartCoroutine(UnblinkR());
	}

	IEnumerator UnblinkR(){
		float timer=0;
		_blendMat.SetFloat("_BlendAmount",0);
		float dur = _blinkDur;
		while(timer<dur)
		{
			timer+=Time.deltaTime;
			_blendMat.SetFloat("_BlendAmount",timer/dur);
			yield return null;
		}
		_blendMat.SetFloat("_BlendAmount",1);
		yield return null;
	}

	IEnumerator BlinkR(){
		_blendMat.SetFloat("_BlendAmount",1);
		float timer=0;
		float dur = _blinkDur;
		while(timer<dur)
		{
			timer+=Time.deltaTime;
			_blendMat.SetFloat("_BlendAmount",1-timer/dur);
			yield return null;
		}
		_blendMat.SetFloat("_BlendAmount",0);
		yield return null;
		//enabled=false;
	}

	void OnRenderImage(RenderTexture src, RenderTexture dst){
		Graphics.Blit(src, dst, _blendMat);
	}
}
