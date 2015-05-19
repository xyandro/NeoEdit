using System;
using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;

namespace NeoEdit.TextEdit
{
	class MarkupNode
	{
		[Flags]
		public enum MarkupNodeType
		{
			None = 0,
			Element = 1,
			Text = 2,
			Comment = 4,
			All = Element | Text | Comment
		}

		[Flags]
		public enum MarkupNodeList
		{
			None = 0,
			Self = 1,
			Children = 2,
			Descendants = 4,
			Parents = 8,
			SelfAndChildren = Self | Children,
			SelfAndDescendants = Self | Descendants,
			SelfAndParents = Self | Parents,
		}

		public readonly MarkupNode Parent;

		public readonly int StartOuterPosition;
		public readonly int EndOuterPosition;
		public readonly int OuterLength;

		public readonly int StartInnerPosition;
		public readonly int EndInnerPosition;
		public readonly int InnerLength;

		public readonly int Depth;

		public readonly MarkupNodeType NodeType;

		public readonly Dictionary<string, List<string>> Attributes = new Dictionary<string, List<string>>();

		public Range RangeOuter
		{
			get
			{
				switch (NodeType)
				{
					case MarkupNodeType.Text:
					case MarkupNodeType.Comment:
						return RangeOuterFull;
					default:
						return RangeOuterStart;
				}
			}
		}
		public Range RangeOuterFull { get { return new Range(StartOuterPosition, EndOuterPosition); } }
		public Range RangeOuterStart { get { return new Range(StartOuterPosition); } }
		public Range RangeOuterEnd { get { return new Range(EndOuterPosition); } }
		public Range RangeInnerFull { get { return new Range(StartInnerPosition, EndInnerPosition); } }

		readonly List<MarkupNode> children = new List<MarkupNode>();

		static public MarkupNode FromHTML(string data, int position)
		{
			var doc = new HtmlDocument();
			doc.LoadHtml(data);
			return new MarkupNode(doc.DocumentNode, null, position, 0, data);
		}

		MarkupNode(HtmlNode node, MarkupNode parent, int position, int depth, string data)
		{
			Parent = parent;
			Depth = depth;

			StartOuterPosition = StartInnerPosition = position;
			EndOuterPosition = EndInnerPosition = position + node.OuterHtml.Length;
			OuterLength = InnerLength = EndOuterPosition - StartOuterPosition;

			if (data.Substring(StartOuterPosition, OuterLength) != node.OuterHtml)
				throw new Exception("Failed to parse HTML.");

			switch (node.NodeType)
			{
				case HtmlNodeType.Document:
				case HtmlNodeType.Element: NodeType = MarkupNodeType.Element; break;
				case HtmlNodeType.Comment: NodeType = MarkupNodeType.Comment; break;
				case HtmlNodeType.Text: NodeType = MarkupNodeType.Text; break;
			}

			Attributes = node.Attributes.GroupBy(attr => attr.Name).ToDictionary(group => group.Key, group => group.Select(attr => attr.Value).ToList());

			if (!Attributes.ContainsKey("tag"))
				Attributes["tag"] = new List<string>();
			Attributes["tag"].Add(node.Name);

			var first = true;
			foreach (var child in node.ChildNodes)
			{
				if (first)
				{
					if (child.StreamPosition != 0)
						position += child.StreamPosition - node.StreamPosition;
					else
						position += node.OuterHtml.LastIndexOf(child.OuterHtml);
					first = false;
				}

				children.Add(new MarkupNode(child, this, position, depth + 1, data));
				position += child.OuterHtml.Length;
			}

			if (children.Any())
			{
				StartInnerPosition = children.First().StartOuterPosition;
				EndInnerPosition = children.Last().EndOuterPosition;
				InnerLength = EndInnerPosition - StartInnerPosition;
			}
		}

		public string GetOuterText(TextData data)
		{
			return data.GetString(StartOuterPosition, OuterLength);
		}

		public string GetInnerText(TextData data)
		{
			return data.GetString(StartInnerPosition, InnerLength);
		}

		public IEnumerable<MarkupNode> List(MarkupNodeList list)
		{
			if (list.HasFlag(MarkupNodeList.Self))
				yield return this;
			if (list.HasFlag(MarkupNodeList.Parents))
			{
				for (var parent = this.Parent; parent != null; parent = parent.Parent)
					yield return parent;
			}
			if ((list.HasFlag(MarkupNodeList.Children)) || (list.HasFlag(MarkupNodeList.Descendants)))
			{
				foreach (var child in children)
				{
					yield return child;
					if (list.HasFlag(MarkupNodeList.Descendants))
						foreach (var childChild in child.List(MarkupNodeList.Descendants))
							yield return childChild;
				}
			}
		}

		public bool HasAttribute(string name, string value)
		{
			if (!Attributes.ContainsKey(name))
				return false;
			return Attributes[name].Any(attr => attr == value);
		}

		public override string ToString()
		{
			return RangeOuterFull.ToString();
		}
	}
}
