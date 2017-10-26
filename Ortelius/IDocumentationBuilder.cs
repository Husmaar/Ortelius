/*
 * Created by SharpDevelop.
 * User: marten
 * Date: 09-08-2011
 * Time: 09:26
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Xml;

namespace dk.marten.ortelius
{
	/// <summary>
	/// Description of IDocumentationBuilder.
	/// </summary>
	public interface IDocumentationBuilder
	{
		
		XmlNodeList AddFile(string[] asFileLines,DateTime modifiedTime, string filename);
		
		string SystemSvar{
		  get;
		  set;
		}
	}
}
