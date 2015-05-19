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

		MarkupNode GetInnerMostNode(MarkupNode node, Range range, MarkupNode.MarkupNodeType type = MarkupNode.MarkupNodeType.All)
		{
			var childInnerMost = node.GetChildren(MarkupNode.MarkupNodeList.Children).Select(child => GetInnerMostNode(child, range, type)).Where(child => child != null).ToList();
			MarkupNode found = null;
			if (found == null)
				found = childInnerMost.Where(child => child.StartOuterPosition == range.Start).FirstOrDefault();
			if (found == null)
				found = childInnerMost.Where(child => child.NodeType == MarkupNode.MarkupNodeType.Element).LastOrDefault();
			if (found == null)
				found = childInnerMost.LastOrDefault();
			if (found != null)
				return found;
			if ((type.HasFlag(node.NodeType)) && (range.Start >= node.StartOuterPosition) && (range.End <= node.EndOuterPosition))
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
			var innerMost = GetInnerMostNode(rootNode, range, MarkupNode.MarkupNodeType.Element | MarkupNode.MarkupNodeType.Comment);
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
			return innerMost.RangeInnerFull;
		}

		List<Range> MarkupGetChildrenAndDescendants(MarkupNode rootNode, Range findRange, MarkupNode.MarkupNodeList list, MarkupNode.MarkupNodeType type, bool trimWhitespace, FindMarkupAttribute.Result findAttr)
		{
			var node = GetInnerMostNode(rootNode, findRange);
			if (node == null)
				return new List<Range>();

			var childNodes = node.GetChildren(list).Select(childNode => new { Node = childNode, Range = childNode.RangeOuter }).ToList();
			childNodes = childNodes.Where(child => type.HasFlag(child.Node.NodeType)).ToList();

			if (trimWhitespace)
			{
				childNodes = childNodes.Select(childNode => new { Node = childNode.Node, Range = childNode.Node.NodeType == MarkupNode.MarkupNodeType.Text ? TrimRange(childNode.Range) : childNode.Range }).ToList();
				childNodes = childNodes.Where(childNode => (childNode.Node.NodeType != MarkupNode.MarkupNodeType.Text) || (childNode.Range.HasSelection)).ToList();
			}

			if (findAttr != null)
				childNodes = childNodes.Where(childNode => childNode.Node.HasAttribute(findAttr.Attribute, findAttr.Value)).ToList();

			var ranges = childNodes.Select(childNode => childNode.Range).ToList();

			return ranges;
		}

		bool MatchesType(MarkupNode rootNode, Range range, MarkupNode.MarkupNodeType type)
		{
			var node = GetInnerMostNode(rootNode, range);
			if (node == null)
				return false;
			return node.NodeType == type;
		}

		bool MatchesFindAttribute(MarkupNode rootNode, Range range, FindMarkupAttribute.Result result)
		{
			var node = GetInnerMostNode(rootNode, range);
			if (node == null)
				return false;
			return node.HasAttribute(result.Attribute, result.Value);
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

		internal FindMarkupAttribute.Result Command_Markup_ChildrenDescendents_ByName_Dialog()
		{
			return FindMarkupAttribute.Run(UIHelper.FindParent<Window>(this));
		}

		internal void Command_Markup_ChildrenAndDescendants(MarkupNode.MarkupNodeList list, MarkupNode.MarkupNodeType type = MarkupNode.MarkupNodeType.All, bool trimWhitespace = true, FindMarkupAttribute.Result findAttr = null)
		{
			var docNode = GetHTMLNode();
			Selections.Replace(Selections.SelectMany(range => MarkupGetChildrenAndDescendants(docNode, range, list, type, trimWhitespace, findAttr)).ToList());
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

		internal void Command_Markup_Select_Type(MarkupNode.MarkupNodeType type)
		{
			var docNode = GetHTMLNode();
			Selections.Replace(Selections.Where(range => MatchesType(docNode, range, type)).ToList());
		}

		internal void Command_Markup_Select_ByAttribute(FindMarkupAttribute.Result result)
		{
			var docNode = GetHTMLNode();
			Selections.Replace(Selections.Where(range => MatchesFindAttribute(docNode, range, result)).ToList());
		}
	}
}
