using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using System.Diagnostics;

public class ToolsHelper : EditorWindow
{
    //pt 2: if mymqtt is disconnected & server is localhost, then try launching mqtt server
    //make sure we do it only once 
    //look through mymqtt script
    /*MyMQTT [] qts = FindObjectsOfType<MyMQTT>();
    foreach(MyMQTT qt in qts){
        if(qt.gameObject.tag=="GameController")
            _mqtt=qt;
    }*/

    [MenuItem("Tools/Launch Mosquitto")]
    static void LaunchMqtt()
    {
        string iniPath = System.Environment.ExpandEnvironmentVariables("%localappdata%").Replace("\\", "/");
        iniPath += "/Cal3D/EdgeCalifia3D.ini";

        INIParser ini = new INIParser();
        ini.Open(iniPath);
        string server = ini.ReadValue("Base", "Server", "");
        ini.Close();

        //get path of mosquitto and launch
        string path = "c:/Program Files/mosquitto/mosquitto.exe";

        if (File.Exists(@path) && server == "localhost")
        {
            Process.Start(@path);
        }
        else if (!File.Exists(@path))
        {
            UnityEngine.Debug.LogError("Mosquitto not found");
        }
        else if (server != "localhost")
        {
            UnityEngine.Debug.LogError("Server not set to local host");
        }
    }


    [MenuItem("Tools/Run Test Probe")]
    static void RunTestProbe()
    {
        //use ini to get prefix
        string path = System.Environment.ExpandEnvironmentVariables("%localappdata%").Replace("\\", "/");
        path += "/Cal3D/EdgeCalifia3D.ini";

        INIParser ini = new INIParser();
        ini.Open(path);
        string prefix = ini.ReadValue("Base", "Prefix", "");
        ini.Close();
        if (prefix == "")
        {
            UnityEngine.Debug.Log("prefix not found");
        }

        //get folder of mqtt test probe and write and run batch file
        string parent = Directory.GetParent(Application.dataPath).FullName.Replace('\\','/');
        string mqttFolder = Directory.GetParent(parent).FullName.Replace('\\','/') + "/Mqtt.Net Test Probe";
        UnityEngine.Debug.Log(mqttFolder);

        string bat = "cd " + mqttFolder + "\ntester " + prefix;
        string batPath = mqttFolder + "/runTestProbe.bat";
        File.WriteAllText(batPath, bat);

        //run test probe process
        Process.Start(batPath);
    }


    [MenuItem("Tools/Find To-do")]
    static void FindToDo()
    {
        //command to find all "todo" in all scripts
		string path = Application.dataPath;
		string batPath = Application.dataPath+"/Scripts/Editor/todo.bat";
        //string commands = "cd "+path+"\nfindstr /s/n/c:todo *.cs";
        string commands = "cd "+path+"\nfindstr /s/n/c:OnApplication *.cs";
		File.WriteAllText(batPath,commands);
		UnityEngine.Debug.Log("commands: "+commands);

        //set up command prompt process
        ProcessStartInfo cmdInfo = new ProcessStartInfo(batPath);
        cmdInfo.RedirectStandardOutput = true;
        cmdInfo.UseShellExecute = false;
        cmdInfo.CreateNoWindow = true;

        //run process
        Process cmd = new System.Diagnostics.Process();
        cmd.StartInfo = cmdInfo;
        cmd.Start();

        //log todos
        string result = cmd.StandardOutput.ReadToEnd();
        UnityEngine.Debug.Log(result);
    }

}
