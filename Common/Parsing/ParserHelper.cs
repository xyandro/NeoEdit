using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using Antlr4.Runtime.Tree;

namespace NeoEdit.Common.Parsing
{
	public static class ParserHelper
	{
		class ErrorListener<T> : IAntlrErrorListener<T>
		{
			public void SyntaxError(IRecognizer recognizer, T offendingSymbol, int line, int pos, string msg, RecognitionException e)
			{
				throw new Exception($"Failed to parse at line {line} pos {pos}: {msg}");
			}
		}

		class GenericListener : IParseTreeListener
		{
			Stack<ParserNode> stack = new Stack<ParserNode>();

			public ParserNode Root => stack.Peek();

			readonly string input;
			public GenericListener(string input)
			{
				stack.Push(new ParserNode { Type = "Root" });
				this.input = input;
			}

			public void EnterEveryRule(ParserRuleContext ctx)
			{
				var node = new ParserNode { Type = ctx.GetType().ToString(), Parent = stack.Peek(), LocationParserRule = ctx };
				node.AddAttr("Text", input, ctx);
				stack.Push(node);
			}

			public void ExitEveryRule(ParserRuleContext ctx) => stack.Pop();
			public void VisitErrorNode(IErrorNode node) { }
			public void VisitTerminal(ITerminalNode node) { }
		}

		public static TreeT Parse<LexerT, ParserT, TreeT>(string input, Func<ParserT, TreeT> parse, bool strict = false, bool caseSensitive = true, string debugPath = null) where LexerT : Lexer where ParserT : Parser where TreeT : IParseTree
		{
			var inputStream = caseSensitive ? new AntlrInputStream(input) : new CaseInsensitiveInputStream(input);
			var lexer = (Lexer)Activator.CreateInstance(typeof(LexerT), new[] { inputStream });
			if (strict)
			{
				lexer.RemoveErrorListeners();
				lexer.AddErrorListener(new ErrorListener<int>());
			}
			if (debugPath != null)
				Save(lexer, Path.Combine(debugPath, "Lexer.txt"));

			var tokens = new CommonTokenStream(lexer);
			var parser = (ParserT)Activator.CreateInstance(typeof(ParserT), new[] { tokens });
			if (strict)
			{
				parser.RemoveErrorListeners();
				parser.AddErrorListener(new ErrorListener<IToken>());
			}
			parser.Interpreter.PredictionMode = PredictionMode.Sll;

			TreeT tree;
			try
			{
				tree = parse(parser);
			}
			catch (RecognitionException)
			{
				tokens.Reset();
				parser.Reset();
				parser.Interpreter.PredictionMode = PredictionMode.Ll;
				tree = parse(parser);
			}
			if (debugPath != null)
				Save(tree, input, Path.Combine(debugPath, "Parser.txt"));
			return tree;
		}

		static string ToLiteral(string input)
		{
			using (var writer = new StringWriter())
			{
				using (var provider = CodeDomProvider.CreateProvider("CSharp"))
				{
					provider.GenerateCodeFromExpression(new CodePrimitiveExpression(input), writer, null);
					return writer.ToString().Replace("\r", "\\r").Replace("\n", "\\n");
				}
			}
		}

		static void Save(Lexer lexer, string fileName)
		{
			var names = (lexer.GetType().GetField("_SymbolicNames", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null) as string[]).Select(str => str ?? "").ToList();
			var maxNameLen = names.Max(str => str.Length);
			names = names.Select(str => str.PadRight(maxNameLen)).ToList();
			Func<int> getMode = () => (int)lexer.GetType().BaseType.GetField("_mode", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(lexer);
			var tokenText = new List<string>();
			var modes = lexer.ModeNames.ToList();
			var maxModeLen = modes.Max(str => str.Length);
			modes = modes.Select(str => str.PadRight(maxModeLen)).ToList();
			while (true)
			{
				var mode = getMode();
				var token = lexer.NextToken();
				if (token.Type == Lexer.Eof)
					break;
				tokenText.Add($"{modes[mode]} {token.StartIndex:00000000}-{token.StopIndex + 1:00000000} ({token.Type:000}) {names[token.Type]} : {ToLiteral(token.Text)}\n");
			}
			File.WriteAllText(fileName, string.Join("", tokenText));
			lexer.Reset();
		}

		static void Save(IParseTree tree, string input, string fileName)
		{
			var listener = new GenericListener(input);
			new ParseTreeWalker().Walk(listener, tree);
			listener.Root.Save(fileName);
		}
	}
}
