using System;
using System.Collections;
using System.Text.RegularExpressions;
using System.Xml;

namespace dk.marten.ortelius
{
	/// <summary><![CDATA[
	/// Description of build.
	/// ]]></summary>
	public class ASDocumentationBuilder : IDocumentationBuilder
	{		
		private string systemSvar = "";
		public string SystemSvar
		{
	      get
	      {
	         return systemSvar;
	      }
	
	      set
	      {
	         systemSvar = value;
	      }
	   }
		 
//		private string delimeter = "*";
//		private string startTag = "/**";
		private string classType = "";
		private string currentPackages = "";
		
		
		private Regex accessPublicTest = new Regex(".*public.*");
		private Regex accessProtectedTest = new Regex(".*protected.*");
		private Regex accessInternalTest = new Regex(".*internal.*");
		private Regex accessPrivateTest = new Regex(".*private.*");
		private string accessString = "protected|public|internal|private";
		private Regex accessPublicAndProtectedTest;
		private Regex commentPublicAndProtectedTest;
		private Regex lastWhiteSpace = new Regex(@"[\t| ]*$");
		private Regex packageTest = new Regex(@"^[\t| ]*package");		
		
		private	Regex publicClassTest = new Regex(@"^[^\*/]*(public|internal)[ |\t][^\*/]*class[ |\t]");
		private	Regex interfaceTest = new Regex(@"^[^\*/]*(public|internal)[ |\t][^\*/]*interface[ |\t]");
		
		private Regex funcTest = new Regex(@"^[^\*/]*function +");
		private Regex funcGetTest = new Regex("function +get +");
		private Regex funcSetTest = new Regex("function +set +");
		
		private string[] modifiers;



        public ASDocumentationBuilder()
		{			
			accessPublicAndProtectedTest = new Regex(".*("+accessString+").*");
			commentPublicAndProtectedTest = new Regex(@".*(//|/\*|\*).*("+accessString+")");
			modifiers = new string[]{"override","dynamic","static","final","abstract","virtual"};
		}

		
		public XmlNodeList AddFile(string[] asFileLines,DateTime modifiedTime, string filename)
		{
			currentPackages = "";
			string classXml = "<modified ticks=\""+modifiedTime.Ticks+"\">"+String.Format("{0:d/M yyyy}", modifiedTime)+"</modified>";
			asFileLines = Utils.cleanUpLines(asFileLines,true);
			classXml += getImportInfo(asFileLines);
			classXml += getClassInfo(asFileLines);
			classXml += getOtherInfo(asFileLines);
			
			
			XmlDocument xml = new XmlDocument();
			xml.LoadXml("<nodes><class>"+classXml+"</class></nodes>");
			XmlNodeList classes = xml.SelectNodes("/nodes/class");
			
			
			return classes;
		}
		
		public string GetSystemSvar(){
			return systemSvar;
		}
		
		#region Creates the different doc xml
		
