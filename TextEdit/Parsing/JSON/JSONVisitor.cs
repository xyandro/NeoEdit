using System.Collections.Generic;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using NeoEdit.TextEdit.Parsing.JSON.Parser;

namespace NeoEdit.TextEdit.Parsing.JSON
{
	class JSONVisitor : JSONBaseVisitor<object>
	{
		const string PAGE = "Page";
		const string OBJECT = "Object";
		const string ARRAY = "Array";
		const string PAIR = "Pair";
		const string ID = "ID";
		const string VALUE = "Value";

		readonly ParserNode Root;
		ParserNode Parent { get { return stack.Peek(); } }
		readonly Stack<ParserNode> stack = new Stack<ParserNode>();
		readonly string input;
		JSONVisitor(string input)
		{
			this.input = input;
			stack.Push(Root = new ParserNode { Type = PAGE, Start = 0, End = input.Length });
		}

		public static ParserNode Parse(string input, IParseTree tree)
		{
			var visitor = new JSONVisitor(input);
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

		public override object VisitPair(JSONParser.PairContext context) { return AddNode(context, PAIR); }
		public override object VisitValue(JSONParser.ValueContext context) { return AddNode(context, VALUE, context.str != null); }

		public override object VisitObject(JSONParser.ObjectContext context)
		{
			Parent.Type = OBJECT;
			return base.VisitObject(context);
		}

		public override object VisitArray(JSONParser.ArrayContext context)
		{
			Parent.Type = ARRAY;
			return base.VisitArray(context);
		}

		public override object VisitPairid(JSONParser.PairidContext context)
		{
			int start, end;
			context.GetBounds(out start, out end);
			++start;
			--end;
			var str = input.Substring(start, end - start);
			Parent.AddAttr(ID, str, start, end);
			return null;
		}
	}
}
