using System;
using NeoEdit.Common;
using NeoEdit.Common.Enums;
using NeoEdit.Common.Parsing;
using NeoEdit.Editor.Content.Balanced;
using NeoEdit.Editor.Content.Columns;
using NeoEdit.Editor.Content.CSharp;
using NeoEdit.Editor.Content.ExactColumns;
using NeoEdit.Editor.Content.HTML;
using NeoEdit.Editor.Content.JSON;
using NeoEdit.Editor.Content.SQL;
using NeoEdit.Editor.Content.TCSV;
using NeoEdit.Editor.Content.XML;
using NeoEdit.Editor.Transactional;

namespace NeoEdit.Editor.Content
{
	public static class Parser
	{
		public static ParserNode Parse(string data, ParserType parserType, bool strict)
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

		internal static string Comment(ParserType type, NEText text, Range range)
		{
			switch (type)
			{
				case ParserType.CSharp: return CSharpVisitor.Comment(text, range);
				case ParserType.HTML:
				case ParserType.XML:
					return HTMLVisitor.Comment(text, range);
				default: throw new Exception("Cannot comment this content type");
			}
		}

		internal static string Uncomment(ParserType type, NEText text, Range range)
		{
			switch (type)
			{
				case ParserType.CSharp: return CSharpVisitor.Uncomment(text, range);
				case ParserType.HTML:
				case ParserType.XML:
					return HTMLVisitor.Uncomment(text, range);
				default: throw new Exception("Cannot comment this content type");
			}
		}

		public static string Reformat(ParserNode node, string input, ParserType parserType)
		{
			switch (parserType)
			{
				case ParserType.HTML: return HTMLVisitor.Format(node, input);
				case ParserType.JSON: return JSONVisitor.Format(node, input);
				case ParserType.XML: return XMLVisitor.Format(node, input);
				default: throw new Exception("Unable to reformat this type");
			}
		}
	}
}
