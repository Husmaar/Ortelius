/*
 * Created by SharpDevelop.
 * User: moe
 * Date: 15-07-2011
 * Time: 10:23
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections;
using System.Text.RegularExpressions;

namespace dk.marten.ortelius
{
	/// <summary>
	/// Description of Utils.
	/// </summary>
	public class Utils
	{
		private static int idCounter = 0;
		
		private static string delimeter = "*";
		private static string startTag = "/**";
		
		private static Regex lastWhiteSpace = new Regex(@"[\t| ]*$");
		private static Regex funcTest = new Regex(@"^[^\*/]*function +");
		//
		public static string[] cleanUpLines(string[] asFileLines, bool singleClass)
		{	
			int curlyBracketCounter = 0;
			bool removeTheRest = false;
			bool multiLineComment = false;
			bool javaDocComment = false;
			
			for(int i = 0; i<asFileLines.Length;i++ ){
				asFileLines[i] = removeIndent(asFileLines[i]);
				
				//ignore if javadoc comments
				int javadocCommentIndex = asFileLines[i].IndexOf(startTag);
				if(!javaDocComment && javadocCommentIndex!=-1 && 
				   (javadocCommentIndex<=asFileLines[i].IndexOf("/*")) && 
				   (asFileLines[i].IndexOf("//")==-1 || javadocCommentIndex<asFileLines[i].IndexOf("//"))  ){
					javaDocComment = true;
				}
				
				int javadocCommentIndexEnd = asFileLines[i].IndexOf("*/");
				if(javaDocComment && javadocCommentIndexEnd!=-1){
					javaDocComment = false;
				}
				
				if(!javaDocComment){
					
					//remove inline multiline comments
					Regex imcPattern = new Regex(@"\/\*.*(\*\/)");
					asFileLines[i] =  imcPattern.Replace(asFileLines[i], "");
					
					//remove multiline comments
					int multiLineCommentIndexStart = asFileLines[i].IndexOf("/*");
					if(!multiLineComment && multiLineCommentIndexStart!=-1 && 
					   asFileLines[i].IndexOf(startTag)==-1 && 
					   (asFileLines[i].IndexOf("//")==-1 || multiLineCommentIndexStart<asFileLines[i].IndexOf("//"))){
						multiLineComment = true;
					}
					 
					int multiLineCommentIndexEnd = asFileLines[i].IndexOf("*/");
					if(multiLineComment && multiLineCommentIndexEnd!=-1){
						if(multiLineCommentIndexStart==-1) multiLineCommentIndexStart=0;
						asFileLines[i] = asFileLines[i].Substring(multiLineCommentIndexEnd+2,(asFileLines[i].Length - (multiLineCommentIndexEnd+2)));
						multiLineComment = false;
					} 
//					if(javaDocComment && multiLineCommentIndexEnd!=-1){
//						javaDocComment = false;
//					}
					
					//remove single line comments
					int commentIndex = asFileLines[i].IndexOf("//");
					if(commentIndex!=-1){
						asFileLines[i] = asFileLines[i].Substring(0,commentIndex);
					}
					
					//keep count on when the class is ending to avoid package with more than one class
					//This situation occurs in some singleton implementations
					if(!multiLineComment){
						int bracketPos = asFileLines[i].IndexOf("{");
						while(bracketPos != -1){
							curlyBracketCounter++;
							bracketPos++;
							if(bracketPos<asFileLines[i].Length) bracketPos = asFileLines[i].IndexOf("{",bracketPos);
							else bracketPos = -1;
						}
						bracketPos = asFileLines[i].IndexOf("}");
						while(bracketPos != -1){
							curlyBracketCounter--;
							if(curlyBracketCounter<=1 && singleClass) removeTheRest = true;
							bracketPos++;
							if(bracketPos<asFileLines[i].Length) bracketPos = asFileLines[i].IndexOf("}",bracketPos);
							else bracketPos = -1;
						}
					}
					
				}
				
				if(removeTheRest || multiLineComment){
					asFileLines[i] = "";
				}else{
					asFileLines[i] = asFileLines[i].Replace("<br>","<br />");
					asFileLines[i] = asFileLines[i].Replace("\t"," ");
					
					Regex emptyStartChars = new Regex(@"^[\t| ]*");
					asFileLines[i] =  emptyStartChars.Replace(asFileLines[i], "");
					
					Regex lastAddSpace = new Regex(@"\*\s*@");
					asFileLines[i] =  lastAddSpace.Replace(asFileLines[i], "*@");
			
					
					//I the function uses parameter in more than one line
					int ekstraIndex = 1;
					while(funcTest.IsMatch(asFileLines[i]) && asFileLines[i].IndexOf(")")==-1){
						asFileLines[i] += asFileLines[i+ekstraIndex];
						ekstraIndex++;
						if(ekstraIndex>20){
							break;
						}
					}
			}
				}
			
			return asFileLines;
		}
		
		
		///<summary><![CDATA[
		///
		///</summary
		public static string removeIndent(string asLine)
		{	
			char[] trimChar = {'\t',' '};
			asLine = asLine.TrimStart(trimChar);
			asLine = asLine.TrimEnd(trimChar);
						
			return asLine;
		}
		
		
		///<summary><![CDATA[
		///Removes comment characters
		///</summary
		public static string removeCommentChars(string summeryText)
		{			
			char[] trimChar = {'\t',' ','/','*','\\'};
			return summeryText.TrimStart(trimChar);			
		}
		
		public static string stripElement(string linje,string startRegexp,string slutRegexp){
			Regex rx1 = new Regex(startRegexp);
			Regex rx2 = new Regex(slutRegexp);
			linje =  rx1.Replace(linje, "");
			linje =  rx2.Replace(linje, "");
			linje =  lastWhiteSpace.Replace(linje, "");
			
			return linje;
		}
		
		public static string getId(){
			return "<fid>_"+(idCounter++).ToString()+"</fid>\r\n";
		}
		
		///<summary>
		///Getting the clas summary - getSummery, getDescription & getMultiLineDescription looks very alike
		///</summary>
		public static string getSummery(string[] asFileLines,int elementIndex)
		{	
			
			char[] trimChar = {'\n','\r','\t',' '};
			string resultText = "";
			int decIndex = elementIndex-1;
			int tagIndex = elementIndex;
			
			//skip empty line
			while(decIndex>0 && asFileLines[decIndex].TrimEnd(trimChar)=="" ){				
				decIndex --;
			}
			//find the first line of the documentation
			while(decIndex>=0 && (asFileLines[decIndex].IndexOf(delimeter) == 0 || asFileLines[decIndex].IndexOf(startTag) == 0)){
				if(asFileLines[decIndex].IndexOf(startTag)!= -1){
					tagIndex = decIndex+1;
					break;
				}
				decIndex --;
			}
			
			while(tagIndex < elementIndex){
				
				if(asFileLines[tagIndex].IndexOf("*@")==0 || asFileLines[tagIndex].IndexOf("*/")!=-1 ) break;
				else {
				string text = removeCommentChars(asFileLines[tagIndex]);
				if(text != "") resultText +=  text +"<br/>";
				}
				tagIndex ++;
			}
			
			resultText = resultText.TrimEnd('\r');
			resultText = resultText.TrimEnd('\n');
			resultText = resultText.TrimEnd('\r');
			return resultText;
		}
		
		/// <summary>
		/// Check if there is af tag with the given name in the javadoc block
		/// </summary>
		/// <param name="asFileLines">All the code lines</param>
		/// <param name="elementIndex">The index after the javadoc section</param>
		/// <param name="tag">The tag to look for</param>
		/// <returns>True if the tag exists</returns>
		public static bool tagExists(string[] asFileLines,int elementIndex,string tag){
			
			if(elementIndex<=0) return false;
			
			int index = elementIndex-1;
			int tagIndex = elementIndex;			
			char[] trimChar = {'\n','\r'};
			
			//skip empty line
			while(index>0 && asFileLines[index].TrimEnd(trimChar)=="" ){				
				index --;
			}
			//find line where doc starts
			while(index>0 && asFileLines[index].IndexOf(delimeter) == 0 ){
				index --;
			}
			
			try{
				while(index <= elementIndex){
					string asLine = removeCommentChars(asFileLines[index]);
					if(asLine.IndexOf("@"+tag)== 0){
						return true;
					} 
					index ++;
				}
			
			}catch(Exception){
			}
			return false;
			
		}
		
		public static string getOneLineMultiDescription(string[] asFileLines,int elementIndex,string tag){
			string[] lines = getMultiDescription( asFileLines, elementIndex, tag);
			string result = "";
			foreach(string docString in lines){
				result += docString;
			}
			return result;
		}
		
		///<summary>
		///Gets the description of more than one occurence of a tag - getSummery, getDescription & getMultiDescription looks very alike
		///</summary>
		public static string[] getMultiDescription(string[] asFileLines,int elementIndex,string tag){
			
			if(elementIndex<=0) return new string[]{};
			string resultText="";
			ArrayList allResults = new ArrayList();
			char[] trimChar = {'\n','\r'};
			
			int index = elementIndex-1;
			int tagIndex = elementIndex;
			
			//skip empty line
			while(index>0 && asFileLines[index].TrimEnd(trimChar)=="" ){				
				index --;
			}
			//find line where doc starts
			while(index>0 && asFileLines[index].IndexOf(delimeter) == 0 ){
				index --;
			}
			
			try{
			bool recordFlag = false;
			while(index <= elementIndex){
				string asLine = removeCommentChars(asFileLines[index]);
				if((asFileLines[index].IndexOf("*/")== 0 ||  asLine.IndexOf("@")== 0 || index == elementIndex) && recordFlag){
					//stop recording
					resultText = resultText.TrimEnd(trimChar);
					allResults.Add(resultText);
					recordFlag = false;
				}
				if(asLine.IndexOf("@"+tag)== 0){
					//start recording
					string text = removeCommentChars(asFileLines[index]);
					
					text = text.Substring(tag.Length+1);
					
					resultText =  text.TrimStart(' ') +"<br/>";
					recordFlag = true;
				} else if(recordFlag){					
					resultText +=asLine+"<br/>";					
				}			
				index ++;
			}
			
			}catch(Exception){
//				SystemSvar +=  "\nLine #"+tagIndex+" \""+asFileLines[tagIndex]+"\"";
			}
			return (string[])allResults.ToArray(typeof(string));
		}
		
		///<summary>
		///getSummery, getDescription & getMultiDescription looks very alike
		///</summary>
		public static string getDescription(string[] asFileLines,int elementIndex,string tag)
		{
			
			char[] trimChar = {'\n','\r'};
			string resultText = "";
			int decIndex = elementIndex-1;
			int tagIndex = elementIndex;
			try{
						//skip empty line
			while(decIndex>0 && asFileLines[decIndex].TrimEnd(trimChar)=="" ){				
				decIndex --;
			}
				while(decIndex>=0 && (asFileLines[decIndex].IndexOf(delimeter) == 0 || asFileLines[decIndex]=="")){
				if(asFileLines[decIndex].IndexOf(tag)!= -1){
					tagIndex = decIndex;
					break;
				}
				decIndex --;
			}
			bool firstFlag = true;
			while(tagIndex < elementIndex){
				
				if(!firstFlag && (asFileLines[tagIndex].IndexOf("*@")==0 || asFileLines[tagIndex].IndexOf("*/")!=-1 )) break;
				else {
				string text = removeCommentChars(asFileLines[tagIndex]);
				if(text != ""){
					
					
					
				if(firstFlag) text = text.Substring(tag.Length);
					 resultText +=  text.TrimStart(' ') +"<br/>";
					firstFlag = false;
				}
				}
				tagIndex ++;
			}
			
			resultText = resultText.TrimEnd('\n');
			resultText = resultText.TrimEnd('\r');
			resultText = resultText.TrimEnd('\n');
			}catch(Exception){
				//SystemSvar += "\nLine #"+tagIndex+" \""+asFileLines[tagIndex]+"\"";
				
			}
			
			return resultText;
		}		
		
		/// <summary>
		/// Gets a number of standard tags
		/// </summary>
		/// <param name="asFileLines"></param>
		/// <param name="elementIndex"></param>
		/// <returns></returns>
		public static string getStandAloneTags(string[] asFileLines,int elementIndex)
		{
			string[] tags = {"see","version","author","todo","langversion","keyword","playerversion","throws","exception","deprecated","sends","example","since","dependency","uses","using","readOnly","writeOnce","default","this"};
			string resultText = "";
			foreach(string tag in tags){
				string tagName = tag;
				if(tag=="dependency"|| tag=="uses" || tag=="using") tagName = "uses"; 
				string[] tagDoc = getMultiDescription(asFileLines, elementIndex, tag);
				foreach(string docString in tagDoc){
					resultText += "<"+tagName+"><![CDATA["+docString+"]]></"+tagName+">\r\n";
				}
			}
			
			return resultText;
		}
		
	}
}
