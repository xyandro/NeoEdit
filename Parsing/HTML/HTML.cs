using System;
using System.Collections.Generic;
using System.Linq;

namespace NeoEdit.Parsing
{
	public static class HTML
	{
		const string Name = "Name";
		const string Element = "Element";
		const string Comment = "Comment";
		const string Text = "Text";
		const string SelfClosing = "SelfClosing";
		const string Doc = "DOC";

		static ParserNode GetCommentNode(string str, ref int location)
		{
			if ((location >= str.Length - 7) || (str.Substring(location, 4) != "<!--"))
				return null;
			var startComment = location;
			var endComment = str.IndexOf("-->", location) + 3;
			if (endComment == 2) // IndexOf returned -1
				endComment = str.Length;
			location = endComment;
			return new ParserNode(Comment) { { ParserNode.LocationStr, startComment, endComment }, { SelfClosing, "true" } };
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
		static ParserNode GetOpenCloseNode(string str, ref int location)
		{
			var step = OpenCloseStep.Start;
			int itemStart = 0;
			string itemName = null;
			var result = new ParserNode(Element) { { ParserNode.LocationStr, location, location } };
			var inQuote = (char)0;

			while (true)
			{
				if (step == OpenCloseStep.Start)
				{
					if ((location >= str.Length) || (str[location] != '<'))
						return null;
					++location;
					itemName = Name;
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
						result.Add(itemName, value, itemStart, location);
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
						result.Set(SelfClosing, "true");
						++location;
					}
					if ((location < str.Length) && (str[location] == '>'))
						++location;
					break;
				}
			}

			if (result.GetAttrText(Name) == "!doctype")
				return new ParserNode(Comment) { { ParserNode.LocationStr, result.Start, location } };

			result.Set(ParserNode.LocationStr, result.Start, location);
			return result;
		}

		static ParserNode GetTextNode(string str, ref int location, string rawName)
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
			return new ParserNode(Text) { { ParserNode.LocationStr, startLocation, endLocation }, { SelfClosing, "true" } };
		}

		static readonly HashSet<string> voidElements = new HashSet<string> { "area", "base", "br", "col", "embed", "hr", "img", "input", "keygen", "link", "meta", "param", "source", "track", "wbr" };
		static readonly HashSet<string> rawTextElements = new HashSet<string> { "script", "style", "textarea", "title" };

		static public ParserNode ParseHTML(string str, int location)
		{
			var doc = new ParserNode(Element) { { ParserNode.LocationStr, location, location }, { Name, Doc } };
			var stack = new Stack<Tuple<string, ParserNode>>();
			stack.Push(Tuple.Create(Doc, doc));
			while (location < str.Length)
			{
				var topStack = stack.Peek().Item2;

				var textTag = topStack.GetAttrText(Name);
				var rawName = rawTextElements.Contains(textTag) ? textTag : null;

				var node = GetCommentNode(str, ref location) ?? GetOpenCloseNode(str, ref location) ?? GetTextNode(str, ref location, rawName);
				if (node == null)
					throw new ArgumentException("Failed to parse HTML");

				if ((node.Type == Text) && (node.Start == node.End))
					continue;

				if (node.GetAttrText(SelfClosing) == "true")
				{
					node.Parent = topStack;
					continue;
				}

				var name = node.GetAttrText(Name);
				if (!name.StartsWith("/"))
				{
					node.Parent = topStack;
					if (voidElements.Contains(name))
						node.Set(ParserNode.LocationStr, node.Start, location);
					else
						stack.Push(Tuple.Create(name, node));
					continue;
				}

				name = name.Substring(1);
				var toRemove = stack.FirstOrDefault(item => (item.Item1 == name) && (item.Item2 != doc));
				if (toRemove != null)
				{
					while (true)
					{
						var item = stack.Pop();
						item.Item2.Set(ParserNode.LocationStr, item.Item2.Start, node.Start);
						if (item == toRemove)
							break;
					}
				}
			}
			while (stack.Any())
			{
				var item = stack.Pop();
				item.Item2.Set(ParserNode.LocationStr, item.Item2.Start, location);
			}

			return doc;
		}
	}
}
