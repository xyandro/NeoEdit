using System;
using System.Collections.Generic;
using System.Linq;
using NeoEdit.Program;
using NeoEdit.Program.Content.Balanced;
using NeoEdit.Program.Content.Columns;
using NeoEdit.Program.Content.CSharp;
using NeoEdit.Program.Content.ExactColumns;
using NeoEdit.Program.Content.HTML;
using NeoEdit.Program.Content.JSON;
using NeoEdit.Program.Content.SQL;
using NeoEdit.Program.Content.TCSV;
using NeoEdit.Program.Content.XML;
using NeoEdit.Program.Parsing;

namespace NeoEdit.Program.Content
{
	static public class Parser
	{
		static public ParserNode Parse(string data, ParserType parserType, bool strict)
		{
			switch (parserType)
			{
				case ParserType.Balanced: return BalancedVisitor.Parse(data, strict);
				case ParserType.Columns: return ColumnsVisitor.Parse(data, strict);
				case ParserType.CSharp: return CSharpVisitor.Parse(data, strict);
				case ParserType.CSV: return CSVVisitor.Parse(data, strict);
				case ParserType.ExactColumns: return ExactColumnsVisitor.Parse(data, strict);
				case ParserType.HTML: return HTMLVisitor.Parse(data, strict);
				case ParserType.JSON: return JSONVisitor.Parse(data, strict);
				case ParserType.SQL: return SQLVisitor.Parse(data, strict);
				case ParserType.TSV: return TSVVisitor.Parse(data, strict);
				case ParserType.XML: return XMLVisitor.Parse(data, strict);
				default: throw new ArgumentException("Unable to parse this type");
			}
		}

		static internal string Comment(ParserType type, TextData data, Range range)
		{
			// TODO: Remove
			switch (type)
			{
				case ParserType.CSharp: return CSharpVisitor.Comment(data, range);
				case ParserType.HTML:
				case ParserType.XML:
					return HTMLVisitor.Comment(data, range);
				default: throw new Exception("Cannot comment this content type");
			}
		}

		static internal string Comment(ParserType type, NEText text, NETextView textView, Range range)
		{
			switch (type)
			{
				case ParserType.CSharp: return CSharpVisitor.Comment(text, textView, range);
				case ParserType.HTML:
				case ParserType.XML:
					return HTMLVisitor.Comment(text, range);
				default: throw new Exception("Cannot comment this content type");
			}
		}

		static internal string Uncomment(ParserType type, TextData data, Range range)
		{
			// TODO: Remove
			switch (type)
			{
				case ParserType.CSharp: return CSharpVisitor.Uncomment(data, range);
				case ParserType.HTML:
				case ParserType.XML:
					return HTMLVisitor.Uncomment(data, range);
				default: throw new Exception("Cannot comment this content type");
			}
		}

		static internal string Uncomment(ParserType type, NEText text, NETextView textView, Range range)
		{
			switch (type)
			{
				case ParserType.CSharp: return CSharpVisitor.Uncomment(text, textView, range);
				case ParserType.HTML:
				case ParserType.XML:
					return HTMLVisitor.Uncomment(text, range);
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
