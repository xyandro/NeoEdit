using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using NeoEdit.Common;
using NeoEdit.Common.Parsing;
using NeoEdit.TextEdit.Content.HTML.Parser;

namespace NeoEdit.TextEdit.Content.HTML
{
	class HTMLVisitor : HTMLParserBaseVisitor<ParserNode>
	{
		public static ParserNode Parse(string input)
		{
			var tree = ParserHelper.Parse<HTMLLexer, HTMLParser, HTMLParser.DocumentContext>(input, parser => parser.document(), caseSensitive: false);
			return new HTMLVisitor(input).Visit(tree);
		}

		static HashSet<string> joinLeftSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "a", "b", "em", "i", "small", "strong", "sub", "sup", "ins", "del", "mark", "u", "br" };
		static HashSet<string> joinRightSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "a", "b", "em", "i", "small", "strong", "sub", "sup", "ins", "del", "mark", "u", };
		public static Tuple<List<string>, bool, bool> rFormat(ParserNode node, string input)
		{
			switch (node.Type)
			{
				case DOCUMENT:
					{
						var result = new List<string>();
						var children = node.List(ParserNode.ParserNodeListType.Children).ToList();
						foreach (var child in children)
							result.AddRange(rFormat(child, input).Item1);
						return Tuple.Create(result, false, false);
					}
				case MISC:
				case COMMENT:
				case TEXT:
				case SCRIPT:
				case STYLE:
					{
						var result = input.Substring(node.Start, node.Length).Split('\n').Select(str => str.TrimEnd('\r').Trim()).ToList();
						return Tuple.Create(result, result.Count <= 1, result.Count <= 1);
					}
				case ELEMENT:
					{
						var result = new List<string>();

						var attributes = node.List(ParserNode.ParserNodeListType.Attributes).Where(attr => attr.HasLocation).ToList();
						var nameTag = attributes.Where(attr => attr.Type == NAME).Single();
						var name = nameTag.Text;
						attributes = attributes.Where(attr => attr != nameTag).ToList(); ;

						var attrs = new List<string> { name };
						attrs.AddRange(attributes.Select(attr => $"{attr.Type}=\"{input.Substring(attr.Start, attr.Length)}\""));
						var open = String.Join(" ", attrs);

						var startTag = $"<{open}{(node.HasAttr(SELFCLOSING) ? "/" : "")}>";
						var endTag = $"</{name}>";

						if ((node.HasAttr(SELFCLOSING)) || (node.HasAttr(ISVOID)))
							return Tuple.Create(new List<string> { startTag }, joinLeftSet.Contains(name), joinRightSet.Contains(name));

						var childrenOutput = new List<string>();
						var children = node.List(ParserNode.ParserNodeListType.Children).ToList();
						var joinRight = false;
						foreach (var child in children)
						{
							var childData = rFormat(child, input);
							if (childData.Item1.Count == 0)
								continue;
							if ((childData.Item1.Count == 1) && (childrenOutput.Count != 0) && (joinRight) && (childData.Item2))
								childrenOutput[childrenOutput.Count - 1] += childData.Item1.First();
							else
								childrenOutput.AddRange(childData.Item1);
							joinRight = childData.Item3;
						}

						childrenOutput = childrenOutput.Where((str, index) => (index == 0) || (!String.IsNullOrWhiteSpace(childrenOutput[index])) || (!String.IsNullOrWhiteSpace(childrenOutput[index - 1]))).ToList();

						if (childrenOutput.Count <= 1)
						{
							var resultStr = startTag + childrenOutput.FirstOrDefault() + endTag;
							if ((childrenOutput.Count == 0) || (resultStr.Length < 200))
								return Tuple.Create(new List<string> { resultStr }, joinLeftSet.Contains(name), joinRightSet.Contains(name));
						}

						var output = new List<string> { startTag };
						output.AddRange(childrenOutput.Select(str => $"\t{str.TrimStart(' ')}"));
						if (!String.IsNullOrWhiteSpace(endTag))
							output.Add(endTag);

						return Tuple.Create(output, false, false);
					}
				default: throw new ArgumentException("Unable to interpret HTML");
			}
		}

		public static string Format(ParserNode document, string input) => String.Join("", rFormat(document, input).Item1.Select(str => $"{str.TrimEnd()}\r\n"));

		public static string Comment(TextData data, Range range)
		{
			var str = data.GetString(range.Start, range.Length);
			if (String.IsNullOrWhiteSpace(str))
				return str;
			return $"<!--{str.Replace("-->", "--><!--")}-->";
		}

		public static string Uncomment(TextData data, Range range)
		{
			var str = data.GetString(range.Start, range.Length);
			if ((String.IsNullOrWhiteSpace(str)) || (!str.StartsWith("<!--")) || (!str.EndsWith("-->")))
				return str;
			return str.Substring(4, str.Length - 7).Replace("--><!--", "-->");
		}

		const string DOCUMENT = "Document";
		const string COMMENT = "Comment";
		const string MISC = "Misc";
		const string TEXT = "Text";
		const string SCRIPT = "Script";
		const string STYLE = "Style";
		const string ELEMENT = "Element";
		const string NAME = "Name";
		const string SELFCLOSING = "SelfClosing";
		const string ISVOID = "IsVoid";

		ParserNode Parent => stack.FirstOrDefault();
		readonly Stack<ParserNode> stack = new Stack<ParserNode>();
		readonly string input;
		HTMLVisitor(string input) { this.input = input; }

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

		public override ParserNode VisitDocument(HTMLParser.DocumentContext context) => HandleContext(context, DOCUMENT);
		public override ParserNode VisitComment(HTMLParser.CommentContext context) => HandleContext(context, COMMENT);
		public override ParserNode VisitMisc(HTMLParser.MiscContext context) => HandleContext(context, MISC);
		public override ParserNode VisitText(HTMLParser.TextContext context) => HandleContext(context, TEXT);

		public override ParserNode VisitElement(HTMLParser.ElementContext context)
		{
			var node = new ParserNode { Type = ELEMENT, Parent = Parent, LocationParserRule = context };
			stack.Push(node);
			if (context.SLASHCLOSE() != null)
				node.AddAttr(SELFCLOSING, "True");

			if (context.tag != null)
			{
				var start = context.tag.StartIndex + 1; ;
				var end = context.tag.StopIndex + 1;
				var tag = input.Substring(start, end - start);
				node.AddAttr(NAME, tag, start, end);

				context.GetBounds(out start, out end);
				start = context.body.StartIndex;
				end = input.LastIndexOf('<', end - 1);
				while (Char.IsWhiteSpace(input[end - 1]))
					--end;
				while ((start < end) && (Char.IsWhiteSpace(input[start])))
					++start;
				var type = TEXT;
				switch (tag.ToLower())
				{
					case "script": type = SCRIPT; break;
					case "style": type = STYLE; break;
				}
				new ParserNode { Type = type, Parent = node, Start = start, End = end };
			}

			base.VisitElement(context);

			stack.Pop();
			return null;
		}

		public override ParserNode VisitElementopen(HTMLParser.ElementopenContext context)
		{
			stack.Push(new ParserNode { Type = ELEMENT, Parent = Parent, LocationParserRule = context });
			return base.VisitElementopen(context);
		}

		public override ParserNode VisitElementclose(HTMLParser.ElementcloseContext context)
		{
			base.VisitElementclose(context);

			if (context.name == null)
				return null;

			var current = context.name.Text;
			var found = stack.FirstOrDefault(node => node.GetAttrText(NAME) == current);
			if (found == null)
				return null;

			int start, end;
			context.GetBounds(out start, out end);
			found.End = end;
			while ((start > 0) && (Char.IsWhiteSpace(input[start - 1])))
				--start;
			while (true)
			{
				var item = stack.Pop();
				if (item == found)
					break;
				item.End = start;
			}
			return null;
		}

		public override ParserNode VisitAttribute(HTMLParser.AttributeContext context)
		{
			var value = context.value;
			if (value == null)
				Parent.AddAttr(context.name.Text, default(string));
			else
			{
				var start = value.StartIndex;
				var end = value.StopIndex + 1;
				if (context.ATTRSTRING() != null)
				{
					++start;
					--end;
				}
				Parent.AddAttr(context.name.Text, input.Substring(start, end - start), start, end);
			}
			return base.VisitAttribute(context);
		}

		public override ParserNode VisitTagname(HTMLParser.TagnameContext context)
		{
			Parent.AddAttr(NAME, input, context);
			return base.VisitTagname(context);
		}

		public override ParserNode VisitVoidname(HTMLParser.VoidnameContext context)
		{
			Parent.AddAttr(NAME, input, context);
			Parent.AddAttr(ISVOID, "True");
			return base.VisitVoidname(context);
		}
	}
}
