using System;
using System.Collections.Generic;
using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using NeoEdit.Common.Parsing;
using NeoEdit.TextEdit.Content.Balanced.Parser;

namespace NeoEdit.TextEdit.Content.Balanced
{
	class BalancedVisitor : BalancedBaseVisitor<ParserNode>
	{
		public class BalancedErrorListener : IAntlrErrorListener<IToken>
		{
			public void SyntaxError(IRecognizer recognizer, IToken token, int line, int pos, string msg, RecognitionException e) { throw new Exception($"Error: Token mistmatch at position {token.StartIndex}"); }
		}

		public static ParserNode Parse(string input)
		{
			var inputStream = new AntlrInputStream(input);
			var lexer = new BalancedLexer(inputStream);
			var tokens = new CommonTokenStream(lexer);
			var parser = new BalancedParser(tokens);
			parser.RemoveErrorListeners();
			parser.AddErrorListener(new BalancedErrorListener());
			parser.Interpreter.PredictionMode = PredictionMode.Sll;

			BalancedParser.BalancedContext tree;
			try
			{
				tree = parser.balanced();
			}
			catch
			{
				tokens.Reset();
				parser.Reset();
				parser.Interpreter.PredictionMode = PredictionMode.Ll;
				tree = parser.balanced();
			}

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
