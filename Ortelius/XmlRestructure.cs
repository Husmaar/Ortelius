using System;
using System.Xml;
using System.Xml.XPath;
using System.Collections;

namespace dk.marten.ortelius
{
	/// <summary>
	/// Description of DocEvaluator.
	/// </summary>
	public class XmlRestructure
	{
		private XmlDocument documentationXml;
		public XmlDocument DocumentationXml {
			get{
				return documentationXml;
			}
			set{
				documentationXml=value;
			}
		}
		
		
		private string errorLog="";
		public string Errors {
			get{
				return errorLog;
			}
		}
				
		private XmlNode contentCloneXml;
		
		/// <summary>
		/// 
		/// </summary>
		public XmlRestructure(XmlDocument allDocXml)
		{
			this.documentationXml = allDocXml;			
		}
		
		private int testCounter = 0;
		/// <summary>
		/// 
		/// </summary
		public void CreateInheritedElements(){
			
			contentCloneXml = documentationXml.DocumentElement.Clone(); 
			XmlNodeList allClasses = documentationXml.GetElementsByTagName("class");
			foreach(XmlElement classNode in allClasses){
				string superClass = getExtendClassPath(classNode);
				if(superClass!="") addInheritedElements((XmlElement) classNode,superClass);
			}
		}
		
		
		/// <summary>
		/// 
		/// </summary
		private void addInheritedElements(XmlElement classNode,string superClassName){
			//ugly hack to make sure superClassNode isen't null
			XmlNode superClassNode = classNode.Clone();
				
			testCounter++;
			bool succes = false;
			XmlNodeList classes = contentCloneXml.SelectNodes("class");
			testCounter++;
			
			//find the super class in xml
			foreach(XmlElement testClass in classes){
				testCounter++;
				string testClassName="";
				if(testClass.SelectSingleNode("package")!=null && testClass.SelectSingleNode("name")!=null){
					testClassName =testClass.SelectSingleNode("package").InnerText+"."+testClass.SelectSingleNode("name").InnerText;
				}

				if(testClassName == superClassName){
					succes = true;
					superClassNode =  testClass;
				}
			}
				
			XmlNode inherHieraElement = classNode.SelectSingleNode("inheritanceHierarchy");
			XmlElement inHieraNode = documentationXml.CreateElement("inheritanceClass");
			
			
			if(!succes){
				inHieraNode.SetAttribute("fullPath","#"+superClassName);
				inHieraNode.InnerText = superClassName.Substring(superClassName.LastIndexOf(".")+1);
				inherHieraElement.AppendChild(inHieraNode);
				//the extended class dosent exist in the in the documentation
				return;
			}
			
			inHieraNode.SetAttribute("fullPath",superClassName);
			inHieraNode.InnerText = superClassName.Substring(superClassName.LastIndexOf(".")+1);
			inherHieraElement.AppendChild(inHieraNode);
			
			XmlNodeList elements = superClassNode.SelectNodes("method[@access = 'public'] | method[@access = 'protected'] | method[@access = 'internal'] | property[@access = 'public'] | property[@access = 'protected'] | property[@access = 'internal']");

			
			foreach(XmlElement elementNode in elements){
				//dont copy constructor of super class
				string elementNodeName = elementNode.SelectSingleNode("name").InnerText;
				
				if(superClassNode.SelectSingleNode("name").InnerText != elementNodeName){				
					
					elementNode.SetAttribute("inherited","true");
					elementNode.SetAttribute("inheritedFrom",superClassName);
					
					//Dont add if the element already exists
					XmlNodeList originalElements = classNode.SelectNodes("method[name = '"+elementNodeName+"'] | property[name = '"+elementNodeName+"']");
					if(originalElements.Count == 0) classNode.AppendChild(elementNode.Clone());
					else {
						//the element exist
						string rwSuper = elementNode.GetAttribute("readWrite");
						string rwSub = ((XmlElement) originalElements[0]).GetAttribute("readWrite");
						if(rwSub != "ReadWrite" && rwSuper != rwSub) ((XmlElement) originalElements[0]).SetAttribute("readWrite","ReadWrite");
					}
				}
				
			}	
			
			//add elements thats extends the superClass
			string superSuperClass = getExtendClassPath(superClassNode);
			if(superSuperClass!="") addInheritedElements((XmlElement) classNode,superSuperClass);
		}
		
		
		private string getExtendClassPath(XmlNode classNode){
			
				XmlNode exNode = classNode.SelectSingleNode("extends");
				string superSuperClass = "";
				if(exNode != null){
					superSuperClass = exNode.InnerText;
					if(superSuperClass.IndexOf(".")==-1){	
						//get the class own packagename
						string packageName = "";					
						XmlNode pNode = classNode.SelectSingleNode("package");
						if(pNode != null) packageName = pNode.InnerText;
							
						XmlNodeList importPack = classNode.SelectNodes("import");
						superSuperClass = getFullExtendClassPath(superSuperClass,packageName,importPack);
						
					}
					
				}
				
				return superSuperClass;
		}
		
			
				
				
		
