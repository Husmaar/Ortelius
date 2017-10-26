/*
 * Created by SharpDevelop.
 * User: moe
 * Date: 13-02-2008
 * Time: 18:10
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.Collections;
using System.IO;

namespace dk.marten.ortelius
{
	
	#region Objects for storing settings
	[Serializable]
	public class ProjectSettings{
		
		
		private string destinationPath="";
		public string DestinationPath {
			get{
				return destinationPath;
			}
			set{
				destinationPath=value;
			}
		}
		
		private string styleName = "OrteliusAjax";
		public string StyleName {
			get{
				return styleName;
			}
			set{
				styleName=value;
			}
		}
		private string docXmlFileName = "/ortelius.xml";
		public string DocXmlFileName {
			get{
				return docXmlFileName;
			}
			set{
				docXmlFileName=value;
			}
		}
		private string docHtmlFileName = "/index.html";
		public string DocHtmlFileName {
			get{
				return docHtmlFileName;
			}
			set{
				docHtmlFileName=value;
			}
		}
		
		private bool xmlDelete = true;
		public bool XmlDelete {
			get{
				return xmlDelete;
			}
			set{
				xmlDelete=value;
			}
		}
		private ArrayList allFiles;
		public ArrayList AllFiles {
			get{
				return allFiles;
			}
			set{
				allFiles=value;
			}
		}
		
		private string lastFolderName = "";
		public string LastFolderName {
			get{
				return lastFolderName;
			}
			set{
				lastFolderName=value;
			}
		}
		
		private string introText = "";
		public string IntroText {
			get{
				return introText;
			}
			set{
				introText=value;
			}
		}
		
		private string introHeader = "";
		public string IntroHeader {
			get{
				return introHeader;
			}
			set{
				introHeader=value;
			}
		}
		
		private bool showAfterBuild = false;
		public bool ShowAfterBuild {
			get{
				return showAfterBuild;
			}
			set{
				showAfterBuild=value;
			}
		}
		
		private int language = 1;
		public int Language {
			get{
				return language;
			}
			set{
				language = value;
			}
		}		
				
		public ProjectSettings(){
			AllFiles= new ArrayList();
		}
		
		public bool AddFile(string fileName){
			
			bool test = false;
			string filExtension = Path.GetExtension(fileName);
			if (filExtension == ".as" || filExtension == ".js" ){
					test = true;
					foreach(string asFile in allFiles){
						//test om filen findes allerede
						test = test && (asFile != fileName);						
					}
					if(test) allFiles.Add(fileName);
					
			}
									
			return test;
		}
		
		public void RemoveFile(string fileName){

			string filExtension = Path.GetExtension(fileName);
			if (filExtension == ".as" || filExtension == ".js" ){
				allFiles.Remove(fileName);
			}
			
		}
		
		public void ResetProjectSettings(){
			AllFiles.Clear();
			
			destinationPath="";
			styleName = "OrteliusAjax";
			docXmlFileName = "/ortelius.xml";
			docHtmlFileName = "/index.html";
			introText = "";
			introHeader = "";
			showAfterBuild = false;
			language = 0;
		}



        public void ConvertFromLegacyProjectSettings(Ortelius.ProjectSettings legacyProjSettings)
        {
            ResetProjectSettings();
            AllFiles = legacyProjSettings.AllASFiles;
            IntroHeader = legacyProjSettings.IntroHeader;
            IntroText = legacyProjSettings.IntroText;
            DestinationPath = legacyProjSettings.DestinationPath;
            Language = legacyProjSettings.Language;
            LastFolderName = legacyProjSettings.LastFolderName;
            ShowAfterBuild = legacyProjSettings.ShowAfterBuild;
            StyleName = legacyProjSettings.StyleName;
            XmlDelete = legacyProjSettings.XmlDelete;
        }
	}
	
	
	
	
	[Serializable]
	public class GenerelSettings{
				
		private string currentProject = "";
		public string CurrentProject {
			get{
				return currentProject;
			}
			set{
				currentProject=value;
			}
		}
		
		private ArrayList projectHistory;
		public ArrayList ProjectHistory {
			get{
				return projectHistory;
			}
			set{
				projectHistory=value;
			}
		}
				
		private Boolean firstRun = true;
		public Boolean FirstRun {
			get{
				firstRun = false;
				return firstRun;
			}
		}
		
		public GenerelSettings(){
			projectHistory= new ArrayList();
		}
	}
	#endregion
	
}
