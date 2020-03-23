using System.Collections.Generic;
using Antlr4.Runtime;
using NeoEdit.Common.Parsing;
using NeoEdit.Program.Content.Columns.Parser;

namespace NeoEdit.Program.Content.Columns
{
	class ColumnsVisitor : ColumnsBaseVisitor<ParserNode>
	{
		public static ParserNode Parse(string input, bool strict)
		{
			var tree = ParserHelper.Parse<ColumnsLexer, ColumnsParser, ColumnsParser.RootContext>(input, parser => parser.root(), strict);
			return new ColumnsVisitor().Visit(tree);
		}

		const string ROOT = "Root";
		const string ROW = "Row";
		const string CELL = "Cell";

		ParserNode GetNode(ParserRuleContext context, string type, IEnumerable<ParserRuleContext> nodes, ParserNode.ParserNavigationTypeEnum parserNavigationType)
		{
			var node = new ParserNode { Type = type, LocationParserRule = context, ParserNavigationType = parserNavigationType };
			if (nodes != null)
				foreach (var child in nodes)
					Visit(child).Parent = node;
			return node;
		}

		public override ParserNode VisitRoot(ColumnsParser.RootContext context) => GetNode(context, ROOT, context.row(), ParserNode.ParserNavigationTypeEnum.FirstChild);
		public override ParserNode VisitRow(ColumnsParser.RowContext context) => GetNode(context, ROW, context.cell(), ParserNode.ParserNavigationTypeEnum.FirstChild);
		public override ParserNode VisitCell(ColumnsParser.CellContext context) => Visit(context.text());
		public override ParserNode VisitText(ColumnsParser.TextContext context) => GetNode(context, CELL, null, ParserNode.ParserNavigationTypeEnum.Cell);
	}
}
