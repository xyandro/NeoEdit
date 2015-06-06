using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using Antlr4.Runtime.Tree;

namespace NeoEdit.TextEdit.Parsing
{

	static class ParserHelper
	{
		public class CaseInsensitiveInputStream : AntlrInputStream
		{
			public CaseInsensitiveInputStream(string input) : base(input) { }

			public override int La(int i)
			{
				var value = base.La(i);
				if ((value >= Char.MinValue) && (value <= Char.MaxValue))
					value = Char.ToLower((char)value);
				return value;
			}
		}

		public class ErrorListener<T> : IAntlrErrorListener<T>
		{
			bool found = false;
			public void SyntaxError(IRecognizer recognizer, T offendingSymbol, int line, int pos, string msg, RecognitionException e)
			{
				int start = 0, end = 0;
				var token = offendingSymbol as IToken;
				if (token != null)
				{
					start = token.StartIndex;
					end = token.StopIndex + 1;
				}

				if (!found)
				{
					if (Debugger.IsAttached)
						Debugger.Break();
					found = true;
				}
			}
		}

		class GenericListener : IParseTreeListener
		{
			Stack<ParserNode> stack = new Stack<ParserNode>();

			public ParserNode Root { get { return stack.Peek(); } }

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

			public void ExitEveryRule(ParserRuleContext ctx)
			{
				stack.Pop();
			}

			public void VisitErrorNode(IErrorNode node)
			{
			}

			public void VisitTerminal(ITerminalNode node)
			{
			}
		}

		private static string ToLiteral(string input)
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

		public static void Save(Lexer lexer, string fileName)
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
				tokenText.Add(String.Format("{0} {1:00000000}-{2:00000000} ({3:000}) {4} : {5}\n", modes[mode], token.StartIndex, token.StopIndex + 1, token.Type, names[token.Type], ToLiteral(token.Text)));
			}
			File.WriteAllText(fileName, String.Join("", tokenText));
			lexer.Reset();
		}

		public static void Save(IParseTree tree, string input, string fileName)
		{
			var listener = new GenericListener(input);
			new ParseTreeWalker().Walk(listener, tree);
			listener.Root.Save(fileName);
		}

		public static void AddErrorListener<Symbol, ATNInterpreter>(Recognizer<Symbol, ATNInterpreter> item) where ATNInterpreter : ATNSimulator
		{
			item.RemoveErrorListeners();
			item.AddErrorListener(new ErrorListener<Symbol>());
		}
	}
}
