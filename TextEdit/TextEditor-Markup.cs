using System;
using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;

namespace NeoEdit.TextEdit
{
	public partial class TextEditor
	{
		MarkupNode GetHTMLNode()
		{
			var allRange = new Range(BeginOffset(), EndOffset());
			var data = GetString(allRange);
			var mn = MarkupNode.FromHTML(data, BeginOffset());
			if (data.Substring(mn.StartOuterPosition, mn.OuterLength) != data)
				throw new Exception("Invalid data found; please validate.");
			return mn;
		}

		MarkupNode GetInnerMostNode(MarkupNode node, Range range)
		{
			foreach (var child in node.Children().Reverse())
			{
				var result = GetInnerMostNode(child, range);
				if (result != null)
					return result;
			}
			if ((range.Start >= node.StartOuterPosition) && (range.End <= node.EndOuterPosition))
				return node;
			return null;
		}

		Range GetParent(MarkupNode node, Range range)
		{
			var innerMost = GetInnerMostNode(node, range);
			if (innerMost != null)
				innerMost = innerMost.Parent;
			if (innerMost == null)
				return range;
			return innerMost.OuterRange;
		}

		Range GetOuterHtml(MarkupNode node, Range range)
		{
			var innerMost = GetInnerMostNode(node, range);
			if (innerMost == null)
				return range;
			return innerMost.OuterRange;
		}

		Range GetInnerHtml(MarkupNode node, Range range)
		{
			var innerMost = GetInnerMostNode(node, range);
			if (innerMost == null)
				return range;
			if (!innerMost.Children().Any())
				return innerMost.OuterRange;
			return innerMost.InnerRange;
		}

		List<Range> MarkupGetChildrenAndDescendants(MarkupNode rootNode, Range findRange, bool children, MarkupNode.MarkupNodeType type, bool trimWhitespace)
		{
			var innerMost = GetInnerMostNode(rootNode, findRange);
			if (innerMost == null)
				return new List<Range>();

			var nodes = (children ? innerMost.Children().ToList() : innerMost.Descendants()).Select(node => new { Node = node, Range = node.OuterRange }).ToList();
			if (type != MarkupNode.MarkupNodeType.All)
				nodes = nodes.Where(child => child.Node.NodeType == type).ToList();

			if (trimWhitespace)
			{
				nodes = nodes.Select(node => new { Node = node.Node, Range = node.Node.NodeType == MarkupNode.MarkupNodeType.Text ? TrimRange(node.Range) : node.Range }).ToList();
				nodes = nodes.Where(node => (node.Node.NodeType != MarkupNode.MarkupNodeType.Text) || (node.Range.HasSelection)).ToList();
			}

			var ranges = nodes.Select(node => node.Range).ToList();

			// Since descendant elements can overlap, don't leave selections
			if ((!children) && (type != MarkupNode.MarkupNodeType.Comment) && (type != MarkupNode.MarkupNodeType.Text))
				ranges = ranges.Select(range => new Range(range.Start)).ToList();

			return ranges;
		}

		internal void Command_Markup_Tidy()
		{
			// Validates too
			var allRange = new Range(BeginOffset(), EndOffset());
			var str = GetString(allRange);
			str = Win32.Interop.HTMLTidy(str);
			var doc = new HtmlDocument();
			doc.LoadHtml(str);
			Replace(new List<Range> { allRange }, new List<string> { doc.DocumentNode.OuterHtml });
		}

		internal void Command_Markup_Validate()
		{
			var allRange = new Range(BeginOffset(), EndOffset());
			var doc = new HtmlDocument();
			doc.LoadHtml(GetString(allRange));
			Replace(new List<Range> { allRange }, new List<string> { doc.DocumentNode.OuterHtml });
		}

		internal void Command_Markup_Parent()
		{
			var docNode = GetHTMLNode();
			Selections.Replace(Selections.Select(range => GetParent(docNode, range)).ToList());
		}

		internal void Command_Markup_ChildrenAndDescendants(bool children, MarkupNode.MarkupNodeType type, bool trimWhitespace = true)
		{
			var docNode = GetHTMLNode();
			Selections.Replace(Selections.SelectMany(range => MarkupGetChildrenAndDescendants(docNode, range, children, type, trimWhitespace)).ToList());
		}

		internal void Command_Markup_OuterTag()
		{
			var docNode = GetHTMLNode();
			Selections.Replace(Selections.Select(range => GetOuterHtml(docNode, range)).ToList());
		}

		internal void Command_Markup_InnerTag()
		{
			var docNode = GetHTMLNode();
			Selections.Replace(Selections.Select(range => GetInnerHtml(docNode, range)).ToList());
		}
	}
}
