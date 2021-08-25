using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;

public class BuildHelper : EditorWindow
{
	static string version;
	static string rc;
	static string v;

	static void UpdateIniBuildInfo(){
		//read ini to get app version 
		INIParser ini = new INIParser();
		string iniPath = Application.streamingAssetsPath+"/EdgeCalifia3D.ini";
		ini.Open(iniPath);
		version= ini.ReadValue("Base","Version","null");

		//check changelog
		string clPath = Application.streamingAssetsPath+"/changeLog.txt";
		if(!File.Exists(clPath))
		{
			Debug.LogError("Changelog not found - aborting build");
			version="";
			return;
		}
		string [] clLines = File.ReadAllLines(clPath);
		rc = clLines[0];
		v = clLines[1];

		//write build to ini
		ini.WriteValue("Base","Build",rc+"-"+v);
		ini.Close();
	}


	[MenuItem("Build/Califia Windows")]
	static void DefaultBuild(){
		//get version id
		UpdateIniBuildInfo();
		if(version=="")
			return;

		//set platform
		SwitchToWindows();

		//setup build path for application
		string buildPath = Directory.GetParent(Application.dataPath).FullName.Replace('\\','/')+"/buildPath";
		if(!Directory.Exists(buildPath))
			Directory.CreateDirectory(buildPath);

		buildPath+="/Windows_"+version+rc+"-"+v;
		Debug.Log("build path: "+buildPath);
		if(!Directory.Exists(buildPath))
			Directory.CreateDirectory(buildPath);

		BuildPipeline.BuildPlayer(EditorBuildSettings.scenes,buildPath+"/"+Application.productName+".exe",BuildTarget.StandaloneWindows,BuildOptions.None); 
	}

	[MenuItem("Build/Califia-AR Android")]
	static void AndroidBuild(){
		//get version id
		UpdateIniBuildInfo();
		if(version=="")
			return;
		
		SwitchToAndroid();

		//setup build path for application
		string buildPath = Directory.GetParent(Application.dataPath).FullName.Replace('\\','/')+"/buildPath";
		if(!Directory.Exists(buildPath))
			Directory.CreateDirectory(buildPath);

		buildPath+="/Android_"+version+rc+"-"+v;
		Debug.Log("build path: "+buildPath);
		if(!Directory.Exists(buildPath))
			Directory.CreateDirectory(buildPath);

		BuildReport report = BuildPipeline.BuildPlayer(EditorBuildSettings.scenes,buildPath+"/build.apk",BuildTarget.Android,BuildOptions.None); 
        BuildSummary summary = report.summary;
        if (summary.result == BuildResult.Succeeded)
            Debug.Log("Build succeeded: " + summary.totalSize + " bytes");
        if (summary.result == BuildResult.Failed)
            Debug.Log("Build failed");
	}

	[MenuItem("Build/Switch Platform/Windows")]
	static void SwitchToWindows(){
		//include 4 main scenes for windows builds
		EditorBuildSettingsScene[] scenes = new EditorBuildSettingsScene[4];
		scenes[0]=new EditorBuildSettingsScene("Assets/Scenes/CustomSplash.unity", true);
		scenes[1]=new EditorBuildSettingsScene("Assets/Scenes/OpeningScene.unity", true);
		scenes[2]=new EditorBuildSettingsScene("Assets/Scenes/ICU.unity", true);
		scenes[3]=new EditorBuildSettingsScene("Assets/Scenes/OR.unity", true);

        // Set the Build Settings window Scene list
        EditorBuildSettings.scenes = scenes;

		EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows);

		//set to linear color space
		PlayerSettings.colorSpace=ColorSpace.Linear;

		//set default orientation
		PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
	}

	[MenuItem("Build/Switch Platform/Android")]
	static void SwitchToAndroid(){
		//include 2 scenes for android build
		EditorBuildSettingsScene[] scenes = new EditorBuildSettingsScene[1];
		//scenes[0]=new EditorBuildSettingsScene("Assets/Scenes/AndroidSplash.unity", true);
		//scenes[1]=new EditorBuildSettingsScene("Assets/Scenes/AndroidTest.unity", true);
		//scenes[0]=new EditorBuildSettingsScene("Assets/Scenes/ARcopy.unity", true);
		//scenes[0]=new EditorBuildSettingsScene("Assets/Scenes/MqttTest.unity", true);
		scenes[0]=new EditorBuildSettingsScene("Assets/Scenes/CalifiaAR.unity", true);

        // Set the Build Settings window Scene list
        EditorBuildSettings.scenes = scenes;
		
		EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android,BuildTarget.Android);

		PlayerSettings.colorSpace=ColorSpace.Gamma;
		
		//set default orientation
		PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
	}
}
