using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NeoEdit.Parsing
{
	static public class Parser
	{
		public enum ParserType
		{
			None,
			CSharp,
			HTML,
		}

		static public ParserNode Parse(string data, ParserType parserType)
		{
			switch (parserType)
			{
				case ParserType.CSharp: return CSharp.Parse(data);
				case ParserType.HTML: return new HTML(data).Parse();
				default: throw new ArgumentException("Invalid parser value");
			}
		}

		static public ParserType GetParserType(string fileName)
		{
			if (String.IsNullOrEmpty(fileName))
				return ParserType.None;
			switch (Path.GetExtension(fileName).ToLowerInvariant())
			{
				case ".htm":
				case ".html": return ParserType.HTML;
				case ".cs": return ParserType.CSharp;
				default: return ParserType.None;
			}
		}

		static public string Reformat(ParserNode node, string data, ParserType parserType)
		{
			switch (parserType)
			{
				case ParserType.HTML: return HTML.FormatHTML(node, data);
				default: throw new Exception("Unable to reformat this type");
			}
		}

		static public List<string> GetAvailableAttrs(List<ParserNode> nodes)
		{
			return nodes.SelectMany(node => node.GetAttrTypes()).Distinct().OrderBy(str => str).ToList();
		}

		static public List<string> GetAvailableValues(List<ParserNode> nodes, string type)
		{
			return nodes.SelectMany(node => node.GetAttrsText(type)).Distinct().OrderBy(str => str).ToList();
		}
	}
}
