using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

public class Reservation : EditorWindow
{
	static string GetResFilePath() {
		string path = System.Environment.ExpandEnvironmentVariables("%userprofile%").Replace("\\", "/") + "/Dropbox/Cal3Ddropbox/reservations.txt";
		return path;
	}

	//returns true if action needs to be taken
	//false if no changes needed
	static bool CheckGitStatus(string filename, bool checkIn) {

		//compose batch script
		string path = Application.dataPath;
		string batPath = Application.dataPath + "/Scripts/Editor/status.bat";
		string commands="";
		if(checkIn){
			commands = "cd " + path + "\ncd ..\ngit status";
		}
		else{//checkout we need fetch - make sure we are not behind
			commands = "cd " + path + "\ncd ..\ngit fetch\ngit status -uno";
		}
		File.WriteAllText(batPath, commands);

		//configure batch process
		System.Diagnostics.ProcessStartInfo cmdInfo = new System.Diagnostics.ProcessStartInfo(batPath);
		cmdInfo.RedirectStandardOutput = true;
		cmdInfo.UseShellExecute = false;
		cmdInfo.CreateNoWindow = true;

		//run process
		System.Diagnostics.Process cmd = new System.Diagnostics.Process();
		cmd.StartInfo = cmdInfo;
		cmd.Start();

		//get results from git status
		string result = cmd.StandardOutput.ReadToEnd();

		//check for modified files
		string[] lines = result.Split('\n');
		foreach (string l in lines) {
			if(!checkIn){//checkout
				if (!(l.Contains("up to date") || l.Contains("is ahead of")) && l.Contains("'origin/master'"))	//might need to change 
					return true;
			//Debug.Log("<color=red>Your local branch is not up to date. Make sure to pull before committing.</color>");
			}
			else{//checkIn make sure 
				string trimmed = l.Trim();
				if (trimmed.Contains("modified")) {
					string p = trimmed.Split(new string[] { "modified:" }, System.StringSplitOptions.None)[1];
					if (filename == p.Trim())
						return true;
				}
			}
		}
		return false;
	}

	/*[MenuItem("Reservation/Test_Git")]
	static void TestGit()
    {
		CheckGitStatus("Assets/Scripts/Editor/Reservation.cs");
	}*/

	[MenuItem("Reservation/Return my files")]
	static void CheckIn(){
		List<string> outLines = new List<string>();
		List<string> errors = new List<string>();
		string [] lines = File.ReadAllLines(GetResFilePath());
		foreach(string l in lines){
			if (l == "")
				continue;
			if (l[0] == ';'){
				outLines.Add(l);
				continue;
			}
			string[] parts = l.Split(new string[] {"###"},System.StringSplitOptions.None);
			if(parts.Length!=3){
				outLines.Add(l);
				continue;
			}
            if (parts[1] == System.Environment.UserName){
				if(CheckGitStatus(parts[0],true)){
					errors.Add(parts[0]);
					//keep the file in the reservations file
					outLines.Add(l);
					continue;
				}
				continue;
			}
			else
				outLines.Add(l);
		}

		if (errors.Count > 0)
			ErrorPopup.Init(errors);

		File.WriteAllLines(GetResFilePath(),outLines.ToArray());
	}

	[MenuItem("Reservation/Return select files")]
	static void OpenCheckinPopup(){
		CheckinPopup.Init();
	}

	static string[] CheckForReservation(string s){
		string [] lines = File.ReadAllLines(GetResFilePath());
		foreach(string l in lines){
			if(l[0]==';')
				continue;
			string[] parts = l.Split(new string[] {"###"},System.StringSplitOptions.None);
			if(parts.Length!=3)
				continue;
			if(s==parts[0])
				return parts;
		}
		return null;
	}

	static void CheckoutFiles(List<string> paths){
		List<string> outLines = new List<string>();
		string [] lines = File.ReadAllLines(GetResFilePath());
		foreach(string l in lines)
			outLines.Add(l);
		foreach(string s in paths){
			string line = s+"###"+System.Environment.UserName+"###"+System.DateTime.Now;
			outLines.Add(line);
		}
		File.WriteAllLines(GetResFilePath(),outLines.ToArray());
	}

