using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;

namespace NeoEdit.TextEdit
{
	class MarkupNode
	{
		public enum MarkupNodeType { Unknown, Element, Text, Comment, All }

		public readonly MarkupNode Parent;

		public readonly int StartOuterPosition;
		public readonly int EndOuterPosition;
		public readonly int OuterLength;

		public readonly int StartInnerPosition;
		public readonly int EndInnerPosition;
		public readonly int InnerLength;

		public readonly string Name;

		public readonly MarkupNodeType NodeType;

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
			return new MarkupNode(doc.DocumentNode, null, position);
		}

		MarkupNode(HtmlNode node, MarkupNode parent, int position)
		{
			Parent = parent;
			Name = node.Name;

			StartOuterPosition = StartInnerPosition = position;
			EndOuterPosition = EndInnerPosition = position + node.OuterHtml.Length;
			OuterLength = InnerLength = EndOuterPosition - StartOuterPosition;

			switch (node.NodeType)
			{
				case HtmlNodeType.Document:
				case HtmlNodeType.Element: NodeType = MarkupNodeType.Element; break;
				case HtmlNodeType.Comment: NodeType = MarkupNodeType.Comment; break;
				case HtmlNodeType.Text: NodeType = MarkupNodeType.Text; break;
			}

			var first = true;
			foreach (var child in node.ChildNodes)
			{
				if (first)
				{
					position += node.OuterHtml.IndexOf(child.OuterHtml);
					first = false;
				}

				children.Add(new MarkupNode(child, this, position));
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

		public IEnumerable<MarkupNode> Children()
		{
			return children;
		}

		public IEnumerable<MarkupNode> ChildrenAndSelf()
		{
			yield return this;
			foreach (var child in Children())
				yield return child;
		}

		public IEnumerable<MarkupNode> Descendants()
		{
			foreach (var child in children)
			{
				yield return child;
				foreach (var childChild in child.Descendants())
					yield return childChild;
			}
		}

		public IEnumerable<MarkupNode> DescendantsAndSelf()
		{
			yield return this;
			foreach (var child in Descendants())
				yield return child;
		}
	}
}
