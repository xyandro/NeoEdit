using System;
using System.Collections.Generic;
using System.Linq;

namespace NeoEdit.Parsing
{
	class HTML
	{
		const string Tag = "Tag";
		const string Element = "Element";
		const string Comment = "Comment";
		const string Text = "Text";
		const string SelfClosing = "SelfClosing";
		const string Doc = "DOC";

		readonly string input;
		int location;

		public HTML(string input)
		{
			this.input = input;
			location = 0;
		}

		ParserNode GetCommentNode()
		{
			if ((location >= input.Length - 7) || (input.Substring(location, 4) != "<!--"))
				return null;
			var startComment = location;
			var endComment = input.IndexOf("-->", location) + 3;
			if (endComment == 2) // IndexOf returned -1
				endComment = input.Length;
			location = endComment;
			var node = new ParserNode { Type = Comment, Start = startComment, End = endComment };
			node.AddAttr(SelfClosing, "true");
			return node;
		}

		enum OpenCloseStep
		{
			Start,
			BeforeAttrNameWS,
			AttrName,
			AfterAttrNameWS,
			AttrEqual,
			BeforeAttrValueWS,
			AttrValue,
			End,
		}

		delegate bool CharLocPredicate(char? c, int location);
		delegate Tuple<OpenCloseStep> CharLocAction(char? c, int location);
		ParserNode GetOpenCloseNode()
		{
			var step = OpenCloseStep.Start;
			int itemStart = 0;
			string itemName = null;
			var result = new ParserNode { Type = Element, Start = location };
			var inQuote = (char)0;

			while (true)
			{
				if (step == OpenCloseStep.Start)
				{
					if ((location >= input.Length) || (input[location] != '<'))
						return null;
					++location;
					itemName = Tag;
					step = OpenCloseStep.BeforeAttrValueWS;
				}
				else if ((step == OpenCloseStep.BeforeAttrNameWS) || (step == OpenCloseStep.AfterAttrNameWS) || (step == OpenCloseStep.BeforeAttrValueWS))
				{
					if ((location < input.Length) && (Char.IsWhiteSpace(input[location])))
					{
						++location;
						continue;
					}

					switch (step)
					{
						case OpenCloseStep.BeforeAttrNameWS: step = OpenCloseStep.AttrName; break;
						case OpenCloseStep.AfterAttrNameWS: step = OpenCloseStep.AttrEqual; break;
						case OpenCloseStep.BeforeAttrValueWS: step = OpenCloseStep.AttrValue; break;
					}
					itemStart = location;
				}
				else if ((step == OpenCloseStep.AttrName) || (step == OpenCloseStep.AttrValue))
				{
					if ((step == OpenCloseStep.AttrValue) && (itemStart == location) && (location < input.Length) && ((input[location] == '"') || (input[location] == '\'')))
					{
						inQuote = input[location];
						++itemStart;
						++location;
					}

					var stop = (location >= input.Length) || ((inQuote == 0) && ((input[location] == '<') || (input[location] == '>') || ((input[location] == '/') && (location != result.Start + 1))));
					if ((itemStart == location) && (stop))
					{
						step = OpenCloseStep.End;
						continue;
					}

					if ((!stop) && ((inQuote != 0) || ((!Char.IsWhiteSpace(input[location])) && ((step == OpenCloseStep.AttrValue) || (input[location] != '=')))) && ((inQuote == 0) || (input[location] != inQuote)))
					{
						++location;
						continue;
					}

					var value = input.Substring(itemStart, location - itemStart);
					if (step == OpenCloseStep.AttrName)
					{
						itemName = value;
						step = OpenCloseStep.AfterAttrNameWS;
					}
					else
					{
						result.AddAttr(itemName, value, itemStart, location);
						step = OpenCloseStep.BeforeAttrNameWS;
					}

					if (inQuote != 0)
					{
						if ((location < input.Length) && (input[location] == inQuote))
							++location;
						inQuote = (char)0;
					}
				}
				else if (step == OpenCloseStep.AttrEqual)
				{
					if ((location < input.Length) && (input[location] == '='))
					{
						step = OpenCloseStep.BeforeAttrValueWS;
						++location;
					}
					else
						step = OpenCloseStep.BeforeAttrNameWS;
				}
				else if (step == OpenCloseStep.End)
				{
					if ((location < input.Length) && (input[location] == '/'))
					{
						result.SetAttr(SelfClosing, "true");
						++location;
					}
					if ((location < input.Length) && (input[location] == '>'))
						++location;
					break;
				}
			}

			if (result.GetAttrText(Tag) == "!doctype")
				return new ParserNode { Type = Comment, Start = result.Start, End = location };

			result.End = location;
			return result;
		}

		ParserNode GetTextNode(string rawName)
		{
			var startLocation = location;
			var find = rawName == null ? "<" : "</" + rawName;
			location = input.IndexOf(find, startLocation);
			if (location == -1)
				location = input.Length;
			var endLocation = location;
			while ((startLocation < endLocation) && (Char.IsWhiteSpace(input[startLocation])))
				++startLocation;
			while ((endLocation > startLocation) && (Char.IsWhiteSpace(input[endLocation - 1])))
				--endLocation;
			var node = new ParserNode { Type = Text, Start = startLocation, End = endLocation };
			node.AddAttr(SelfClosing, "true");
			return node;
		}

		static readonly HashSet<string> voidElements = new HashSet<string> { "area", "base", "br", "col", "embed", "hr", "img", "input", "keygen", "link", "meta", "param", "source", "track", "wbr" };
		static readonly HashSet<string> rawTextElements = new HashSet<string> { "script", "style", "textarea", "title" };

		public ParserNode Parse()
		{
			if ((input.Length > 1) && (input[0] == '\ufeff'))
				++location;
			var doc = new ParserNode { Type = Element, Start = location, End = input.Length};
			doc.AddAttr(Tag, Doc);
			var stack = new Stack<Tuple<string, ParserNode>>();
			stack.Push(Tuple.Create(Doc, doc));
			while (location < input.Length)
			{
				var topStack = stack.Peek().Item2;

				var textTag = topStack.GetAttrText(Tag);
				var rawName = rawTextElements.Contains(textTag) ? textTag : null;

				var node = GetCommentNode() ?? GetOpenCloseNode() ?? GetTextNode(rawName);
				if (node == null)
					throw new ArgumentException("Failed to parse HTML");

				if ((node.Type == Text) && (node.Start == node.End))
					continue;

				if (node.GetAttrText(SelfClosing) == "true")
				{
					node.Parent = topStack;
					continue;
				}

				var tag = node.GetAttrText(Tag);
				if (!tag.StartsWith("/"))
				{
					node.Parent = topStack;
					if (voidElements.Contains(tag))
						node.End = location;
					else
						stack.Push(Tuple.Create(tag, node));
					continue;
				}

				tag = tag.Substring(1);
				var toRemove = stack.FirstOrDefault(item => (item.Item1 == tag) && (item.Item2 != doc));
				if (toRemove != null)
				{
					while (true)
					{
						var item = stack.Pop();
						item.Item2.End = node.Start;
						if (item == toRemove)
							break;
					}
				}
			}
			while (stack.Any())
			{
				var item = stack.Pop();
				item.Item2.End = location;
			}

			return doc;
		}
	}
}
