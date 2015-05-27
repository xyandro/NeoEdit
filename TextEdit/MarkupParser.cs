using System;
using System.Collections.Generic;
using System.Linq;

namespace NeoEdit.TextEdit
{
	static class MarkupParser
	{
		static MarkupNode GetCommentNode(string str, ref int location)
		{
			if ((location >= str.Length - 7) || (str.Substring(location, 4) != "<!--"))
				return null;
			var startComment = location;
			var endComment = str.IndexOf("-->", location) + 3;
			if (endComment == 2) // IndexOf returned -1
				endComment = str.Length;
			location = endComment;
			return new MarkupNode { NodeType = MarkupNode.MarkupNodeType.Comment, Start = startComment, End = endComment, SelfClosing = true };
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
		static MarkupNode GetOpenCloseNode(string str, ref int location)
		{
			var step = OpenCloseStep.Start;
			int itemStart = 0;
			string itemName = null;
			var result = new MarkupNode { NodeType = MarkupNode.MarkupNodeType.Element, Start = location };
			var inQuote = (char)0;

			while (true)
			{
				if (step == OpenCloseStep.Start)
				{
					if ((location >= str.Length) || (str[location] != '<'))
						return null;
					++location;
					itemName = "Tag";
					step = OpenCloseStep.BeforeAttrValueWS;
				}
				else if ((step == OpenCloseStep.BeforeAttrNameWS) || (step == OpenCloseStep.AfterAttrNameWS) || (step == OpenCloseStep.BeforeAttrValueWS))
				{
					if ((location < str.Length) && (Char.IsWhiteSpace(str[location])))
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
					if ((step == OpenCloseStep.AttrValue) && (itemStart == location) && (location < str.Length) && ((str[location] == '"') || (str[location] == '\'')))
					{
						inQuote = str[location];
						++itemStart;
						++location;
					}

					var stop = (location >= str.Length) || ((inQuote == 0) && ((str[location] == '<') || (str[location] == '>') || ((str[location] == '/') && (location != result.Start + 1))));
					if ((itemStart == location) && (stop))
					{
						step = OpenCloseStep.End;
						continue;
					}

					if ((!stop) && ((inQuote != 0) || ((!Char.IsWhiteSpace(str[location])) && ((step == OpenCloseStep.AttrValue) || (str[location] != '=')))) && ((inQuote == 0) || (str[location] != inQuote)))
					{
						++location;
						continue;
					}

					var value = str.Substring(itemStart, location - itemStart);
					if (step == OpenCloseStep.AttrName)
					{
						itemName = value;
						step = OpenCloseStep.AfterAttrNameWS;
					}
					else
					{
						result.AddAttribute(itemName, value, itemStart, location);
						step = OpenCloseStep.BeforeAttrNameWS;
					}

					if (inQuote != 0)
					{
						if ((location < str.Length) && (str[location] == inQuote))
							++location;
						inQuote = (char)0;
					}
				}
				else if (step == OpenCloseStep.AttrEqual)
				{
					if ((location < str.Length) && (str[location] == '='))
					{
						step = OpenCloseStep.BeforeAttrValueWS;
						++location;
					}
					else
						step = OpenCloseStep.BeforeAttrNameWS;
				}
				else if (step == OpenCloseStep.End)
				{
					if ((location < str.Length) && (str[location] == '/'))
					{
						result.SelfClosing = true;
						++location;
					}
					if ((location < str.Length) && (str[location] == '>'))
						++location;
					break;
				}
			}

			if (result.GetAttribute("Tag") == "!doctype")
				return new MarkupNode { NodeType = MarkupNode.MarkupNodeType.Comment, Start = result.Start, End = location };

			result.End = location;
			return result;
		}

		static MarkupNode GetTextNode(string str, ref int location, string rawName)
		{
			var startLocation = location;
			var find = rawName == null ? "<" : "</" + rawName;
			location = str.IndexOf(find, startLocation);
			if (location == -1)
				location = str.Length;
			var endLocation = location;
			while ((startLocation < endLocation) && (Char.IsWhiteSpace(str[startLocation])))
				++startLocation;
			while ((endLocation > startLocation) && (Char.IsWhiteSpace(str[endLocation - 1])))
				--endLocation;
			return new MarkupNode { NodeType = MarkupNode.MarkupNodeType.Text, Start = startLocation, End = endLocation, SelfClosing = true };
		}

		static readonly HashSet<string> voidElements = new HashSet<string> { "area", "base", "br", "col", "embed", "hr", "img", "input", "keygen", "link", "meta", "param", "source", "track", "wbr" };
		static readonly HashSet<string> rawTextElements = new HashSet<string> { "script", "style", "textarea", "title" };

		static public MarkupNode ParseHTML(string str, int location)
		{
			var doc = new MarkupNode { NodeType = MarkupNode.MarkupNodeType.Element, Start = location };
			doc.AddAttribute("Tag", "DOC", 0, 0);
			var stack = new Stack<Tuple<string, MarkupNode>>();
			stack.Push(Tuple.Create("DOC", doc));
			while (location < str.Length)
			{
				var topStack = stack.Peek().Item2;

				var textTag = topStack.GetAttribute("Tag");
				var rawName = rawTextElements.Contains(textTag) ? textTag : null;

				var node = GetCommentNode(str, ref location) ?? GetOpenCloseNode(str, ref location) ?? GetTextNode(str, ref location, rawName);
				if (node == null)
					throw new ArgumentException("Failed to parse HTML");

				if ((node.NodeType == MarkupNode.MarkupNodeType.Text) && (node.Start == node.End))
					continue;

				if (node.SelfClosing)
				{
					topStack.AddChild(node);
					continue;
				}

				var tag = node.GetAttribute("Tag");
				if (!tag.StartsWith("/"))
				{
					topStack.AddChild(node);
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