		private string getFullExtendClassPath(string className,string packageName,XmlNodeList importPack){
				
				
					if(className.IndexOf(".")==-1){
						foreach(XmlElement package in importPack){	
							string packName = "";
							XmlNode pNode= package.SelectSingleNode("packageName");
							if(pNode != null){
								packName = pNode.InnerText;
								if(packName.EndsWith(className)){
									return packName;
								}
								if(packName.EndsWith(".*") && testForExistense(className,packName.Replace(".*",""))){
									return packName.Replace("*",className);
								}
							}
						}
						
						if(className.IndexOf(".")==-1 && packageName!="" && packageName!=null && testForExistense(className,packageName)){							
							return packageName+"."+className;							
						}
						
					}
				return className;
		}
		
		private bool testForExistense(string className,string packageName){	
			XmlNodeList existingElements = contentCloneXml.SelectNodes("//class[package='"+packageName+"' and name='"+className+"']");
			if(existingElements.Count == 1) return true;
			else return false;
		}
		
		/// <summary>
		/// Creates the xml for the package/class tree
		/// </summary
		public void CreateNestedPackages(){			
			makePackageTree();
		}
		/// <summary>
		/// 
		/// </summary
		private void makePackageTree(){
			ArrayList allPackages = new ArrayList();
			allPackages.Add("");
			XmlNodeList blokke = documentationXml.GetElementsByTagName("class");
			foreach(XmlElement node in blokke){				
				XmlNode pNode = node.SelectSingleNode("package");				
				
				if(pNode != null){
					bool exists = false;
					foreach(string packageName in allPackages){
						if(pNode.InnerText==packageName) exists= true;
					}
					if(!exists) allPackages.Add(pNode.InnerText);
				}
			}
			
			XmlElement allPackagesNode = documentationXml.CreateElement("allpackages");			
			documentationXml.DocumentElement.AppendChild(allPackagesNode);
			
			populatePackageTree(allPackagesNode,"");
			
			foreach(string packageName in allPackages){
				addPackageLevel(allPackagesNode,packageName,"");
			}
		}
		private int apIndex = 0;
		/// <summary>
		/// 
		/// </summary
		private void addPackageLevel(XmlElement xmlElement,string name,string supername){
			if(name=="" || name==null) return;
			apIndex++;
			string topLevel;
			int firstDot = name.IndexOf(".");
			if(firstDot == -1) topLevel = name;
			else topLevel = name.Substring(0,firstDot);
			if(topLevel == "" || topLevel==null)return;
			
			if(supername!="") supername += ".";
			supername += topLevel;
			
			XmlElement packageNode;
			XmlNodeList blokke = xmlElement.SelectNodes("packagelevel[@name ='"+topLevel+"']");
			if(blokke.Count == 0){
				packageNode = documentationXml.CreateElement("packagelevel");
				packageNode.SetAttribute("name",topLevel);
				packageNode.SetAttribute("fullname",supername.Replace(".","_"));
				
				xmlElement.AppendChild(packageNode);				
			}else {
				packageNode = (XmlElement) blokke[0];
			}
			string nextName = "";
			firstDot = name.IndexOf(".");
			if(firstDot!=-1){
				nextName = name.Substring(firstDot,name.Length-firstDot);
				nextName = nextName.TrimStart('.');
			}
			
			addPackageLevel(packageNode,nextName,supername);
			
			populatePackageTree(packageNode,supername);
			
		}
		private int ppIndex = 0;
		/// <summary>
		/// 
		/// </summary>
		private void populatePackageTree(XmlElement xmlElement,string packageName){
			ppIndex++;
			XmlNodeList blokke = contentCloneXml.SelectNodes("class[package ='"+packageName+"']");
			foreach(XmlElement node in blokke){	
				XmlNode cNode = node.SelectSingleNode("name");			
				
				//!!!!!!!!!!!!!!!det er noget rod det her!!!!!!!!!!!!!!
				XmlNodeList testblokke = xmlElement.SelectNodes("packageClass[@package ='"+packageName+"' and @class ='"+cNode.InnerText+"']");
				if(testblokke.Count == 0){
					XmlElement classNode = documentationXml.CreateElement("packageClass");
					classNode.SetAttribute("package",packageName);
					classNode.SetAttribute("class",cNode.InnerText);
					
					XmlNode fidNode = node.SelectSingleNode("fid");
					classNode.SetAttribute("fid", fidNode.InnerText);
					
					xmlElement.AppendChild(classNode);
				}
				
			}
		}
		