		///<summary>
		///
		///</summary
		private string getImportInfo(string[] asFileLines)
		{
			string resultText = "";				
			
			for(int i = 0; i<asFileLines.Length;i++ ){
				string fileLine = asFileLines[i];
				
				if(fileLine.IndexOf("import ") == 0){
					
					int namePosStart = fileLine.IndexOf("import ")+7;
					string text = fileLine.Substring(namePosStart,(fileLine.Length-namePosStart));
					text = text.TrimEnd(';');
					resultText += "<import><packageName fullPath=\"\">"+text+"</packageName></import>\r\n";
					resultText.TrimEnd(("\r\n ;").ToCharArray());
					currentPackages += text+",";
				}
			}
			
			currentPackages = currentPackages.TrimEnd(',');
			return resultText;
		}
		
		
		///<summary><![CDATA[
		///
		///</summary
		private string getClassInfo(string[] asFileLines)
		{
			string packageName = "";
			string resultText = "";

			
			for(int i = 0; i<asFileLines.Length;i++ ){
				string fileLine = asFileLines[i];
				if(packageTest.IsMatch(fileLine)) packageName = stripElement(fileLine,@"package *",@" +|{ *");
				
				if(publicClassTest.IsMatch(fileLine)){
					resultText += "<inheritanceHierarchy/>\r\n";
					if(fileLine.IndexOf(" extends ") != -1) resultText += "<extends>"+stripElement(fileLine,@"(.*extends +)",@"([ +|{].*)")+"</extends>\r\n";
					if(fileLine.IndexOf(" implements ") != -1) resultText += "<implements>"+stripElement(fileLine,@"(.*implements +)",@"([ +|{].*)")+"</implements>\r\n";
								
					if(fileLine.IndexOf("static ") != -1) resultText += "<modifier>static</modifier>\r\n";
					if(fileLine.IndexOf("dynamic ") != -1) resultText += "<modifier>dynamic</modifier>\r\n";
					
					
					resultText += "<package>"+packageName+"</package>\r\n";
					currentPackages += (packageName=="")?  ",*" : ","+packageName+".*";
					
					resultText += "<name>"+stripElement(fileLine,@" *(\[.*\] +)?(dynamic +)?(final +)?((public|internal) +)?(dynamic +)?(final +)?(class +)",@"( +extends)?( +\S*)?( +implements)?( +\S*)? *{? *")+"</name>\r\n";
					resultText += Utils.getId();
					
					resultText += "<summary><![CDATA["+Utils.getSummery(asFileLines,i)+"]]></summary>\r\n";
					//tjek for events
					resultText += Utils.getStandAloneTags(asFileLines,i);
					resultText += getEventTags(asFileLines,i);
					classType= "class";
					
				}else if(interfaceTest.IsMatch(fileLine)){
					resultText += "<package>"+packageName+"</package>\r\n";
					
					resultText += "<name>"+stripElement(fileLine,@" *((public|internal) +)?(interface +)",@"( +extends)?( +\S*)? *{? *")+"</name>\r\n";
					resultText += Utils.getId();
					resultText += "<summary><![CDATA["+Utils.getSummery(asFileLines,i)+"]]></summary>\r\n";
					//tjek for events
			 		resultText += Utils.getStandAloneTags(asFileLines,i);
			 		resultText += getEventTags(asFileLines,i);
					classType= "interface";
				}
			}
			
			return resultText;
		}
		
		

		///<summary><![CDATA[
		///
		///</summary
		private string getOtherInfo(string[] asFileLines){
			
			
			string resultText = "";
			ArrayList publicLineIndexes = new ArrayList();
			//make index of public lines
			for(int i = 0; i<asFileLines.Length;i++ ){
				string fileLine = asFileLines[i];
				if((accessPublicAndProtectedTest.IsMatch(fileLine) || (classType=="interface" && funcTest.IsMatch(fileLine)))&& !commentPublicAndProtectedTest.IsMatch(fileLine) ) {
					if(fileLine.IndexOf("//")!=-1){
						//get everything before commenttag
						asFileLines[i] = asFileLines[i].Substring(0,asFileLines[i].IndexOf("//"));
					}
					resultText += createElementDoc(i,asFileLines);
				}
			}
			return resultText;
		}
		
		
		
