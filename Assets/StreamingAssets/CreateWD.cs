using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Windows.Forms;

class CreateWD
{

	//helper function
	public static string ReplaceSection(string body, string header, string newData){
		string[] lines = body.Split(new string[] {System.Environment.NewLine}, System.StringSplitOptions.None);
		string newBody = "";
		bool atSection=false;
		int count=0;
		foreach(string l in lines){
			if(count>=lines.Length-1)
				break;
			if(!atSection){
				if(l.Trim()==header)
					atSection=true;
				newBody+=l.Trim()+System.Environment.NewLine;
			}
			else if(atSection){
				if(l.Trim()=="")
				{
					newBody+=newData;
					atSection=false;
				}
			}
			count++;
		}
		if(atSection)
			newBody+=newData;
		return newBody;
	}
	
	//helper function
	public static void AddToNumberedSection(string path,string section,string[] data){
		if(!File.Exists(path))
			return;

		int count=0;
		string [] iniLines = File.ReadAllLines(path);
		string output="";
		bool atSection=false;
		foreach(string l in iniLines){
			if(!atSection && l=="["+section+"]")
				atSection=true;
			else if(atSection){
				if(l==""||l[0]=='[')
				{

					for(int i=count; i<count+data.Length; i++){
						output +=i+" = "+data[i-count]+System.Environment.NewLine;
					}
					atSection=false;
					//break;
				}
				else
				{
					if(l[0]!=';')
					{
						count++;
					}
				}
			}
			output +=l+System.Environment.NewLine;
		}
		File.WriteAllText(path,output);
	}

	//helper function
	public static Dictionary<string,string> GetStringDict(string path,string section){
		if(!File.Exists(path))
			return null;

		Dictionary<string,string> items = new Dictionary<string,string>();
		string [] iniLines = File.ReadAllLines(path);
		bool atSection=false;
		foreach(string l in iniLines){
			if(!atSection && l=="["+section+"]")
				atSection=true;
			else if(atSection){
				if(l==""||l[0]=='[')
					break;
				else
				{
					if(l[0]!=';')
					{
						string [] parts = l.Split('=');
						string ids = parts[0].Trim();
						string str = parts[1].Trim();
						items.Add(ids,str);
					}
				}
			}
		}
		return items;
	}

	//helper function
	static string GetSection(string body, string header){
		string[] lines = body.Split(new string[] {System.Environment.NewLine}, System.StringSplitOptions.None);
		string section ="";
		bool atSection=false;
		int count=0;
		bool first=true;
		foreach(string l in lines){
			if(count>lines.Length-1)
				break;
			if(!atSection){
				if(l.Trim()=="["+header+"]")
					atSection=true;
			}
			else if(atSection){
				if(l.Trim()==""||l.Trim()[0]=='[')
				{
					atSection=false;
				}
				else if(l[0]!=';')
				{
					if(!first)
						section+="\n"+l.Trim();
					else
					{
						section+=l.Trim();
						first=false;
					}
				}
			}
			count++;
		}
		return section;
	}

