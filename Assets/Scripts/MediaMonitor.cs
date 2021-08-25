using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using UnityEngine.Networking;

public class MediaMonitor : MonoBehaviour
{
	public RawImage _display;
	Vector2 _maxDim;
	float _aspect;
	VideoPlayer _video;
	public Transform _arrow;
	public CanvasGroup _notif;
	public CanvasGroup _notFound;
	bool _newMedia=false;
	float _animAmp=.02f;
	int _playMode;
	int _lastMedia=0; //0=none, 1=image, 2=video
	int _maxAttempts=10;
	float _fileCheckDelay=1f;
	public int _monitorId;
	public string _monitorName;
	Button _playButt;
	Image _playButtImg;
	public Sprite _playSprite, _pauseSprite;
	public Color _powerOn, _powerOff;
	bool _powered=true;
	public CanvasGroup _screen;
	public MeshRenderer _powerButt;
	public Image _slider;
	public Transform _handle;
	float _handleMax;
	YoutubePlayer _youtube;
	bool _isYoutube;
    // Start is called before the first frame update
    void Start()
    {
		_maxDim=_display.GetComponent<RectTransform>().sizeDelta;
		_aspect = _maxDim.x/_maxDim.y;
		_video=GetComponentInChildren<VideoPlayer>();
		_playButt = GetComponentInChildren<Button>();
		_playButtImg = _playButt.GetComponent<Image>();
		_playButt.gameObject.SetActive(false);
		_screen.alpha=1;
		_handleMax = _handle.parent.GetComponent<RectTransform>().sizeDelta.x;
		_display.enabled=false;
		_youtube=GetComponentInChildren<YoutubePlayer>();
    }

	float frac;
    // Update is called once per frame
    void Update()
    {
		if(_newMedia){
			_arrow.localPosition = Vector3.up*Mathf.PingPong(Time.time,.5f)*_animAmp;
		}
		if(_lastMedia==2 && _video.isPlaying){
			frac = (float)(_video.time/_video.length);
			_slider.fillAmount=frac;
			_handle.localPosition = Vector3.right*frac*_handleMax;
		}
    }

	public void LoadImage(string path, int disp,int playMode){
		if(disp!=_monitorId)
			return;
		if(_video!=null)
			_video.Stop();
		StopAllCoroutines();
		StartCoroutine(LoadImageR(path));
		_playMode=playMode;
	}

	public void LoadVideo(string path, int playMode, float volume, bool isWeb,int disp,bool isYoutube=false){
		if(disp!=_monitorId)
			return;
		if(_video!=null)
			_video.Stop();
		StopAllCoroutines();
		_isYoutube=isYoutube;
		StartCoroutine(LoadVideoR(path,playMode,volume,isWeb,isYoutube));
	}

	public void FileNotFound(int disp=-1){
		if(disp==-1 || disp==_monitorId)
			_notFound.alpha=1f;
	}