		///<summary>
		///
		///</summary
		private string createElementDoc(int lineIndex,string[] asFileLines){
			
			string resultText = "";
			string fileLine = asFileLines[lineIndex];	

			string[] modifierStrings = getModifierStrings(fileLine);
			string modifierRegExpString = "";
			for(int i=0;i<modifierStrings.Length;i++){
				modifierRegExpString += "("+modifierStrings[i]+" +)?";	
			}
			
			//methods
			if(funcTest.IsMatch(fileLine) && !funcGetTest.IsMatch(fileLine)  && !funcSetTest.IsMatch(fileLine)){	
				
				resultText += "<method access=\""+getAccessString(fileLine)+"\">\r\n";
				
				resultText += "<name>"+stripElement(fileLine,@" *"+modifierRegExpString+"(("+accessString+") +)?"+modifierRegExpString+"(function +)",@"\(.*")+"</name>\r\n";
				resultText += Utils.getId();
				resultText += "<summary><![CDATA["+Utils.getSummery(asFileLines,lineIndex)+"]]></summary>\r\n";
				
				resultText += formatCodeline(fileLine);
				resultText += "<modifiers>\r\n"+getModifierXml(modifierStrings)+"</modifiers>\r\n";	
				
				try{
					//loop through
					string paramString = stripElement(fileLine,@".*(function +)(.*\()",@"\).*");
					if(paramString !=""){
					string[] allParams = paramString.Split(',');
					foreach(string param in allParams){
						resultText += "<param>\r\n";
						int colonIndex = param.IndexOf(":");
						
						if(colonIndex == -1) colonIndex = param.Length-1;
						
						
						
						string pName = param.Substring(0,colonIndex).TrimStart(' ');
						if(param.IndexOf(":")!=-1) resultText += getTypeXml(param,pName);
						
						resultText += "<name>" +pName +"</name>\r\n";
						resultText += "<summary><![CDATA["+Utils.getDescription(asFileLines,lineIndex,"@param "+pName+" ")+"]]></summary>\r\n";
						resultText += "</param>\r\n";
					}
				
					}
				}catch(Exception){
					systemSvar += "\r\nLine #"+lineIndex+": \""+ asFileLines[lineIndex]+"\"";
				}
				
				Regex returnParamTest = new Regex(@".*\) *:");
		
		
					if(returnParamTest.IsMatch(fileLine)){
						resultText += "<returns>\r\n";
						resultText += formatType(stripElement(fileLine,@".*\) *: *",@" *{? *"));
						resultText += "<summary><![CDATA["+Utils.getDescription(asFileLines,lineIndex,"@return")+Utils.getDescription(asFileLines,lineIndex,"@returns")+"]]></summary>\r\n";
						resultText += "</returns>\r\n";
					}
					
				 	resultText += Utils.getStandAloneTags(asFileLines,lineIndex);
					
					resultText += "</method>\r\n";
					
				}
		
			else if(fileLine.IndexOf("function get ") != -1){
				resultText += "<property access=\""+getAccessString(fileLine)+"\" readWrite=\"Read\">\r\n";
				
				resultText += "<name>"+stripElement(fileLine,@" *"+modifierRegExpString+"(("+accessString+") +)?"+modifierRegExpString+"(function +get +)",@"\(.*")+"</name>\r\n";
				resultText += Utils.getId();
				if(fileLine.IndexOf("):") != -1){
					string typeName =stripElement(fileLine,@".*\) *: *",@" *{.*");
					resultText += formatType(typeName);;
				}
				
				resultText += "<modifiers>\r\n"+getModifiers(fileLine)+"</modifiers>\r\n";
				resultText += "<summary><![CDATA["+Utils.getSummery(asFileLines,lineIndex)+"]]></summary>\r\n";
				resultText += formatCodeline(fileLine);
				resultText += "</property>\r\n";
			}	
			else if(fileLine.IndexOf("function set ") != -1){
				
				resultText += "<property access=\""+getAccessString(fileLine)+"\" readWrite=\"Write\">\r\n";
				
				string pName = stripElement(fileLine,@" *"+modifierRegExpString+"(("+accessString+") +)?"+modifierRegExpString+"(function +set +)",@"\(.*");
				resultText += "<name>"+pName+"</name>\r\n";	
				resultText += Utils.getId();
				string typeName = stripElement(fileLine,@".*\(.*[^\)]:",@" *\).*");
				
				resultText += formatType(typeName);
				
				resultText += "<modifiers>\r\n"+getModifiers(fileLine)+"</modifiers>\r\n";			
				resultText += "<summary><![CDATA["+Utils.getSummery(asFileLines,lineIndex)+"]]></summary>\r\n";
				resultText += formatCodeline(fileLine);
				resultText += "</property>\r\n";
			}	
			
				else if(fileLine.IndexOf("var ") != -1 || fileLine.IndexOf("const ") != -1){	
				
				resultText += "<property access=\""+getAccessString(fileLine)+"\" readWrite=\"ReadWrite\">\r\n";
				
				string propName = stripElement(fileLine,@" *"+modifierRegExpString+"(("+accessString+") +)?"+modifierRegExpString+"((const|var) +)",@" *[:| |=|;].*");
				resultText += "<name>"+propName+"</name>\r\n";
				resultText += Utils.getId();
				resultText += "<modifiers>\r\n"+getModifiers(fileLine)+"</modifiers>\r\n";	
				
				if(fileLine.IndexOf(propName+":")!=-1) resultText += getTypeXml(fileLine,propName)+"\r\n";
				
				resultText += "<summary><![CDATA["+Utils.getSummery(asFileLines,lineIndex)+"]]></summary>\r\n";
				resultText += formatCodeline(stripElement(fileLine,"",@"[{|;]"));
				if(fileLine.IndexOf("=") != -1) resultText += "<defaultValue><![CDATA["+formatCode(stripElement(fileLine,@".*= *",@";+ *"))+"]]></defaultValue>\r\n";
				
				resultText += Utils.getStandAloneTags(asFileLines,lineIndex);
				resultText += "</property>\r\n";
				
			}
			return resultText;			
		}
		
