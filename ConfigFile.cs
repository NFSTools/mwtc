using System;
using System.Collections;
using System.IO;
using System.Text;

namespace mwtc
{
	class ConfigFile 
	{
		Hashtable _sections;

		private object this[string name] 
		{
			get 
			{ 
				if (_sections.ContainsKey(name))
					return _sections[name];
				else
					return null;
			}
		}

		public string GetValue(string section, string key) 
		{
			Hashtable ht = this[section.ToLower()] as Hashtable;
			if (ht == null)
				return null;
			return ht[key.ToLower()] as string;
		}

		public string GetValue(string section, int index, string key) 
		{
			ArrayList list = this[section.ToLower()] as ArrayList;
			if (list == null)
				return null;
			Hashtable ht = list[index] as Hashtable;
			if (ht == null)
				return null;
			return ht[key.ToLower()] as string;
		}

		public int GetCount(string section) 
		{
			ArrayList list = this[section] as ArrayList;
			if (list == null)
				return -1;
			else
				return list.Count;
		}

		public void LoadFile(string filename, string[] multi) 
		{
			ArrayList multiSections = new ArrayList(multi);
			StreamReader sr = new StreamReader(filename, Encoding.Default, true);
			Hashtable currentSection = null;
			_sections = new Hashtable();
			while(true) 
			{
				string line = sr.ReadLine();
				if (line == null) 
					break;
				line = line.Trim();
				if (line.StartsWith("#") || line.StartsWith(";") || line == "")
					continue;

				if (line.StartsWith("[") && line.EndsWith("]")) 
				{
					string sectionName;
					if (line != "[]")
						sectionName = line.Substring(1,line.Length-2).ToLower();
					else
						sectionName = "";
					if (_sections.ContainsKey(sectionName)) 
					{
						if (multiSections.Contains(sectionName)) 
						{
							currentSection = new Hashtable();
							ArrayList list = this[sectionName] as ArrayList;
							list.Add(currentSection);
						} 
						else 
						{
							currentSection = this[sectionName] as Hashtable;
						}
					} 
					else 
					{
						currentSection = new Hashtable();
						if (multiSections.Contains(sectionName)) 
						{
							ArrayList list = new ArrayList();
							list.Add(currentSection);
							_sections.Add(sectionName, list);
						} 
						else 
						{
							_sections.Add(sectionName, currentSection);
						}
					}
				} 
				else 
				{
					if (line.IndexOf("=")>-1) 
					{
						string[] kvPair = line.Split(new char[] {'='}, 2);
						string key = kvPair[0].Trim();
						string val = "";
						if (kvPair.Length > 1)
							val = kvPair[1].Trim();
						currentSection.Add(key.ToLower(), val);
					}
				}

			}
			sr.Close();
		}

	}
}
