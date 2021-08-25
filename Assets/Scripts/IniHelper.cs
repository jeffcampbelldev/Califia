//IniHelper.cs
//
//Description: Helper class for reading ini files
//Probably redundant with System methods GetPrivateProfileString but it works so...
//methods include reading ini sections and writing ini sections
//Note: For reading and writing individaul items, use INIParser a 3rd part script
//


using System.Collections;
using System.Collections.Generic;
using System.IO;

public class IniHelper
{
	//get section
	//key : int
	//value: string
	public static Dictionary<int,string> GetStrings(string path,string section){
		if(!File.Exists(path))
		{
			return null;
		}

		Dictionary<int,string> items = new Dictionary<int,string>();
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
						int id = int.Parse(ids);
						string str = parts[1].Trim();
						items.Add(id,str);
					}
				}
			}
		}
		return items;
	}

	public static Dictionary<string,string> GetStringsS(string path,string section){
		if(!File.Exists(path))
		{
			return null;
		}

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

	//get section
	//key : string
	//value : string
	public static Dictionary<string,string> GetStringDict(string path,string section){
		if(!File.Exists(path))
		{
			return null;
		}

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

	//Get Section
	//key : int
	//value : int
	public static Dictionary<int,int> GetInts(string path,string section){
		if(!File.Exists(path))
		{
			return null;
		}

		Dictionary<int,int> items = new Dictionary<int,int>();
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
						int id = int.Parse(ids);
						string strInt = parts[1].Trim();
						int val = int.Parse(strInt);
						items.Add(id,val);
					}
				}
			}
		}
		return items;
	}

	//get number of entries under header
	public static int GetSectionSize(string body, string header){
		string[] lines = body.Split(new string[] {System.Environment.NewLine}, System.StringSplitOptions.None);
		int sectionSize=0;
		bool atSection=false;
		int count=0;
		foreach(string l in lines){
			if(count>=lines.Length-1)
				break;
			if(!atSection){
				if(l.Trim()=="["+header+"]")
					atSection=true;
			}
			else if(atSection){
				if(l.Trim()=="")
				{
					atSection=false;
				}
				else if(l[0]!=';')
					sectionSize++;
			}
			count++;
		}
		return sectionSize;
	}

	//gets a section in full - line by line
	public static string[] GetSection(string body, string header){
		string[] lines = body.Split(new string[] {System.Environment.NewLine}, System.StringSplitOptions.None);
		List<string> section = new List<string>();
		bool atSection=false;
		int count=0;
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
					section.Add(l.Trim());
			}
			count++;
		}
		return section.ToArray();
	}

	//body: entire ini body
	//header: header label including brackets
	//new data: string to replace body with including new line breaks
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

	public static string GetValue(string path, string header, string key){
		string val="";
		Dictionary<string,string> data = GetStringsS(path,header);
		if(data.ContainsKey(key))
			val=data[key];
		return val;
	}

	public static string GetValue(string path, string header, int key){
		string val="";
		Dictionary<int,string> languages = GetStrings(path,header);
		if(languages.ContainsKey(key))
			val=languages[key];
		return val;
	}
}
