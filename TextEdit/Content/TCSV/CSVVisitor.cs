using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using NeoEdit.Common.Parsing;
using NeoEdit.TextEdit.Content.TCSV.Parser;

namespace NeoEdit.TextEdit.Content.TCSV
{
	class CSVVisitor : CSVBaseVisitor<ParserNode>
	{
		public static ParserNode Parse(string input)
		{
			var inputStream = new AntlrInputStream(input);
			var lexer = new CSVLexer(inputStream);
			var tokens = new CommonTokenStream(lexer);
			var parser = new CSVParser(tokens);
			parser.Interpreter.PredictionMode = PredictionMode.Sll;

			CSVParser.DocContext tree;
			try
			{
				tree = parser.doc();
			}
			catch
			{
				tokens.Reset();
				parser.Reset();
				parser.Interpreter.PredictionMode = PredictionMode.Ll;
				tree = parser.doc();
			}

			return new CSVVisitor().Visit(tree);
		}

		const string PAGE = "Page";
		const string ROW = "Row";
		const string FIELD = "Field";

		ParserNode GetNode(ParserRuleContext context, string type)
		{
			int start, end;
			context.GetBounds(out start, out end);
			return new ParserNode { Type = type, Start = start, End = end };
		}

		public override ParserNode VisitDoc(CSVParser.DocContext context)
		{
			var node = GetNode(context, PAGE);
			foreach (var row in context.row())
			{
				var rowNode = Visit(row);
				if (rowNode == null)
					continue;
				rowNode.Parent = node;
			}
			return node;
		}

		public override ParserNode VisitRow(CSVParser.RowContext context)
		{
			var node = GetNode(context, ROW);
			if (node.Start == node.End)
				return null;
			foreach (var field in context.field())
				Visit(field).Parent = node;
			return node;
		}

		public override ParserNode VisitField(CSVParser.FieldContext context) => GetNode(context, FIELD);
	}
}
