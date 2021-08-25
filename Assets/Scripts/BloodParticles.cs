using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BloodParticles : MonoBehaviour
{
    public Tube _tube;
    public int segID;
    //instead of referencing a tube rn, reference a segment id (int)
    //segment id: look through topics key 
    [HideInInspector]
    public ParticleSystem _ps;
    [HideInInspector]
    public ParticleSystem.MainModule _psMain;

    // Start is called before the first frame update
    void Start()
    {
        _ps = GetComponent<ParticleSystem>();
        _psMain = _ps.main;

        if (_tube)
           _psMain.startColor = _tube._col;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    /*public void SetLabelColor(string col)
    {
        Color color;
        string colHex = "#" + col;
        if (!ColorUtility.TryParseHtmlString(colHex, out color))
            return;
        color.a = _tubeMat.GetColor("_Color").a;
        _tubeMat.SetColor("_Color", color);
    }*/
}
