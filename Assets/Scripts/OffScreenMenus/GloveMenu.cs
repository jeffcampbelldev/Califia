using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class GloveMenu : MonoBehaviour
{
    [System.Serializable]
    public class GloveData{
        public string[] Name;
        public string[] Texture;
    }

    [HideInInspector]
    public GloveData _gloveData;

    public GameObject gloveModel;
    private int currGlove = 0;

    // Start is called before the first frame update
    void Start(){
        //read glove data from json
        string path = Application.streamingAssetsPath + "/Gloves.json";
        string content = File.ReadAllText(path);
        _gloveData = JsonUtility.FromJson<GloveData>(content);
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void NextGlove()
    {
        if (currGlove < _gloveData.Name.Length - 1)
            currGlove++;
        Debug.Log(currGlove);
    }

    public void PrevGlove()
    {
        if (currGlove > 0)
            currGlove--;
        Debug.Log(currGlove);
    }

}
