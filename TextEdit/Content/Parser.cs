using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NeoEdit.Common;
using NeoEdit.Common.Parsing;
using NeoEdit.TextEdit.Content.Balanced;
using NeoEdit.TextEdit.Content.Columns;
using NeoEdit.TextEdit.Content.CPlusPlus;
using NeoEdit.TextEdit.Content.CSharp;
using NeoEdit.TextEdit.Content.HTML;
using NeoEdit.TextEdit.Content.JSON;
using NeoEdit.TextEdit.Content.SQL;
using NeoEdit.TextEdit.Content.TCSV;
using NeoEdit.TextEdit.Content.XML;

namespace NeoEdit.TextEdit.Content
{
	static public class Parser
	{
		public enum ParserType
		{
			None,
			Balanced,
			Columns,
			CPlusPlus,
			CSharp,
			CSV,
			HTML,
			JSON,
			SQL,
			TSV,
			XML,
		}

		public static bool IsTableType(this ParserType parserType) => (parserType == ParserType.Columns) || (parserType == ParserType.TSV) || (parserType == ParserType.CSV);

		static public ParserNode Parse(string data, ParserType parserType)
		{
			switch (parserType)
			{
				case ParserType.Balanced: return BalancedVisitor.Parse(data);
				case ParserType.Columns: return ColumnsVisitor.Parse(data);
				case ParserType.CPlusPlus: return CPlusPlusVisitor.Parse(data);
				case ParserType.CSharp: return CSharpVisitor.Parse(data);
				case ParserType.CSV: return CSVVisitor.Parse(data);
				case ParserType.HTML: return HTMLVisitor.Parse(data);
				case ParserType.JSON: return JSONVisitor.Parse(data);
				case ParserType.SQL: return SQLVisitor.Parse(data);
				case ParserType.TSV: return TSVVisitor.Parse(data);
				case ParserType.XML: return XMLVisitor.Parse(data);
				default: throw new ArgumentException("Unable to parse this type");
			}
		}

		static public ParserType GetParserType(string fileName)
		{
			if (string.IsNullOrEmpty(fileName))
				return ParserType.None;
			switch (Path.GetExtension(fileName).ToLowerInvariant())
			{
				case ".nec": return ParserType.Columns;
				case ".c": case ".cpp": return ParserType.CPlusPlus;
				case ".cs": return ParserType.CSharp;
				case ".csv": return ParserType.CSV;
				case ".htm": case ".html": return ParserType.HTML;
				case ".jsn": case ".json": return ParserType.JSON;
				case ".sql": return ParserType.SQL;
				case ".tsv": return ParserType.TSV;
				case ".csproj": case ".vbproj": case ".vcxproj": case ".xml": case ".xaml": return ParserType.XML;
				default: return ParserType.None;
			}
		}

		static internal string Comment(ParserType type, TextData data, Range range)
		{
			switch (type)
			{
				case ParserType.CSharp: return CSharpVisitor.Comment(data, range);
				case ParserType.HTML:
				case ParserType.XML:
					return HTMLVisitor.Comment(data, range);
				default: throw new Exception("Cannot comment this content type");
			}
		}

		static internal string Uncomment(ParserType type, TextData data, Range range)
		{
			switch (type)
			{
				case ParserType.CSharp: return CSharpVisitor.Uncomment(data, range);
				case ParserType.HTML:
				case ParserType.XML:
					return HTMLVisitor.Uncomment(data, range);
				default: throw new Exception("Cannot comment this content type");
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

		static public List<string> GetAvailableAttrs(List<ParserNode> nodes, bool withLocation = false) => nodes.SelectMany(node => node.GetAttrTypes(withLocation)).Distinct().OrderBy().ToList();
		static public List<string> GetAvailableValues(List<ParserNode> nodes, string type) => nodes.SelectMany(node => node.GetAttrsText(type)).Distinct().OrderBy().ToList();
	}
}
