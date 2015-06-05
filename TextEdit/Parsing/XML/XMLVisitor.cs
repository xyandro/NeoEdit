using System.Collections.Generic;
using Antlr4.Runtime.Tree;
using NeoEdit.TextEdit.Parsing.XML.Parser;

namespace NeoEdit.TextEdit.Parsing.XML
{
	class XMLVisitor : XMLParserBaseVisitor<object>
	{
		const string COMMENT = "comment";
		const string DOCUMENT = "document";
		const string ELEMENT = "element";
		const string NAME = "name";
		const string ROOTSTR = "root";
		const string TEXT = "text";

		readonly ParserNode Root;
		ParserNode Parent { get { return stack.Peek(); } }
		readonly Stack<ParserNode> stack = new Stack<ParserNode>();
		readonly string input;
		XMLVisitor(string input)
		{
			this.input = input;
			stack.Push(Root = new ParserNode { Type = ROOTSTR, Start = 0, End = input.Length });
		}

		public static ParserNode Parse(string input, IParseTree tree)
		{
			var visitor = new XMLVisitor(input);
			visitor.Visit(tree);
			return visitor.Root;
		}

		public override object VisitAttribute(XMLParser.AttributeContext context)
		{
			int start, end;
			context.STRING().GetBounds(out start, out end);
			++start;
			--end;
			Parent.AddAttr(context.Name().GetText(input), input.Substring(start, end - start), start, end);
			return base.VisitAttribute(context);
		}

		public override object VisitChardata(XMLParser.ChardataContext context)
		{
			context.Take(c => c.TEXT()).Do(c => new ParserNode { Type = TEXT, Parent = Parent, LocationTerminalNode = c });
			return null;
		}

		public override object VisitContent(XMLParser.ContentContext context)
		{
			context.Take(c => c.COMMENT()).Do(c => new ParserNode { Type = COMMENT, Parent = Parent, LocationTerminalNode = c });
			context.Take(c => c.PI()).Do(c => new ParserNode { Type = COMMENT, Parent = Parent, LocationTerminalNode = c });
			context.Take(c => c.CDATA()).Do(c => new ParserNode { Type = COMMENT, Parent = Parent, LocationTerminalNode = c });
			return base.VisitContent(context);
		}

		public override object VisitDocument(XMLParser.DocumentContext context)
		{
			stack.Push(new ParserNode { Type = DOCUMENT, Parent = Parent, LocationParserRule = context });
			base.VisitDocument(context);
			stack.Pop();
			return null;
		}

		public override object VisitElement(XMLParser.ElementContext context)
		{
			var node = new ParserNode { Type = ELEMENT, Parent = Parent, LocationParserRule = context };
			node.AddAttr(NAME, input, context.Name()[0]);

			stack.Push(node);
			base.VisitElement(context);
			stack.Pop();
			return null;
		}

		public override object VisitMisc(XMLParser.MiscContext context)
		{
			context.Take(c => c.COMMENT()).Do(c => new ParserNode { Type = COMMENT, Parent = Parent, LocationTerminalNode = c });
			context.Take(c => c.PI()).Do(c => new ParserNode { Type = COMMENT, Parent = Parent, LocationTerminalNode = c });
			return null;
		}

		public override object VisitProlog(XMLParser.PrologContext context)
		{
			stack.Push(new ParserNode { Type = COMMENT, Parent = Parent, LocationParserRule = context });
			base.VisitProlog(context);
			stack.Pop();
			return null;
		}
	}
}