	IEnumerator LoadImageR(string path){
		bool fileFound=false;
		int attempts=0;
		while(!fileFound && attempts<_maxAttempts)
		{
			using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(path))
			{
				yield return uwr.SendWebRequest();

				if (uwr.isNetworkError || uwr.isHttpError)
				{
					Debug.Log(uwr.error);
					attempts++;
					yield return new WaitForSeconds(_fileCheckDelay);
				}
				else
				{
					// Get downloaded asset bundle
					var texture = DownloadHandlerTexture.GetContent(uwr);
					_display.texture=texture;
					ScaleTexture(texture.width,texture.height);
					_lastMedia=1;
					ReceiveNotif();
					fileFound=true;
					if(_playMode==1)
						SilenceNotif();
				}
			}
		}
		if(!fileFound){
			FileNotFound();
		}
	}

	IEnumerator LoadVideoR(string path, int playMode, float volume,bool isWeb,bool isYoutube=false){
		bool fileFound=false;
		int attempts=0;
		while(!fileFound && attempts<_maxAttempts)
		{
			if(isYoutube){
				fileFound=true;
				_youtube.Play(path);
			}
			else if(isWeb){
				using (UnityWebRequest webRequest = UnityWebRequest.Head(path)){
					yield return webRequest.SendWebRequest();

					if(webRequest.isNetworkError || webRequest.isHttpError){
						Debug.Log(webRequest.error);
						attempts++;
						yield return new WaitForSeconds(_fileCheckDelay);
					}
					else{
						fileFound=true;
					}
				}
			}
			else{
				if(File.Exists(path)){
					fileFound=true;
				}
				else{
					attempts++;
					yield return new WaitForSeconds(_fileCheckDelay);
				}
			}
		}
		if(fileFound)
		{
			if(!isYoutube){
				_video.url=path;
				_video.Prepare();
			}
			else
				_youtube.Play(path);
			while(!_video.isPrepared){
				yield return null;
			}
			_video.SetDirectAudioVolume(0,volume);
			_display.texture=_video.targetTexture;
			ScaleTexture((int)_video.width,(int)_video.height);
			_lastMedia=2;
			_playMode=playMode;
			ReceiveNotif();
			if(_playMode==1)
				SilenceNotif();
		}
		else{
			FileNotFound();
		}
	}

	void ScaleTexture(int w, int h){
		Vector2 rawRes = new Vector2(w,h);
		float rawAspect = rawRes.x/rawRes.y;
		//source file is wider than target display
		if(rawAspect>=_aspect){
			_display.GetComponent<RectTransform>().sizeDelta = new Vector2(_maxDim.x,_maxDim.x/rawAspect);
		}
		//source file is taller than target display
		else{
			_display.GetComponent<RectTransform>().sizeDelta = new Vector2(_maxDim.y*rawAspect,_maxDim.y);
		}
	}

	void ReceiveNotif(){
		_video.loopPointReached -= ReplayOnce;
		_video.loopPointReached -= PauseOnLoop;
		_playButt.gameObject.SetActive(false);
		if(!_powered)
			Power();
		_display.color=Color.black;
		_display.enabled=false;
		_newMedia=true;
		_notif.alpha=1f;
		_notFound.alpha=0f;
	}

	public void SilenceNotif(){
		if(_newMedia==false)
		{
			if(_lastMedia==1)
			{
				//already loaded image clicked again
				float alpha = Mathf.RoundToInt(1-_notif.alpha);
				_notif.alpha=alpha;
				//darken or show image accordingly
				_display.color = (1-alpha)*Color.white;
				_display.enabled=1-alpha>0.5;
			}
			return;
		}
		_notif.alpha=0f;
		_newMedia=false;
		if(_lastMedia==2){
			_playButt.gameObject.SetActive(true);
			StopAllCoroutines();
			StartCoroutine(PlayLastVideo(_playMode));
		}
		else if(_lastMedia==1){
			_display.color=Color.white;
			_display.enabled=true;
		}
	}

	IEnumerator PlayLastVideo(int playMode){
		//prepare
		_video.time=0;
		_video.Prepare();
		while(!_video.isPrepared)
			yield return null;
		_display.color=Color.white;
		_display.enabled=true;
		_video.isLooping=true;
		_video.loopPointReached += PauseOnLoop;
		_video.Play();
		_playButtImg.sprite=_pauseSprite;
	}

	void ReplayOnce(VideoPlayer vp){
		//way may need to add some delay here
		vp.isLooping=false;
		vp.time=0;
		vp.Play();
		//prevent the thing from getting played again
		_video.loopPointReached -= ReplayOnce;
		_video.loopPointReached += PauseOnLoop;
	}

	void PauseOnLoop(VideoPlayer vp){
		vp.time=0;
		vp.Pause();
		_playButtImg.sprite=_playSprite;
	}

	public void PlayPause(){
		if(_video.isPlaying){
			_video.Pause();
			_youtube.Pause();
			_playButtImg.sprite=_playSprite;
		}
		else{
			_video.Play();
			_playButtImg.sprite=_pauseSprite;
		}
	}

	public void Power(){
		_screen.alpha = Mathf.RoundToInt(1f-_screen.alpha);
		_powered=_screen.alpha>0.5f;
		if(_powered)
			_powerButt.material.SetColor("_Color",_powerOn);
		else
		{
			_powerButt.material.SetColor("_Color",_powerOff);
			if(_video.isPlaying){
				_video.Pause();
				_playButtImg.sprite=_playSprite;
			}
		}
	}

	bool _wasPlaying;
	public void Scrub(DragDetection dd){
		frac = dd._horDrag;
		if(_video.isPlaying){
			_wasPlaying=true;
			_playButtImg.sprite=_playSprite;
			if(_isYoutube)
				_youtube.Pause();
			_video.Pause();
		}
		_slider.fillAmount=frac;
		_handle.localPosition = Vector3.right*frac*_handleMax;
		if(_isYoutube)
			_youtube.SkipToPercent(frac);
		else
			_video.time=frac*_video.length;
	}

	public void DoneScrub(){
		if(_wasPlaying)
		{
			_video.Play();
			_playButtImg.sprite=_pauseSprite;
		}
		_wasPlaying=false;
	}
	public string SerializeMonitor(){
		return _monitorId+" = "+_monitorName;
	}
}