		string getAccessString(string fileLine){
			if(accessPublicTest.IsMatch(fileLine))return "public";
			else if(accessProtectedTest.IsMatch(fileLine))return "protected";
			else if(accessInternalTest.IsMatch(fileLine))return "internal";
			else if(accessPrivateTest.IsMatch(fileLine))return "private";
			else return "protected";
		}
		
		string getTypeXml(string line,string name){
			line = stripElement(line,@".*:",@" *[=|;|)|,].*");
			return formatType(line);
		}
			
		string formatType(string typeName){
			typeName = typeName.Replace("<","&lt;").Replace(">","&gt;").Replace(" ","");
			return "<type fullPath=\"\"  context=\""+currentPackages+"\"><![CDATA[" +typeName+"]]></type>\r\n";
		}
		
		string formatCodeline(string codeLine){
			return "<codeLine><![CDATA[" +formatCode(codeLine)+"]]></codeLine>\r\n";
		}
		
		string formatCode(string codeLine){				
			codeLine = codeLine.Replace("<","&lt;").Replace(">","&gt;").Replace(";","");
			
			Regex codeLineEnd = new Regex(@"[;|{|/].*");
			codeLine =  codeLineEnd.Replace(codeLine, "");
					
			codeLine = codeLine.TrimEnd(' ').TrimEnd('{');
			return codeLine;
		}
		#endregion
		
		
		#region Creates the different parts
		
		///<summary>
		///
		///</summary
		private string getModifiers(string fileLine)
		{	
			string result = "";
			for(int i=0;i<modifiers.Length;i++){
				if(fileLine.IndexOf(modifiers[i]+" ") != -1)result += "<modifier>"+modifiers[i]+"</modifier>\r\n";	
			}
			return result;
		}
		
		///<summary>
		///
		///</summary
		private string[] getModifierStrings(string fileLine)
		{	
			string result = "";
			for(int i=0;i<modifiers.Length;i++){
				if(fileLine.IndexOf(modifiers[i]+" ") != -1){
					result += modifiers[i]+",";
				}
			}
			result.TrimEnd(',');
			return result.Split(',');
		}
		
		///<summary>
		///
		///</summary
		private string getModifierXml(string[] modifierStrings)
		{	
			string result = "";
			for(int i=0;i<modifierStrings.Length;i++){
				result += "<modifier>"+modifierStrings[i]+"</modifier>\r\n";	
			}
			return result;
		}
		
		
		private string getEventTags(string[] asFileLines,int elementIndex)
		{
			string resultText = "";
				string[] tagDoc = Utils.getMultiDescription(asFileLines, elementIndex, "@event");
												
				foreach(string docString in tagDoc){
					string tempDocString = docString.TrimStart(' ');
					int delimIndex = tempDocString.IndexOf(" ");
					if(delimIndex == -1) delimIndex = tempDocString.Length;
					string eventName = tempDocString.Substring(0,delimIndex);
					string summery = "";
					if(tempDocString.Length > (delimIndex+1) ) summery =tempDocString.Substring(delimIndex+1);
					resultText += "<event>\r\n<name>"+eventName+"</name>\r\n"+Utils.getId()+"<summary><![CDATA["+summery+"]]></summary>\r\n</event>\r\n";
				}
		
			
			return resultText;
		}
		
		
		
