﻿using System.Collections.Generic;
using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using Antlr4.Runtime.Tree;
using NeoEdit.TextEdit.Content.TCSV.Parser;

namespace NeoEdit.TextEdit.Content.TCSV
{
	class CSVVisitor : CSVBaseVisitor<object>
	{
		public static ParserNode Parse(string input)
		{
			var inputStream = new AntlrInputStream(input);
			var lexer = new CSVLexer(inputStream);
			var tokens = new CommonTokenStream(lexer);
			var parser = new CSVParser(tokens);
			parser.Interpreter.PredictionMode = PredictionMode.Sll;

			CSVParser.DocContext tree;
			try
			{
				tree = parser.doc();
			}
			catch
			{
				tokens.Reset();
				parser.Reset();
				parser.Interpreter.PredictionMode = PredictionMode.Ll;
				tree = parser.doc();
			}

			return CSVVisitor.Parse(input, tree);
		}

		const string PAGE = "Page";
		const string ROW = "Row";
		const string FIELD = "Field";

		readonly ParserNode Root;
		ParserNode Parent { get { return stack.Peek(); } }
		readonly Stack<ParserNode> stack = new Stack<ParserNode>();
		readonly string input;
		CSVVisitor(string input)
		{
			this.input = input;
			stack.Push(Root = new ParserNode { Type = PAGE, Start = 0, End = input.Length });
		}

		public static ParserNode Parse(string input, IParseTree tree)
		{
			var visitor = new CSVVisitor(input);
			visitor.Visit(tree);
			return visitor.Root;
		}

		object AddNode(ParserRuleContext context, string type, bool skipQuotes = false, bool skipEmpty = false)
		{
			int start, end;
			context.GetBounds(out start, out end);
			if ((skipEmpty) && (start == end))
				return null;
			if (skipQuotes)
			{
				++start;
				--end;
			}

			stack.Push(new ParserNode { Type = type, Parent = Parent, Start = start, End = end });
			VisitChildren(context);
			stack.Pop();
			return null;
		}

		public override object VisitRow(CSVParser.RowContext context) { return AddNode(context, ROW, skipEmpty: true); }
		public override object VisitText(CSVParser.TextContext context) { return AddNode(context, FIELD); }
		public override object VisitString(CSVParser.StringContext context) { return AddNode(context, FIELD, true); }
		public override object VisitEmpty(CSVParser.EmptyContext context) { return AddNode(context, FIELD); }
	}
}