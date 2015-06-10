using System.Collections.Generic;
using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using Antlr4.Runtime.Tree;
using NeoEdit.TextEdit.Content.TCSV.Parser;

namespace NeoEdit.TextEdit.Content.TCSV
{
	class TSVVisitor : TSVBaseVisitor<object>
	{
		public static ParserNode Parse(string input)
		{
			var inputStream = new AntlrInputStream(input);
			var lexer = new TSVLexer(inputStream);
			var tokens = new CommonTokenStream(lexer);
			var parser = new TSVParser(tokens);
			parser.Interpreter.PredictionMode = PredictionMode.Sll;

			TSVParser.DocContext tree;
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

			return TSVVisitor.Parse(input, tree);
		}

		const string PAGE = "Page";
		const string ROW = "Row";
		const string FIELD = "Field";

		readonly ParserNode Root;
		ParserNode Parent { get { return stack.Peek(); } }
		readonly Stack<ParserNode> stack = new Stack<ParserNode>();
		readonly string input;
		TSVVisitor(string input)
		{
			this.input = input;
			stack.Push(Root = new ParserNode { Type = PAGE, Start = 0, End = input.Length });
		}

		public static ParserNode Parse(string input, IParseTree tree)
		{
			var visitor = new TSVVisitor(input);
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

		public override object VisitRow(TSVParser.RowContext context) { return AddNode(context, ROW, skipEmpty: true); }
		public override object VisitText(TSVParser.TextContext context) { return AddNode(context, FIELD); }
		public override object VisitString(TSVParser.StringContext context) { return AddNode(context, FIELD, true); }
		public override object VisitEmpty(TSVParser.EmptyContext context) { return AddNode(context, FIELD); }
	}
}
