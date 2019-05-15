using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using NeoEdit.Parsing;
using NeoEdit.Content.XML.Parser;

namespace NeoEdit.Content.XML
{
	class XMLVisitor : XMLParserBaseVisitor<ParserNode>
	{
		public static ParserNode Parse(string input, bool strict)
		{
			var tree = ParserHelper.Parse<XMLLexer, XMLParser, XMLParser.DocumentContext>(input, parser => parser.document(), strict);
			return new XMLVisitor(input).Visit(tree);
		}

		public static List<string> rFormat(ParserNode node, string input)
		{
			switch (node.Type)
			{
				case DOCUMENT:
					{
						var result = new List<string>();
						var children = node.Children().ToList();
						foreach (var child in children)
							result.AddRange(rFormat(child, input));
						return result;
					}
				case MISC:
				case COMMENT:
				case TEXT: return input.Substring(node.Start, node.Length).Split('\n').Select(str => str.TrimEnd('\r').Trim()).ToList();
				case ELEMENT:
					{
						var result = new List<string>();

						var attributes = node.Attributes(true).ToList();
						var name = attributes.Where(attr => attr.Type == TAG).Single();
						attributes = attributes.Where(attr => attr != name).ToList(); ;

						var attrs = new List<string> { name.Text };
						attrs.AddRange(attributes.Select(attr => $"{attr.Type}=\"{input.Substring(attr.Start, attr.Length)}\""));
						var open = string.Join(" ", attrs);

						var children = node.Children().ToList();
						if (children.Any())
						{
							result.Add($"<{open}>");
							var close = $"</{name.Text}>";
							var childData = children.SelectMany(child => rFormat(child, input)).ToList();
							if ((children.Count == 1) && (childData.Count == 1) && (children[0].Type == TEXT))
							{
								result[result.Count - 1] += childData[0];
								result[result.Count - 1] += close;
							}
							else
							{
								result.AddRange(childData.Select(str => $"\t{str}"));
								result.Add(close);
							}
						}
						else
							result.Add($"<{open} />");


						return result;
					}
				default: throw new ArgumentException("Unable to interpret XML");
			}
		}

		public static string Format(ParserNode document, string input) => string.Join("", rFormat(document, input).Select(str => $"{str}\r\n"));

		const string DOCUMENT = "Document";
		const string COMMENT = "Comment";
		const string MISC = "Misc";
		const string TEXT = "Text";
		const string ELEMENT = "Element";
		const string TAG = "Tag";

		ParserNode Parent => stack.FirstOrDefault();
		readonly Stack<ParserNode> stack = new Stack<ParserNode>();
		readonly string input;
		XMLVisitor(string input) { this.input = input; }

		ParserNode HandleContext(ParserRuleContext context, string type)
		{
			ParserNode node = null;
			if (type != null)
			{
				int start, end;
				context.GetBounds(out start, out end);
				if (type == TEXT)
				{
					while ((start < end) && (char.IsWhiteSpace(input[start])))
						++start;
					while ((end > start) && (char.IsWhiteSpace(input[end - 1])))
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

		public override ParserNode VisitDocument(XMLParser.DocumentContext context) => HandleContext(context, DOCUMENT);
		public override ParserNode VisitComment(XMLParser.CommentContext context) => HandleContext(context, COMMENT);
		public override ParserNode VisitMisc(XMLParser.MiscContext context) => HandleContext(context, MISC);
		public override ParserNode VisitText(XMLParser.TextContext context) => HandleContext(context, TEXT);
		public override ParserNode VisitElement(XMLParser.ElementContext context) => HandleContext(context, ELEMENT);

		public override ParserNode VisitAttribute(XMLParser.AttributeContext context)
		{
			var start = context.value.StartIndex + 1;
			var end = context.value.StopIndex;
			Parent.AddAttr(context.name.Text, input.Substring(start, end - start), start, end);
			return base.VisitAttribute(context);
		}

		public override ParserNode VisitTagname(XMLParser.TagnameContext context)
		{
			Parent.AddAttr(TAG, input, context);
			return base.VisitTagname(context);
		}
	}
}
