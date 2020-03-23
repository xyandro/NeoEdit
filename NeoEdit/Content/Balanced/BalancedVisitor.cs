using System.Collections.Generic;
using Antlr4.Runtime;
using NeoEdit.Common.Parsing;
using NeoEdit.Program.Content.Balanced.Parser;

namespace NeoEdit.Program.Content.Balanced
{
	class BalancedVisitor : BalancedBaseVisitor<ParserNode>
	{
		public static ParserNode Parse(string input, bool strict)
		{
			var tree = ParserHelper.Parse<BalancedLexer, BalancedParser, BalancedParser.BalancedContext>(input, parser => parser.balanced(), strict);
			return new BalancedVisitor().Visit(tree);
		}

		const string ROOT = "Root";
		const string ANGLES = "Angles";
		const string BRACES = "Braces";
		const string BRACKETS = "Brackets";
		const string PARENS = "Parens";

		ParserNode GetNode(ParserRuleContext context, string type, IEnumerable<ParserRuleContext> nodes)
		{
			var node = new ParserNode { Type = type, LocationParserRule = context };
			if (nodes != null)
				foreach (var child in nodes)
					Visit(child).Parent = node;
			return node;
		}

		public override ParserNode VisitBalanced(BalancedParser.BalancedContext context) => GetNode(context, ROOT, context.data());
		public override ParserNode VisitAngles(BalancedParser.AnglesContext context) => GetNode(context, ANGLES, context.data());
		public override ParserNode VisitBraces(BalancedParser.BracesContext context) => GetNode(context, BRACES, context.data());
		public override ParserNode VisitBrackets(BalancedParser.BracketsContext context) => GetNode(context, BRACKETS, context.data());
		public override ParserNode VisitParens(BalancedParser.ParensContext context) => GetNode(context, PARENS, context.data());
	}
}
