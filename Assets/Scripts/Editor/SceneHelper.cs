using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class SceneHelper : EditorWindow
{
	[MenuItem("Scene/Splash Scene")]
	static void LoadSplash(){
		EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
		EditorSceneManager.OpenScene("Assets/Scenes/CustomSplash.unity");
	}
	[MenuItem("Scene/Opening Scene")]
	static void LoadOpening(){
		EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
		EditorSceneManager.OpenScene("Assets/Scenes/OpeningScene.unity");
	}
	[MenuItem("Scene/ICU Scene")]
	static void LoadIcu(){
		EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
		EditorSceneManager.OpenScene("Assets/Scenes/ICU.unity");
	}
	[MenuItem("Scene/OR Scene")]
	static void LoadOr(){
		EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
		EditorSceneManager.OpenScene("Assets/Scenes/OR.unity");
	}

	[MenuItem("Scene/AR Scene")]
	static void LoadAr(){
		EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
		EditorSceneManager.OpenScene("Assets/Scenes/AR.unity");
	}
}
