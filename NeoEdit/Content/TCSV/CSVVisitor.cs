using Antlr4.Runtime;
using NeoEdit.Common.Parsing;
using NeoEdit.MenuContent.Content.TCSV.Parser;

namespace NeoEdit.MenuContent.Content.TCSV
{
	class CSVVisitor : CSVBaseVisitor<ParserNode>
	{
		public static ParserNode Parse(string input, bool strict)
		{
			var tree = ParserHelper.Parse<CSVLexer, CSVParser, CSVParser.RootContext>(input, parser => parser.root(), strict);
			return new CSVVisitor().Visit(tree);
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

		public override ParserNode VisitRoot(CSVParser.RootContext context)
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

		public override ParserNode VisitRow(CSVParser.RowContext context)
		{
			var node = GetNode(context, ROW, ParserNode.ParserNavigationTypeEnum.FirstChild);
			if (node.Start == node.End)
				return null;
			foreach (var cell in context.cell())
				Visit(cell).Parent = node;
			return node;
		}

		public override ParserNode VisitCell(CSVParser.CellContext context) => GetNode(context, CELL, ParserNode.ParserNavigationTypeEnum.Cell);
	}
}
