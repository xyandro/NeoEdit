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

		List<Range> GetChildren(MarkupNode node, Range range, bool allChildren)
		{
			var innerMost = GetInnerMostNode(node, range);
			if (innerMost == null)
				return new List<Range>();

			var children = innerMost.Children().ToList();
			if ((!allChildren) && (children.Any(child => child.NodeType == MarkupNode.MarkupNodeType.Element)))
				children = children.Where(child => (child.NodeType != MarkupNode.MarkupNodeType.Text) || (!String.IsNullOrWhiteSpace(child.GetInnerText(Data)))).ToList();

			return children.Select(child => child.OuterRange).ToList();
		}

		List<Range> GetText(MarkupNode node, Range range, bool trimWhitespace)
		{
			var innerMost = GetInnerMostNode(node, range);
			if (innerMost == null)
				return new List<Range>();

			var children = innerMost.DescendantsAndSelf().Where(child => child.NodeType == MarkupNode.MarkupNodeType.Text).ToList();
			var ranges = children.Select(child => child.OuterRange).ToList();

			if (trimWhitespace)
				ranges = ranges.Select(child => TrimRange(child)).ToList();

			ranges = ranges.Where(child => child.HasSelection).ToList();
			return ranges;
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

		bool MarkupSelectByType(MarkupNode node, Range range, MarkupNode.MarkupNodeType type)
		{
			var innerMost = GetInnerMostNode(node, range);
			if (innerMost == null)
				return false;
			return innerMost.NodeType == type;
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

		internal void Command_Markup_Children(bool allChildren)
		{
			var docNode = GetHTMLNode();
			Selections.Replace(Selections.SelectMany(range => GetChildren(docNode, range, allChildren)).ToList());
		}

		internal void Command_Markup_Text(bool trimWhitespace)
		{
			var docNode = GetHTMLNode();
			Selections.Replace(Selections.SelectMany(range => GetText(docNode, range, trimWhitespace)).ToList());
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

		internal void Command_Markup_Select(MarkupNode.MarkupNodeType type)
		{
			var docNode = GetHTMLNode();
			Selections.Replace(Selections.Where(range => MarkupSelectByType(docNode, range, type)).ToList());
		}
	}
}