	static void CheckInSelect(List<string> fileNames, List<bool> toggles){
		List<string> outLines = new List<string>();
		List<string> errors = new List<string>();
		string[] lines = File.ReadAllLines(GetResFilePath());

		foreach (string l in lines){
			if (l == "")
				continue;
			if (l[0] == ';'){
				outLines.Add(l);
				continue;
			}
			string[] parts = l.Split(new string[] { "###" }, System.StringSplitOptions.None);
			if (parts.Length != 3){
				outLines.Add(l);
				continue;
			}

			if (CheckGitStatus(parts[0], true) && toggles[fileNames.IndexOf(parts[0])]) {
				errors.Add(parts[0]);
				outLines.Add(l);
			}
			else if (fileNames.IndexOf(parts[0]) != -1 && toggles[fileNames.IndexOf(parts[0])])
			{
				Debug.Log("Returned: " + parts[0]);
				continue;
			}
            else{
				outLines.Add(l);
			}
		}

		if (errors.Count > 0)
			ErrorPopup.Init(errors);

		File.WriteAllLines(GetResFilePath(), outLines.ToArray());
	}

	public class FileModificationWarning : UnityEditor.AssetModificationProcessor
	{
		static string[] OnWillSaveAssets(string[] paths)
		{
			List<string> validSaves = new List<string>();
			List<string> checkouts = new List<string>();
			List<string[]> lockouts = new List<string[]>();
			foreach(string s in paths)
			{
				string ext = Path.GetExtension(s);
				if(ext==".prefab" || ext==".unity")
				{
					string[] resInfo = CheckForReservation(s);
					if(resInfo!=null)
					{
						//check if file is checked out by current user
						if(resInfo[1]==System.Environment.UserName)
							validSaves.Add(s);
						else
							lockouts.Add(resInfo);//file is locked
					}
					else{
						//file can be checked out
						//check git status to see if we are up to date
						if(CheckGitStatus("",false))//don't care filename
							ErrorPopup.Init("Cannot check out files because your local repo is out of date. Pull from GitHub before checking out.");
						else
							checkouts.Add(s);
					}
				}
				else
					validSaves.Add(s);
			}
			if(checkouts.Count>0)
				CheckoutPopup.Init(checkouts);
			if(lockouts.Count>0)
				LockoutPopup.Init(lockouts);

			return validSaves.ToArray();
		}
	}

	public class CheckoutPopup : EditorWindow
	{
		static Texture2D tex;
		static List<string> files=new List<string>();
		public static void Init(List<string> checkouts)
		{
			files=checkouts;
			tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
			tex.SetPixel(0, 0, new Color(1f, 1f, 1f));
			tex.Apply();
			CheckoutPopup window = ScriptableObject.CreateInstance<CheckoutPopup>();
			window.position = new Rect(Screen.width / 2, Screen.height / 2, 250, 350);
			window.ShowPopup();
		}

		void OnGUI()
		{
			GUI.DrawTexture(new Rect(0, 0, 250, 350), tex, ScaleMode.StretchToFill);
			string append="";
			foreach(string s in files){
				append+="\n"+s;
			}
			GUIStyle style = new GUIStyle();
			style.fontStyle = FontStyle.Bold;
			style.wordWrap=true;
			EditorGUILayout.LabelField("The following files need to be checked out before saving:",EditorStyles.wordWrappedLabel);
			EditorGUILayout.LabelField(append, style);
			EditorGUILayout.LabelField("\nIf you do not wish to check out these files, then please revert any changes.", EditorStyles.wordWrappedLabel);
			if (GUILayout.Button("Checkout!")){ 
				CheckoutFiles(files);
				EditorApplication.ExecuteMenuItem("File/Save");
				this.Close();
			}
			if (GUILayout.Button("I'll revert")){ 
				Debug.Log("Nevermind");
				this.Close();
			}
		}
	}

	public class CheckinPopup : EditorWindow
	{
		static Texture2D tex;
		static string[] lines;
		static List<bool> toggles = new List<bool>();
		static List<string> fileNames = new List<string>();

