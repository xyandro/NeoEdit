using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using HtmlAgilityPack;
using NeoEdit.GUI.Common;
using NeoEdit.TextEdit.Dialogs;

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
			var childInnerMost = node.Children().Select(child => GetInnerMostNode(child, range)).Where(child => child != null).ToList();
			var elements = childInnerMost.Where(child => child.NodeType == MarkupNode.MarkupNodeType.Element).ToList();
			if (elements.Any())
				return elements.Last();
			if (childInnerMost.Any())
				return childInnerMost.Last();
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
			return innerMost.RangeOuterStart;
		}

		Range GetToggleTagPosition(MarkupNode rootNode, Range range, bool shiftDown)
		{
			var innerMost = GetInnerMostNode(rootNode, range);
			if (innerMost == null)
				return range;
			var pos = innerMost.StartOuterPosition;
			if (pos == range.Cursor)
				pos = innerMost.EndOuterPosition;
			return MoveCursor(range, pos, shiftDown);
		}

		Range GetOuterHtml(MarkupNode node, Range range)
		{
			var innerMost = GetInnerMostNode(node, range);
			if (innerMost == null)
				return range;
			return innerMost.RangeOuterFull;
		}

		Range GetInnerHtml(MarkupNode node, Range range)
		{
			var innerMost = GetInnerMostNode(node, range);
			if (innerMost == null)
				return range;
			if (!innerMost.Children().Any())
				return innerMost.RangeOuterFull;
			return innerMost.RangeInnerFull;
		}

		List<Range> MarkupGetChildrenAndDescendants(MarkupNode rootNode, Range findRange, bool children, MarkupNode.MarkupNodeType type, bool trimWhitespace, FindElementByNameDialog.Result findByName)
		{
			var innerMost = GetInnerMostNode(rootNode, findRange);
			if (innerMost == null)
				return new List<Range>();

			var nodes = (children ? innerMost.Children().ToList() : innerMost.Descendants()).Select(node => new { Node = node, Range = node.RangeOuter }).ToList();
			if (type != MarkupNode.MarkupNodeType.All)
				nodes = nodes.Where(child => child.Node.NodeType == type).ToList();

			if (findByName != null)
				nodes = nodes.Where(node => String.Equals(node.Node.Name, findByName.NameToFind, StringComparison.OrdinalIgnoreCase)).ToList();

			if (trimWhitespace)
			{
				nodes = nodes.Select(node => new { Node = node.Node, Range = node.Node.NodeType == MarkupNode.MarkupNodeType.Text ? TrimRange(node.Range) : node.Range }).ToList();
				nodes = nodes.Where(node => (node.Node.NodeType != MarkupNode.MarkupNodeType.Text) || (node.Range.HasSelection)).ToList();
			}

			var ranges = nodes.Select(node => node.Range).ToList();

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

		internal void Command_Markup_ToggleTagPosition(bool shiftDown)
		{
			var docNode = GetHTMLNode();
			Selections.Replace(Selections.Select(range => GetToggleTagPosition(docNode, range, shiftDown)).ToList());
		}

		internal void Command_Markup_Parent()
		{
			var docNode = GetHTMLNode();
			Selections.Replace(Selections.Select(range => GetParent(docNode, range)).ToList());
		}

		internal FindElementByNameDialog.Result Command_Markup_ChildrenDescendents_ByName_Dialog()
		{
			return FindElementByNameDialog.Run(UIHelper.FindParent<Window>(this));
		}

		internal void Command_Markup_ChildrenAndDescendants(bool children, MarkupNode.MarkupNodeType type = MarkupNode.MarkupNodeType.All, bool trimWhitespace = true, FindElementByNameDialog.Result findByName = null)
		{
			var docNode = GetHTMLNode();
			Selections.Replace(Selections.SelectMany(range => MarkupGetChildrenAndDescendants(docNode, range, children, type, trimWhitespace, findByName)).ToList());
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
