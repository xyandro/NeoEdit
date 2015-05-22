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
			return new MarkupNode { NodeType = MarkupNode.MarkupNodeType.Comment, StartOuterPosition = startComment, EndOuterPosition = endComment, StartInnerPosition = startComment + 4, EndInnerPosition = endComment - 3, SelfClosing = true };
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
			var result = new MarkupNode { NodeType = MarkupNode.MarkupNodeType.Element, StartOuterPosition = location };
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

					var stop = (location >= str.Length) || ((inQuote == 0) && ((str[location] == '<') || (str[location] == '>') || (str.Substring(location).StartsWith("/>"))));
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
				return new MarkupNode { NodeType = MarkupNode.MarkupNodeType.Comment, StartOuterPosition = result.StartOuterPosition, StartInnerPosition = result.StartOuterPosition + 2, EndInnerPosition = location - 1, EndOuterPosition = location };

			result.StartInnerPosition = result.EndInnerPosition = result.EndOuterPosition = location;
			return result;
		}

		static MarkupNode GetTextNode(string str, ref int location, string rawName)
		{
			var startLocation = location;
			var find = rawName == null ? "<" : "</" + rawName;
			location = str.IndexOf(find, startLocation);
			if (location == -1)
				location = str.Length;
			return new MarkupNode { NodeType = MarkupNode.MarkupNodeType.Text, StartOuterPosition = startLocation, StartInnerPosition = startLocation, EndInnerPosition = location, EndOuterPosition = location, SelfClosing = true };
		}

		static readonly HashSet<string> voidElements = new HashSet<string> { "area", "base", "br", "col", "embed", "hr", "img", "input", "keygen", "link", "meta", "param", "source", "track", "wbr" };
		static readonly HashSet<string> rawTextElements = new HashSet<string> { "script", "style", "textarea", "title" };

		static public MarkupNode ParseHTML(string str, int location)
		{
			var doc = new MarkupNode { NodeType = MarkupNode.MarkupNodeType.Element, StartOuterPosition = location, StartInnerPosition = location };
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
						node.EndInnerPosition = node.EndOuterPosition = location;
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
						item.Item2.EndInnerPosition = item.Item2.EndOuterPosition = node.StartOuterPosition;
						if (item == toRemove)
						{
							item.Item2.EndOuterPosition = node.StartInnerPosition;
							break;
						}
					}
				}
			}
			while (stack.Any())
			{
				var item = stack.Pop();
				item.Item2.EndInnerPosition = item.Item2.EndOuterPosition = location;
			}

			return doc;
		}

		static string TrimBeginWS(string str)
		{
			var trimStr = str.TrimStart();
			if ((trimStr.Length != 0) && (trimStr != str))
				trimStr = " " + trimStr;
			return trimStr;
		}

		static string TrimEndWS(string str)
		{
			var trimStr = str.TrimEnd();
			if ((trimStr.Length != 0) && (trimStr != str))
				trimStr += " ";
			return trimStr;
		}

		static string TrimWS(string str)
		{
			return TrimBeginWS(TrimEndWS(str));
		}

		static HashSet<string> joinLeftSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "a", "b", "em", "i", "small", "strong", "sub", "sup", "ins", "del", "mark", "u", "br" };
		static HashSet<string> joinRightSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "a", "b", "em", "i", "small", "strong", "sub", "sup", "ins", "del", "mark", "u", };
		static Tuple<List<string>, bool, bool> rFormatHTML(MarkupNode node, string data)
		{
			if (node.NodeType != MarkupNode.MarkupNodeType.Element)
			{
				var text = data.Substring(node.StartInnerPosition, node.InnerLength);
				if (String.IsNullOrWhiteSpace(text))
					return Tuple.Create(new List<string> { }, true, true);

				var lines = text.Split(new string[] { "\r\n", "\n\r", "\n", "\r" }, StringSplitOptions.None).Select(line => TrimEndWS(line)).ToList();
				while (lines.First().Length == 0)
					lines.RemoveAt(0);
				while (lines.Last().Length == 0)
					lines.RemoveAt(lines.Count - 1);
				lines = lines.Where((line, index) => (line.Length != 0) || (index == 0) || (lines[index - 1].Length != 0)).ToList();
				if (lines.Count == 1)
					lines = lines.Select(line => TrimBeginWS(line)).ToList();
				else
				{
					var spacing = lines.Select(line => line.Replace("\t", "    ")).Select(line => line.Length == 0 ? -1 : line.Length - line.TrimStart().Length).ToList();
					var trimSpace = spacing.Where(space => space != -1).Min();
					spacing = spacing.Select(space => space == -1 ? 0 : space - trimSpace).ToList();
					lines = lines.Select((line, index) => new String('\t', spacing[index] / 4) + new String(' ', spacing[index] % 4) + line.TrimStart()).ToList();
				}
				if (node.NodeType == MarkupNode.MarkupNodeType.Comment)
				{
					var startComment = data.Substring(node.StartOuterPosition, node.StartInnerPosition - node.StartOuterPosition);
					var endComment = data.Substring(node.EndInnerPosition, node.EndOuterPosition - node.EndInnerPosition);
					if (lines.Count == 1)
						lines[0] = startComment + lines[0] + endComment;
					else
					{
						lines.Insert(0, startComment);
						lines.Add(endComment);
					}
				}
				return Tuple.Create(lines, lines.Count <= 1, lines.Count <= 1);
			}

			var startTagItems = new List<string>();
			var tag = node.GetAttribute("Tag");
			var attrs = node.GetAttributes().OrderBy(attr => attr != "Tag").ToList();
			foreach (var attr in attrs)
			{
				var isTag = attr == "Tag";
				foreach (var value in node.GetAllAttributes(attr))
				{
					if (isTag)
						startTagItems.Add(value);
					else
						startTagItems.Add(String.Format("{0}=\"{1}\"", attr, value));
				}
			}
			var startTag = "<" + String.Join(" ", startTagItems) + (node.SelfClosing ? "/" : "") + ">";
			var endTag = (voidElements.Contains(tag)) || (node.SelfClosing) ? "" : String.Format("</{0}>", tag);

			var childrenOutput = new List<string>();
			var children = node.List(MarkupNode.MarkupNodeList.Children).ToList();
			var joinRight = false;
			foreach (var child in children)
			{
				var childData = rFormatHTML(child, data);
				if (childData.Item1.Count == 0)
					continue;
				if ((childData.Item1.Count == 1) && (childrenOutput.Count != 0) && (joinRight) && (childData.Item2))
					childrenOutput[childrenOutput.Count - 1] += childData.Item1.First();
				else
					childrenOutput.AddRange(childData.Item1);
				joinRight = childData.Item3;
			}

			if (tag == "DOC")
				return Tuple.Create(childrenOutput, false, false);

			if (childrenOutput.Count <= 1)
			{
				var resultStr = startTag + childrenOutput.FirstOrDefault() + endTag;
				if ((childrenOutput.Count == 0) || (resultStr.Length < 200))
					return Tuple.Create(new List<string> { resultStr }, joinLeftSet.Contains(tag), joinRightSet.Contains(tag));
			}

			var output = new List<string> { startTag };
			output.AddRange(childrenOutput.Select(str => "\t" + str.TrimStart(' ')));
			if (!String.IsNullOrWhiteSpace(endTag))
				output.Add(endTag);

			return Tuple.Create(output, false, false);
		}

		static public string FormatHTML(MarkupNode node, string data)
		{
			return String.Join("", rFormatHTML(node, data).Item1.Select(text => String.Format("{0}\r\n", text.TrimEnd())));
		}
	}

	class Program
	{
		static void Main()
		{
			var data = System.IO.File.ReadAllText(@"C:\Dev\NeoEdit - Work\a.htm");
			var doc = MarkupParser.ParseHTML(data, 0);
			var result = MarkupParser.FormatHTML(doc, data);
			System.IO.File.WriteAllText(@"C:\Dev\NeoEdit - Work\a2.htm", result);
		}
	}
}
