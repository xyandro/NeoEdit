using System.Collections.Generic;
using Antlr4.Runtime;
using NeoEdit.Common.Parsing;
using NeoEdit.Editor.Content.ExactColumns.Parser;

namespace NeoEdit.Editor.Content.ExactColumns
{
	class ExactColumnsVisitor : ExactColumnsBaseVisitor<ParserNode>
	{
		public static ParserNode Parse(string input, bool strict)
		{
			var tree = ParserHelper.Parse<ExactColumnsLexer, ExactColumnsParser, ExactColumnsParser.RootContext>(input, parser => parser.root(), strict);
			return new ExactColumnsVisitor().Visit(tree);
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

		public override ParserNode VisitRoot(ExactColumnsParser.RootContext context) => GetNode(context, ROOT, context.row(), ParserNode.ParserNavigationTypeEnum.FirstChild);
		public override ParserNode VisitRow(ExactColumnsParser.RowContext context) => GetNode(context, ROW, context.cell(), ParserNode.ParserNavigationTypeEnum.FirstChild);
		public override ParserNode VisitCell(ExactColumnsParser.CellContext context) => Visit(context.text());
		public override ParserNode VisitText(ExactColumnsParser.TextContext context) => GetNode(context, CELL, null, ParserNode.ParserNavigationTypeEnum.Cell);
	}
}
