using System;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Linq;

namespace dk.marten.ortelius
{
	/// <summary>
	/// Class to make crosslink between different datatypes in the project
	/// </summary>
	public class DataType
	{
		public string Name { get; set; }
		public string Package { get; set; }
		public string Extras { get; set; }
		
		public DataType(string name ,string package)
		{			
			this.Name = name;
			this.Package = package;
		}
		
		public DataType(string name ,string package ,string extras)
		{			
			this.Name = name;
			this.Package = package;
			this.Extras = extras;
		}
		
	}
	
	

 
	
	
	/// <summary>
	/// Class to make crosslink between different datatypes in the project
	/// Made with a non multithread safe pattern
	/// </summary>
	public class DataTypeUtil
	{
		
		private static DataTypeUtil instance;
		public static DataTypeUtil Instance
		{
	    	get 
	    	{
	         if (instance == null)
	         {
	            instance = new DataTypeUtil();
	         }
	         return instance;
	      }
	   }
		
		
		private List<DataType> allDataTypes;
		
		private DataTypeUtil()
		{
			allDataTypes = new List<DataType>();
		
			//Standard AS3 datatypes
			allDataTypes.Add(new DataType("Sprite", "flash.display", "#"));
			allDataTypes.Add(new DataType("MovieClip", "flash.display", "#"));
			allDataTypes.Add(new DataType("Vector3D ", "flash.geom", "#"));
			
		}
		
		/// <summary>
		/// Adds a new datatype to the list
		/// </summary>
		/// <param name="name">Name of the datatype/object</param>
		/// <param name="package">The package the datatype belongs to</param>
		/// <param name="extras">Posible hash for types that are not a part of the documentation for the datatype</param>
		public void AddDataType(string name, string package, string extras){
			allDataTypes.Add(new DataType(name,  package,  extras));
		}
		
		/// <summary>
		/// Get the fullpath from the import path
		/// </summary>
		/// <param name="classPackage"></param>
		/// <returns></returns>
		public string GetFullPath(string classPackage){
			if(classPackage.EndsWith("*")) return "#"+classPackage;
			int lastDot = classPackage.LastIndexOf(".");
			if(lastDot == -1) lastDot = 0;
			else lastDot++;
			
			string name = classPackage.Substring(lastDot,classPackage.Length-lastDot);
			string[] test = new string[]{classPackage};
			
			string fullPath = GetFullPath(name,test);
			if(fullPath == "#"+name) fullPath = "#"+classPackage;
			return fullPath;
		}
		
		/// <summary>
		/// Get the full path of a given datatype
		/// </summary>
		/// <param name="name"></param>
		/// <param name="importedClassPackages">The imported packages including the datatypes own pakage</param>
		/// <returns>The full path to the datatype.
		/// </returns>
		public string GetFullPath(string name, string[] importedClassPackages){
			if(name == "*") return "#asterisk";
						
			var posibleTypes = 	from dType in allDataTypes
								where dType.Name == name
								orderby dType.Extras descending
								select dType;
			
			string fullPath;
			string pc;
			
            foreach (DataType dt in posibleTypes)
		    {
				fullPath = (dt.Package=="") ? (dt.Name) :(dt.Package + "." + dt.Name);
            	foreach(string packageClass in importedClassPackages){
					pc = packageClass.Replace("*",dt.Name);
					if(pc == fullPath)return dt.Extras+fullPath;
				}
			}
			
			return "#"+name;
		}
		
	}
}
