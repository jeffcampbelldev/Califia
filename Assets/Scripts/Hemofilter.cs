using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hemofilter : MonoBehaviour
{
	LiquidHelper _liquid;
	float _capacity = 2500;
	bool _overflow = false;
	bool _caniDisconnected = false;
	float _flowSpeed = 2f;
	public MeshRenderer _overflowMesh;
	Material _flowMat;
	public ParticleSystem _puddle;
	Transform _hemoEnd;
	Transform _caniEnd;
	Vector3 _hemoDefault;
	Vector3 _caniDefault;
	MyMQTT _mqtt;
	public ClickDetection _hemoClick;
	int _connected;

	// Start is called before the first frame update
	void Start()
	{
		_connected = 3;
		_liquid = GetComponentInChildren<LiquidHelper>();
		if (_overflowMesh != null)
			_flowMat = _overflowMesh.material;
		_overflowMesh.enabled = false;
		_hemoEnd = transform.Find("HemoDrain");
		_caniEnd = transform.Find("hemo_can").Find("HemoCatch");
		_hemoDefault = _hemoEnd.position;
		_caniDefault = _caniEnd.position;
		_hemoClick.enabled = false;
		MyMQTT[] qts = FindObjectsOfType<MyMQTT>();
		foreach (MyMQTT qt in qts)
		{
			if (qt.gameObject.tag == "GameController")
				_mqtt = qt;
		}
	}

	// Update is called once per frame
	void Update()
	{
		if ((_overflow && _flowMat != null) || (_caniDisconnected && _flowMat != null))
		{
			_flowMat.mainTextureOffset += Vector2.up * Time.deltaTime * _flowSpeed;
			if (_flowMat.mainTextureOffset.y > 1000)
				_flowMat.mainTextureOffset = Vector2.zero;
		}
	}

	public void Update(float v, string c)
	{
		Color color;
		string colHex = "#" + c;
		if (!ColorUtility.TryParseHtmlString(colHex, out color))
			color = new Color(1, 0, 1, 1);
		_liquid.SetColor(color);
		if (_flowMat != null)
			_flowMat.SetColor("_Color", color);
		if (_connected == 3)
			_liquid._targetHeight += v / _capacity;

		if (_caniDisconnected)
		{
			ParticleSystem.MainModule main = _puddle.main;
			main.startColor = color;
			main.loop = false;
			if(!_puddle.isPlaying && !_puddle.isEmitting)
				main.duration = v / 100 * 5; //5s of spillage for every 100mL of volume
			_puddle.Play();
			_overflowMesh.enabled = true;
			StartCoroutine(DisableOverflowMesh(v / 100 * 5));
		}
		else if (_liquid._targetHeight > 1f)
		{
			_liquid._targetHeight = 1f;
			if (!_overflow)
			{
				ParticleSystem.MainModule main = _puddle.main;
				main.loop = true;
				main.duration = 5f;
				main.startColor = color;
				HemoSpill(true);
			}
		}
	}

	public void HemoSpill(bool on)
	{
		if (on)
		{
			_puddle.Play();
			_overflow = true;
			_overflowMesh.enabled = true;
		}
		else
		{
			_puddle.Stop();
			_overflowMesh.enabled = false;
		}

	}

	public void DrainCanister()
	{
		_overflow = false;
		HemoSpill(false);
		_liquid._targetHeight = 0f;
	}

	public void Connect(int port)
	{
		StartCoroutine(ConnectR(port));
	}

	public void ManualConnectOutflow()
	{
		ConnectOutflow();
	}

	public void ConnectOutflow()
	{
		_hemoClick.enabled = false;
		if (_hemoEnd.position != _hemoDefault)
		{
			_mqtt.ReconnectHemo(0);
			StartCoroutine(ConnectR(0));
		}
		if (_caniEnd.position != _caniDefault)
		{
			_mqtt.ReconnectHemo(1);
			StartCoroutine(ConnectR(1));
		}
	}

	public void Disconnect(int port)
	{
		StartCoroutine(DisconnectR(port));
	}

	IEnumerator ConnectR(int port)
	{
		if (port == 0)
		{
			//connect hemofilter port (top)
			_connected += 1;
			Vector3 startPos = _hemoEnd.position;
			float timer = 0;
			while (timer < 1f)
			{
				timer += Time.deltaTime;
				_hemoEnd.position = Vector3.Lerp(startPos, _hemoDefault, timer);
				yield return null;
			}
			_hemoEnd.position = _hemoDefault;
		}
		else if (port == 1)
		{
			//connect canister port (bottom)
			HemoSpill(false);
			_connected += 2;
			Vector3 startPos = _caniEnd.position;
			float timer = 0;
			while (timer < 1f)
			{
				timer += Time.deltaTime;
				_caniEnd.position = Vector3.Lerp(startPos, _caniDefault, timer);
				yield return null;
			}
			_caniEnd.position = _caniDefault;
			_caniDisconnected = false;
		}
	}

	IEnumerator DisconnectR(int port)
	{
		if (port == 0)
		{
			//disconnect hemofilter port (top)
			_connected -= 1;
			_hemoEnd.position = _hemoDefault;
			Vector3 outPos = _hemoEnd.position + Vector3.down * .02f;
			float timer = 0;
			while (timer < 1f)
			{
				timer += Time.deltaTime;
				_hemoEnd.position = Vector3.Lerp(_hemoDefault, outPos, timer);
				yield return null;
			}
			_hemoEnd.position = outPos;
		}
		else if (port == 1)
		{
			//disconnect canister port (bottom)
			_connected -= 2;
			Vector3 outPos = _caniEnd.position + Vector3.up * .02f;
			float timer = 0;
			while (timer < 1f)
			{
				timer += Time.deltaTime;
				_caniEnd.position = Vector3.Lerp(_caniDefault, outPos, timer);
				yield return null;
			}
			_caniEnd.position = outPos;
			_caniDisconnected = true;
		}
		_hemoClick.enabled = true;
	}

	IEnumerator DisableOverflowMesh(float time)
	{
		yield return new WaitForSeconds(time);
		_overflowMesh.enabled = false;
	}

}