		public static void Init()
		{
			tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
			tex.SetPixel(0, 0, new Color(1f, 1f, 1f));
			tex.Apply();

			lines = File.ReadAllLines(GetResFilePath());
			toggles.Clear();
			fileNames.Clear();
			foreach (string l in lines){
				if (l[0] == ';')
					continue;
				string[] parts = l.Split(new string[] { "###" }, System.StringSplitOptions.None);
				if (parts.Length == 3 && parts[1] == System.Environment.UserName){
					fileNames.Add(parts[0]);
					toggles.Add(false);
				}
			}

			CheckinPopup window = ScriptableObject.CreateInstance<CheckinPopup>();
			window.position = new Rect(Screen.width / 2, Screen.height / 2, 500, 500);
			window.ShowPopup();
		}

		void OnGUI()
		{
			GUI.DrawTexture(new Rect(0, 0, 500, 500), tex, ScaleMode.StretchToFill);
			float orig = EditorGUIUtility.labelWidth;
			EditorGUIUtility.labelWidth = 350;
			EditorGUILayout.BeginToggleGroup("Pick which files you want to return:", true);

			for (int i = 0; i < fileNames.Count; ++i){
				toggles[i] = EditorGUILayout.Toggle(fileNames[i], toggles[i]);
			}
			EditorGUILayout.EndToggleGroup();

			if (GUILayout.Button("Check-in!")){
				CheckInSelect(fileNames, toggles);
				this.Close();
			}
			if (GUILayout.Button("Exit"))
				this.Close();

			EditorGUIUtility.labelWidth = orig;
		}
	}

	public class LockoutPopup : EditorWindow
	{
		static List<string[]> resInfo =new List<string[]>();
		static Texture2D tex;
		public static void Init(List<string[]> info)
		{
			resInfo=info;
			tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
			tex.SetPixel(0, 0, new Color(1f, 1f, 1f));
			tex.Apply();
			LockoutPopup window = ScriptableObject.CreateInstance<LockoutPopup>();
			window.position = new Rect(Screen.width / 2, Screen.height / 2, 250, 350);
			window.ShowPopup();
		}

		void OnGUI()
		{
			GUI.DrawTexture(new Rect(0, 0, 250, 350), tex, ScaleMode.StretchToFill);
			EditorGUILayout.LabelField("The following files could not be saved becaused they are checked out by other developers:", EditorStyles.wordWrappedLabel);
			string append = "";
			foreach(string[] foo in resInfo)
				append+="\n"+foo[0]+" by: "+foo[1]+" on: "+foo[2];
			GUIStyle style = new GUIStyle();
			style.fontStyle = FontStyle.Bold;
			style.wordWrap=true;
			EditorGUILayout.LabelField(append, style);
			EditorGUILayout.LabelField("\nPlease be sure to revert changes to prevent merge conflict.", EditorStyles.wordWrappedLabel);
			if (GUILayout.Button("Accept")){ 
				Debug.Log("Accept");
				this.Close();
			}
		}
	}

	public class ErrorPopup : EditorWindow
	{
		static Texture2D tex;
		static List<string> labels;
		static string label;

		public static void Init(List<string> l)
		{
			labels = l;
			label = null;
			tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
			tex.SetPixel(0, 0, new Color(1f, 1f, 1f));
			tex.Apply();
			ErrorPopup window = ScriptableObject.CreateInstance<ErrorPopup>();
			window.position = new Rect(Screen.width / 2, Screen.height / 2, 250, 350);
			window.ShowPopup();
		}

		public static void Init(string l){
			label = l;
			labels = null;
			tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
			tex.SetPixel(0, 0, new Color(1f, 1f, 1f));
			tex.Apply();
			ErrorPopup window = ScriptableObject.CreateInstance<ErrorPopup>();
			window.position = new Rect(Screen.width / 2, Screen.height / 2, 250, 350);
			window.ShowPopup();
		}

		void OnGUI()
		{
			GUI.DrawTexture(new Rect(0, 0, 250, 350), tex, ScaleMode.StretchToFill);
			GUIStyle style = new GUIStyle();
			style.fontStyle = FontStyle.Bold;
			style.wordWrap = true;

            if (label != null){
				EditorGUILayout.LabelField(label, style);
			}
			else if (labels != null){
				EditorGUILayout.LabelField("These files must be committed/reverted before returning:", EditorStyles.wordWrappedLabel);
				foreach (string s in labels){
					EditorGUILayout.LabelField(s, style);
				}
			}
			
			if (GUILayout.Button("Ok")){
				this.Close();
			}
		}
	}


}
