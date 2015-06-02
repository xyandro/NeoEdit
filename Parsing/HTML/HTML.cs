using System;
using System.Collections.Generic;
using System.Linq;

namespace NeoEdit.Parsing
{
	class HTML
	{
		const string Name = "Name";
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
					itemName = Name;
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

			if (result.GetAttrText(Name) == "!doctype")
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
			var doc = new ParserNode { Type = Element, Start = location, End = input.Length };
			doc.AddAttr(Name, Doc);
			var stack = new Stack<Tuple<string, ParserNode>>();
			stack.Push(Tuple.Create(Doc, doc));
			while (location < input.Length)
			{
				var topStack = stack.Peek().Item2;

				var textName = topStack.GetAttrText(Name);
				var rawName = rawTextElements.Contains(textName) ? textName : null;

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

				var name = node.GetAttrText(Name) ?? "";
				if (!name.StartsWith("/"))
				{
					node.Parent = topStack;
					if (voidElements.Contains(name))
						node.End = location;
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
						item.Item2.End = node.Start;
						if (item == toRemove)
						{
							item.Item2.End = node.End;
							break;
						}
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
		static Tuple<List<string>, bool, bool> rFormatHTML(ParserNode node, string data)
		{
			if (node.Type != Element)
			{
				var text = data.Substring(node.Start, node.Length);
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
				if (node.Type == Comment)
				{
					var startComment = data.Substring(node.Start, node.Start - node.Start);
					var endComment = data.Substring(node.End, node.End - node.End);
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
			var name = node.GetAttrText(Name);
			var attrs = node.GetAttrTypes().Where(attr => (attr != ParserNode.TYPE) && (attr != SelfClosing)).OrderBy(attr => attr != Name).ToList();
			foreach (var attr in attrs)
			{
				var isTag = attr == Name;
				foreach (var value in node.GetAttrsText(attr))
				{
					if (isTag)
						startTagItems.Add(value);
					else
						startTagItems.Add(String.Format("{0}=\"{1}\"", attr, value));
				}
			}
			var startTag = "<" + String.Join(" ", startTagItems) + (node.HasAttr(SelfClosing) ? "/" : "") + ">";
			var endTag = (voidElements.Contains(name)) || (node.HasAttr(SelfClosing)) ? "" : String.Format("</{0}>", name);

			var childrenOutput = new List<string>();
			var children = node.List(ParserNode.ParserNodeListType.Children).ToList();
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

			if (name == "DOC")
				return Tuple.Create(childrenOutput, false, false);

			if (childrenOutput.Count <= 1)
			{
				var resultStr = startTag + childrenOutput.FirstOrDefault() + endTag;
				if ((childrenOutput.Count == 0) || (resultStr.Length < 200))
					return Tuple.Create(new List<string> { resultStr }, joinLeftSet.Contains(name), joinRightSet.Contains(name));
			}

			var output = new List<string> { startTag };
			output.AddRange(childrenOutput.Select(str => "\t" + str.TrimStart(' ')));
			if (!String.IsNullOrWhiteSpace(endTag))
				output.Add(endTag);

			return Tuple.Create(output, false, false);
		}

		static public string FormatHTML(ParserNode node, string data)
		{
			return String.Join("", rFormatHTML(node, data).Item1.Select(text => String.Format("{0}\r\n", text.TrimEnd())));
		}
	}
}
