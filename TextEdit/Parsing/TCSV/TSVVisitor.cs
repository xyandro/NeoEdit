using System.Collections.Generic;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using NeoEdit.TextEdit.Parsing.TCSV.Parser;

namespace NeoEdit.TextEdit.Parsing.TCSV
{
	class TSVVisitor : TSVBaseVisitor<object>
	{
		const string PAGE = "Page";
		const string LINE = "Line";
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

		object AddNode(ParserRuleContext context, string type, bool skipQuotes = false)
		{
			int start, end;
			context.GetBounds(out start, out end);
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

		public override object VisitFields(TSVParser.FieldsContext context) { return AddNode(context, LINE); }
		public override object VisitText(TSVParser.TextContext context) { return AddNode(context, LINE); }
		public override object VisitString(TSVParser.StringContext context) { return AddNode(context, LINE, true); }
		public override object VisitEmpty(TSVParser.EmptyContext context) { return AddNode(context, LINE); }
	}
}