		/// <summary>
		/// 
		/// </summary>
		public void UpdateSetterGetters(){				
			updateSetterGetters("public");
			updateSetterGetters("protected");
		}
		
		private void updateSetterGetters(string access){
			//[name=""]
			XmlNodeList readProperties = documentationXml.DocumentElement.SelectNodes("/docElements/class/property[@access='"+access+"' and  @readWrite ='Read']");
			
			
			foreach(XmlElement readNode in readProperties){
				string readName = readNode.SelectSingleNode("name").InnerText;
				string readClassName = readNode.ParentNode.SelectSingleNode("name").InnerText;
			
				
				XmlNodeList writeProperties = documentationXml.DocumentElement.SelectNodes("/docElements/class[name='"+readClassName+"']/property[@access='"+access+"' and @readWrite !='Read']");
				foreach(XmlElement writeNode in writeProperties){
					string writeName = writeNode.SelectSingleNode("name").InnerText;
					if(writeName == readName){
						readNode.SetAttribute("readWrite","ReadWrite");
						writeNode.ParentNode.RemoveChild(writeNode);
						
					}
				}
			
			}
			
			
		}
		
		
		/// <summary>
		/// Creates the xml for the class list sorted by name
		/// </summary
		public void CreateClassIndex(){
			
			XmlElement classByNameNode = documentationXml.CreateElement("classNameIndex");
			documentationXml.DocumentElement.AppendChild(classByNameNode);
						
			XmlNodeList allClasses = documentationXml.DocumentElement.SelectNodes("/docElements/class");
			
			
			foreach (XmlElement node in allClasses) {
				try{
					XmlElement classNode = documentationXml.CreateElement("packageClass");
					XmlNode nameNode = node.SelectSingleNode("name");
					classNode.SetAttribute("class", nameNode.InnerText);	
					XmlNode packageNode = node.SelectSingleNode("package");
					classNode.SetAttribute("package", packageNode.InnerText);	
					XmlNode ticks = node.SelectSingleNode("modified/@ticks");
					classNode.SetAttribute("modified", ticks.InnerText);
					XmlNode fidNode = node.SelectSingleNode("fid");
					classNode.SetAttribute("fid", fidNode.InnerText);
						
					classByNameNode.AppendChild(classNode);
				}catch(Exception){}
			}
			
		}

		public void UpdateTypesFullpath(){
			//<type fullPath="#String" context="com.adnuvo.util.FlashVars">String</type>
			
			DataTypeUtil allDataTypes = DataTypeUtil.Instance;
			
			XmlNodeList allTypes = documentationXml.DocumentElement.SelectNodes("//type");
			foreach (XmlElement node in allTypes) {
				string typeContext = node.GetAttribute("context");
				string typeName = node.InnerText;
				node.SetAttribute("fullPath",allDataTypes.GetFullPath(typeName,typeContext.Split(',')));
				node.RemoveAttribute("context");
			}
			
			XmlNodeList allPackageName = documentationXml.DocumentElement.SelectNodes("//packageName");
			foreach (XmlElement node in allPackageName) {
				string packageName = node.InnerText;
				node.SetAttribute("fullPath",allDataTypes.GetFullPath(packageName));
			}
			
			
				
		}
	}
}
