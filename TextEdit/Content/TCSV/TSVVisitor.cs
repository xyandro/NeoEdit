using Antlr4.Runtime;
using NeoEdit.TextEdit.Parsing;
using NeoEdit.TextEdit.Content.TCSV.Parser;

namespace NeoEdit.TextEdit.Content.TCSV
{
	class TSVVisitor : TSVBaseVisitor<ParserNode>
	{
		public static ParserNode Parse(string input, bool strict)
		{
			var tree = ParserHelper.Parse<TSVLexer, TSVParser, TSVParser.RootContext>(input, parser => parser.root(), strict);
			return new TSVVisitor().Visit(tree);
		}

		const string ROOT = "Root";
		const string ROW = "Row";
		const string CELL = "Cell";

		ParserNode GetNode(ParserRuleContext context, string type, ParserNode.ParserNavigationTypeEnum parserNavigationType)
		{
			int start, end;
			context.GetBounds(out start, out end);
			return new ParserNode { Type = type, Start = start, End = end, ParserNavigationType = parserNavigationType };
		}

		public override ParserNode VisitRoot(TSVParser.RootContext context)
		{
			var node = GetNode(context, ROOT, ParserNode.ParserNavigationTypeEnum.FirstChild);
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
			var node = GetNode(context, ROW, ParserNode.ParserNavigationTypeEnum.FirstChild);
			if (node.Start == node.End)
				return null;
			foreach (var cell in context.cell())
				Visit(cell).Parent = node;
			return node;
		}

		public override ParserNode VisitCell(TSVParser.CellContext context) => GetNode(context, CELL, ParserNode.ParserNavigationTypeEnum.Cell);
	}
}
