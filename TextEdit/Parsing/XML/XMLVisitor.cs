using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using NeoEdit.TextEdit.Parsing.XML.Parser;

namespace NeoEdit.TextEdit.Parsing.XML
{
	class XMLVisitor : XMLParserBaseVisitor<ParserNode>
	{
		public static ParserNode Parse(string input)
		{
			var inputStream = new AntlrInputStream(input);
			var lexer = new XMLLexer(inputStream);
			var tokens = new CommonTokenStream(lexer);
			var parser = new XMLParser(tokens);
			parser.Interpreter.PredictionMode = PredictionMode.Sll;

			XMLParser.DocumentContext tree;
			try
			{
				tree = parser.document();
			}
			catch
			{
				tokens.Reset();
				parser.Reset();
				parser.Interpreter.PredictionMode = PredictionMode.Ll;
				tree = parser.document();
			}

			var visitor = new XMLVisitor(input);
			return visitor.Visit(tree);
		}

		const string DOCUMENT = "Document";
		const string COMMENT = "Comment";
		const string MISC = "Misc";
		const string TEXT = "Text";
		const string ELEMENT = "Element";
		const string NAME = "Name";

		ParserNode Parent { get { return stack.FirstOrDefault(); } }
		readonly Stack<ParserNode> stack = new Stack<ParserNode>();
		readonly string input;
		XMLVisitor(string input)
		{
			this.input = input;
		}

		ParserNode HandleContext(ParserRuleContext context, string type)
		{
			ParserNode node = null;
			if (type != null)
			{
				int start, end;
				context.GetBounds(out start, out end);
				if (type == TEXT)
				{
					while ((start < end) && (Char.IsWhiteSpace(input[start])))
						++start;
					while ((end > start) && (Char.IsWhiteSpace(input[end - 1])))
						--end;
				}
				if (start != end)
				{
					node = new ParserNode { Type = type, Parent = Parent, Start = start, End = end };
					stack.Push(node);
				}
			}

			VisitChildren(context);
			if (node != null)
				stack.Pop();
			return node;
		}

		public override ParserNode VisitDocument(XMLParser.DocumentContext context) { return HandleContext(context, DOCUMENT); }
		public override ParserNode VisitComment(XMLParser.CommentContext context) { return HandleContext(context, COMMENT); }
		public override ParserNode VisitMisc(XMLParser.MiscContext context) { return HandleContext(context, MISC); }
		public override ParserNode VisitText(XMLParser.TextContext context) { return HandleContext(context, TEXT); }
		public override ParserNode VisitElement(XMLParser.ElementContext context) { return HandleContext(context, ELEMENT); }

		public override ParserNode VisitAttribute(XMLParser.AttributeContext context)
		{
			var start = context.value.StartIndex + 1;
			var end = context.value.StopIndex;
			Parent.AddAttr(context.name.Text, input.Substring(start, end - start), start, end);
			return base.VisitAttribute(context);
		}

		public override ParserNode VisitTagname(XMLParser.TagnameContext context)
		{
			Parent.AddAttr(NAME, input, context);
			return base.VisitTagname(context);
		}
	}
}
