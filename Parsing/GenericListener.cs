﻿using System.Collections.Generic;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;

namespace NeoEdit
{
	namespace Parsing
	{
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
				var node = new ParserNode { Type = ctx.GetType().ToString(), Parent = stack.Peek() };
				node.AddAttribute("Text", input, ctx);
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

			internal static void SaveTree(string data, IParseTree tree, string outputFile)
			{
				var listener = new GenericListener(data);
				new ParseTreeWalker().Walk(listener, tree);
				listener.Root.Save(outputFile);
			}
		}
	}
}