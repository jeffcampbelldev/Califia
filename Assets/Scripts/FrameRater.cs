using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FrameRater : MonoBehaviour
{
	Text _t;
	// Start is called before the first frame update
	void Start()
	{
		_t = GetComponent<Text>();

	}

	// Update is called once per frame
	void Update()
	{
		float frameRate = 1/Time.deltaTime;
		_t.text=frameRate.ToString("fps:\n#.#");
	}
}