		#endregion
		
		
		#region Generel methods
		
		
		//
//		private string[] cleanUpLines(string[] asFileLines)
//		{	
//			int curlyBracketCounter = 0;
//			bool removeTheRest = false;
//			bool multiLineComment = false;
//			bool javaDocComment = false;
//			
//			for(int i = 0; i<asFileLines.Length;i++ ){
//				asFileLines[i] = removeIndent(asFileLines[i]);
//				
//				//ignore if javadoc comments
//				int javadocCommentIndex = asFileLines[i].IndexOf(startTag);
//				if(!javaDocComment && javadocCommentIndex!=-1 && 
//				   (javadocCommentIndex<=asFileLines[i].IndexOf("/*")) && 
//				   (asFileLines[i].IndexOf("//")==-1 || javadocCommentIndex<asFileLines[i].IndexOf("//"))  ){
//					javaDocComment = true;
//				}
//				
//				int javadocCommentIndexEnd = asFileLines[i].IndexOf("*/");
//				if(javaDocComment && javadocCommentIndexEnd!=-1){
//					javaDocComment = false;
//				}
//				
//				if(!javaDocComment){
//					
//					//remove inline multiline comments
//					Regex imcPattern = new Regex(@"\/\*.*(\*\/)");
//					asFileLines[i] =  imcPattern.Replace(asFileLines[i], "");
//					
//					//remove multiline comments
//					int multiLineCommentIndexStart = asFileLines[i].IndexOf("/*");
//					if(!multiLineComment && multiLineCommentIndexStart!=-1 && 
//					   asFileLines[i].IndexOf(startTag)==-1 && 
//					   (asFileLines[i].IndexOf("//")==-1 || multiLineCommentIndexStart<asFileLines[i].IndexOf("//"))){
//						multiLineComment = true;
//					}
//					 
//					int multiLineCommentIndexEnd = asFileLines[i].IndexOf("*/");
//					if(multiLineComment && multiLineCommentIndexEnd!=-1){
//						if(multiLineCommentIndexStart==-1) multiLineCommentIndexStart=0;
//						asFileLines[i] = asFileLines[i].Substring(multiLineCommentIndexEnd+2,(asFileLines[i].Length - (multiLineCommentIndexEnd+2)));
//						multiLineComment = false;
//					} 
////					if(javaDocComment && multiLineCommentIndexEnd!=-1){
////						javaDocComment = false;
////					}
//					
//					//remove single line comments
//					int commentIndex = asFileLines[i].IndexOf("//");
//					if(commentIndex!=-1){
//						asFileLines[i] = asFileLines[i].Substring(0,commentIndex);
//					}
//					
//					//keep count on when the class is ending to avoid package with more than one class
//					//This situation occurs in some singleton implementations
//					if(!multiLineComment){
//						int bracketPos = asFileLines[i].IndexOf("{");
//						while(bracketPos != -1){
//							curlyBracketCounter++;
//							bracketPos++;
//							if(bracketPos<asFileLines[i].Length) bracketPos = asFileLines[i].IndexOf("{",bracketPos);
//							else bracketPos = -1;
//						}
//						bracketPos = asFileLines[i].IndexOf("}");
//						while(bracketPos != -1){
//							curlyBracketCounter--;
//							if(curlyBracketCounter<=1) removeTheRest = true;
//							bracketPos++;
//							if(bracketPos<asFileLines[i].Length) bracketPos = asFileLines[i].IndexOf("}",bracketPos);
//							else bracketPos = -1;
//						}
//					}
//					
//				}
//				
//				if(removeTheRest || multiLineComment){
//					asFileLines[i] = "";
//				}else{
//					asFileLines[i] = asFileLines[i].Replace("<br>","<br />");
//					asFileLines[i] = asFileLines[i].Replace("\t"," ");
//					
//					Regex emptyStartChars = new Regex(@"^[\t| ]*");
//					asFileLines[i] =  emptyStartChars.Replace(asFileLines[i], "");
//					
//					Regex lastAddSpace = new Regex(@"\*\s*@");
//					asFileLines[i] =  lastAddSpace.Replace(asFileLines[i], "*@");
//			
//					
//					//I the function uses parameter in more than one line
//					int ekstraIndex = 1;
//					while(funcTest.IsMatch(asFileLines[i]) && asFileLines[i].IndexOf(")")==-1){
//						asFileLines[i] += asFileLines[i+ekstraIndex];
//						ekstraIndex++;
//						if(ekstraIndex>20){
//							break;
//						}
//					}
//			}
//				}
//			
//			return asFileLines;
//		}
//		
		
		///<summary><![CDATA[
		///
		///</summary
		private string removeIndent(string asLine)
		{	
			char[] trimChar = {'\t',' '};
			asLine = asLine.TrimStart(trimChar);
			asLine = asLine.TrimEnd(trimChar);
						
			return asLine;
		}
		
		
		///<summary><![CDATA[
		///Removes comment characters
		///</summary
		private string removeCommentChars(string summeryText)
		{			
			char[] trimChar = {'\t',' ','/','*','\\'};
			return summeryText.TrimStart(trimChar);			
		}
		
		string stripElement(string linje,string startRegexp,string slutRegexp){
			Regex rx1 = new Regex(startRegexp);
			Regex rx2 = new Regex(slutRegexp);
			linje =  rx1.Replace(linje, "");
			linje =  rx2.Replace(linje, "");
			linje =  lastWhiteSpace.Replace(linje, "");
			
			return linje;
		}
		
//		private string getId(){
//			return "<fid>_"+(idCounter++).ToString()+"</fid>\r\n";
//		}
		#endregion
		
		
	}
}
