using System;
using System.Collections.Generic;
using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using NeoEdit.Common.Parsing;
using NeoEdit.TextEdit.Content.Balanced.Parser;

namespace NeoEdit.TextEdit.Content.Balanced
{
	class BalancedVisitor : BalancedBaseVisitor<object>
	{
		public class BalancedErrorListener : IAntlrErrorListener<IToken>
		{
			public void SyntaxError(IRecognizer recognizer, IToken token, int line, int pos, string msg, RecognitionException e)
			{
				throw new Exception(String.Format("Error: Token mistmatch at position {0}", token.StartIndex));
			}
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

			var visitor = new BalancedVisitor(input);
			visitor.Visit(tree);
			return visitor.Root;
		}

		const string ROOT = "Root";
		const string ANGLES = "Angles";
		const string BRACES = "Braces";
		const string BRACKETS = "Brackets";
		const string PARENS = "Parens";

		readonly ParserNode Root;
		ParserNode Parent { get { return stack.Peek(); } }
		readonly Stack<ParserNode> stack = new Stack<ParserNode>();
		readonly string input;
		BalancedVisitor(string input)
		{
			this.input = input;
			stack.Push(Root = new ParserNode { Type = ROOT, Start = 0, End = input.Length });
		}

		ParserNode AddNode(ParserRuleContext context, string type)
		{
			stack.Push(new ParserNode { Type = type, Parent = Parent, LocationParserRule = context });
			VisitChildren(context);
			stack.Pop();
			return null;
		}

		public override object VisitAngles(BalancedParser.AnglesContext context) { return AddNode(context, ANGLES); }
		public override object VisitBraces(BalancedParser.BracesContext context) { return AddNode(context, BRACES); }
		public override object VisitBrackets(BalancedParser.BracketsContext context) { return AddNode(context, BRACKETS); }
		public override object VisitParens(BalancedParser.ParensContext context) { return AddNode(context, PARENS); }
	}
}
