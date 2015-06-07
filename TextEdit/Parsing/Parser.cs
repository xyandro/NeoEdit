using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NeoEdit.TextEdit.Parsing.CSharp;
using NeoEdit.TextEdit.Parsing.HTML;
using NeoEdit.TextEdit.Parsing.JSON;
using NeoEdit.TextEdit.Parsing.TCSV;
using NeoEdit.TextEdit.Parsing.XML;

namespace NeoEdit.TextEdit.Parsing
{
	static public class Parser
	{
		public enum ParserType
		{
			None,
			CSharp,
			CSV,
			HTML,
			JSON,
			TSV,
			XML,
		}

		static public ParserNode Parse(string data, ParserType parserType)
		{
			switch (parserType)
			{
				case ParserType.CSharp: return CSharpEntry.Parse(data);
				case ParserType.CSV: return CSVVisitor.Parse(data);
				case ParserType.HTML: return HTMLVisitor.Parse(data);
				case ParserType.JSON: return JSONVisitor.Parse(data);
				case ParserType.TSV: return TSVVisitor.Parse(data);
				case ParserType.XML: return XMLVisitor.Parse(data);
				default: throw new ArgumentException("Unable to parse this type");
			}
		}

		static public ParserType GetParserType(string fileName)
		{
			if (String.IsNullOrEmpty(fileName))
				return ParserType.None;
			switch (Path.GetExtension(fileName).ToLowerInvariant())
			{
				case ".cs": return ParserType.CSharp;
				case ".csv": return ParserType.CSV;
				case ".htm":
				case ".html": return ParserType.HTML;
				case ".jsn":
				case ".json": return ParserType.JSON;
				case ".tsv": return ParserType.TSV;
				case ".csproj":
				case ".vbproj":
				case ".vcxproj":
				case ".xml":
				case ".xaml": return ParserType.XML;
				default: return ParserType.None;
			}
		}

		static public string Reformat(ParserNode node, string input, ParserType parserType)
		{
			switch (parserType)
			{
				case ParserType.HTML: return HTMLVisitor.Format(node, input);
				case ParserType.JSON: return JSONVisitor.Format(node, input);
				case ParserType.XML: return XMLVisitor.Format(node, input);
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
