using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NeoEdit.TextEdit.Parsing.CSharp;
using NeoEdit.TextEdit.Parsing.HTML;
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
			TSV,
			XML,
		}

		static public ParserNode Parse(string data, ParserType parserType)
		{
			switch (parserType)
			{
				case ParserType.CSharp: return CSharpEntry.Parse(data);
				case ParserType.CSV: return TCSVEntry.ParseCSV(data);
				case ParserType.HTML: return new HTMLEntry(data).Parse();
				case ParserType.TSV: return TCSVEntry.ParseTSV(data);
				case ParserType.XML: return XMLEntry.Parse(data);
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
				case ".tsv": return ParserType.TSV;
				case ".csproj":
				case ".xml":
				case ".xaml": return ParserType.XML;
				default: return ParserType.None;
			}
		}

		static public string Reformat(ParserNode node, string data, ParserType parserType)
		{
			switch (parserType)
			{
				case ParserType.HTML: return HTMLEntry.FormatHTML(node, data);
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
