using Antlr4.Runtime;
using NeoEdit.Common.Parsing;
using NeoEdit.TextEdit.Content.TCSV.Parser;

namespace NeoEdit.TextEdit.Content.TCSV
{
	class TSVVisitor : TSVBaseVisitor<ParserNode>
	{
		public static ParserNode Parse(string input)
		{
			var tree = ParserHelper.Parse<TSVLexer, TSVParser, TSVParser.DocContext>(input, parser => parser.doc());
			return new TSVVisitor().Visit(tree);
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

		public override ParserNode VisitDoc(TSVParser.DocContext context)
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

		public override ParserNode VisitRow(TSVParser.RowContext context)
		{
			var node = GetNode(context, ROW);
			if (node.Start == node.End)
				return null;
			foreach (var field in context.field())
				Visit(field).Parent = node;
			return node;
		}

		public override ParserNode VisitField(TSVParser.FieldContext context) => GetNode(context, FIELD);
	}
}