	//main
	static void Main(string[] args)
	{
		string streamingAssetsPath = Path.GetDirectoryName(Application.ExecutablePath);
		//bool logging=(args.Length>0 && args[0]=="-v");
		bool logging = true;
		string workDir = Environment.ExpandEnvironmentVariables("%localappdata%").Replace('\\','/') + "/Cal3D";
		Dictionary<string,string[]> savedNavs = new Dictionary<string,string[]>();

		//create directory as needed
		if(!Directory.Exists(workDir)){
			Directory.CreateDirectory(workDir);
			if(logging)
				Console.WriteLine("Work directory does not exist - creating...");
		}
		//user already has working directory
		else{
			string edgePath = workDir+"/EdgeCalifia3D.ini";
			if(!File.Exists(edgePath)){
				Console.WriteLine("couldn't find edge file, oops");
			}
			else{
				//check catalogs
				string [] catFiles = Directory.GetFiles(workDir, "catalog_*.ini");
				foreach(string c in catFiles){
					Console.WriteLine("Found cat file: "+c);
					//check locked navs
					string lockedNavBody = GetSection(File.ReadAllText(streamingAssetsPath+"/"+Path.GetFileName(c)),"View");
					int lockedNavs=lockedNavBody.Split('\n').Length;
					//check user navs
					string navs = GetSection(File.ReadAllText(c),"View");
					string [] navLines = navs.Split('\n');
					Console.WriteLine(navLines.Length);
					//save user navs
					if(navLines.Length-lockedNavs>0){
						string[] userNavs = new string[navLines.Length-lockedNavs];
						for(int i=lockedNavs; i<navLines.Length; i++){
							userNavs[i-lockedNavs]=navLines[i].Split('=')[1].Trim();
						}
						savedNavs.Add(c,userNavs);
					}
				}
			}
			
			//after pulling users navs, let's reset the work dir
			Directory.Delete(workDir,true);
				Directory.CreateDirectory(workDir);
				if(logging)
					Console.WriteLine("Clearing old work dir");
		}

		//copying files
		if(logging)
			Console.WriteLine("Copying ini files from: "+streamingAssetsPath+" to: "+workDir);
		//ini files
		string [] configFiles = Directory.GetFiles(streamingAssetsPath, "*.ini");
		foreach(string c in configFiles){
			string fn = Path.GetFileName(c);
			if(fn=="EdgeCalifia3D.ini")
				continue;
			if(logging)
				Console.WriteLine("Copying: "+fn);
			try{
				File.Copy(c,workDir+"/"+fn);
			}
			catch(Exception e){
				if(logging)
				{
					Console.WriteLine("exception copying: "+c+" to "+workDir);
					Console.WriteLine(e.Message);
				}
			}
		}
		//json files
		string [] jsonFiles = Directory.GetFiles(streamingAssetsPath, "*.json");
		foreach(string c in jsonFiles){
			string fn = Path.GetFileName(c);
			try{
				File.Copy(c,workDir+"/"+fn);
			}
			catch(Exception e){
				if(logging)
				{
					Console.WriteLine("exception copying: "+c+" to "+workDir);
					Console.WriteLine(e.Message);
				}
			}
		}
		if(logging)
			Console.WriteLine("Copying txt files...");
		string[] txtFiles = Directory.GetFiles(streamingAssetsPath, "*.txt");
		foreach(string c in txtFiles){
			string fn = Path.GetFileName(c);
			if(logging)
				Console.WriteLine("Copying: "+fn);
			try{
				File.Copy(c,workDir+"/"+fn);
			}
			catch(Exception e){
				if(logging)
				{
					Console.WriteLine("exception copying: "+c+" to "+workDir);
					Console.WriteLine(e.Message);
				}
			}
		}

		//Add user views to catalog files in working directory
		string [] workCats = Directory.GetFiles(workDir,"catalog_*.ini");
		foreach(string s in workCats){
			if(savedNavs.ContainsKey(s)){
				AddToNumberedSection(s,"View",savedNavs[s]);
			}
		}

		//piece de resistance - EdgeCalifia3D.ini
		string [] configData = File.ReadAllLines(streamingAssetsPath+"/EdgeCalifia3D.ini");
		string before ="";
		string after = "";
		string prefix = "";
		bool prefixFound=false;
		foreach(string line in configData){
			string[] parts = line.Split('=');
			if(parts.Length>0 && parts[0].Trim()=="Prefix"){
				prefixFound=true;
				string userName = Environment.UserName;
				userName=userName.Trim().Replace(' ','_');
				if(logging)
					Console.WriteLine("test: "+userName);
				prefix="Prefix="+userName+"\n";
			}
			else{
				if(!prefixFound){
					before+=line+"\n";
				}
				else{
					after+=line+"\n";
				}
			}
		}
		string sumTotal = before+prefix+after;
		File.WriteAllText(workDir+"/EdgeCalifia3D.ini",sumTotal);
	}
}
