using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using NeoEdit.Common;
using NeoEdit.GUI.Common;
using NeoEdit.TextEdit.Dialogs;
using HtmlAgilityPack;

namespace NeoEdit.TextEdit
{
	public partial class TextEditor
	{
		HtmlNode GetHTMLNode()
		{
			var data = Data.GetString(0, Data.NumChars);
			var doc = new HtmlDocument();
			doc.LoadHtml(data);
			if (doc.DocumentNode.OuterHtml != data)
				throw new Exception("Invalid data found; please validate.");
			return doc.DocumentNode;
		}

		HtmlNode GetInnerMostNode(HtmlNode node, Range range)
		{
			foreach (var child in node.ChildNodes.Reverse())
			{
				var result = GetInnerMostNode(child, range);
				if (result != null)
					return result;
			}
			if ((range.Start >= node.StreamPosition) && (range.End <= node.StreamPosition + node.OuterHtml.Length))
				return node;
			return null;
		}

		Range GetParent(HtmlNode node, Range range)
		{
			var innerMost = GetInnerMostNode(node, range);
			if (innerMost != null)
				innerMost = innerMost.ParentNode;
			if (innerMost == null)
				return range;
			return Range.FromIndex(innerMost.StreamPosition, innerMost.OuterHtml.Length);
		}

		List<Range> GetChildren(HtmlNode node, Range range, bool allChildren)
		{
			var innerMost = GetInnerMostNode(node, range);
			if (innerMost == null)
				return new List<Range>();

			var children = innerMost.ChildNodes.ToList();
			if ((!allChildren) && (children.Any(child => child.NodeType == HtmlNodeType.Element)))
				children = children.Where(child => (child.NodeType != HtmlNodeType.Text) || (!String.IsNullOrWhiteSpace(child.InnerText))).ToList();

			return children.Select(child => Range.FromIndex(child.StreamPosition, child.OuterHtml.Length)).ToList();
		}

		Range GetOuterHtml(HtmlNode node, Range range)
		{
			var innerMost = GetInnerMostNode(node, range);
			if (innerMost == null)
				return range;
			return Range.FromIndex(innerMost.StreamPosition, innerMost.OuterHtml.Length);
		}

		Range GetInnerHtml(HtmlNode node, Range range)
		{
			var innerMost = GetInnerMostNode(node, range);
			if (innerMost == null)
				return range;
			if (!innerMost.HasChildNodes)
				return Range.FromIndex(innerMost.StreamPosition, innerMost.OuterHtml.Length);
			return Range.FromIndex(innerMost.FirstChild.StreamPosition, innerMost.InnerHtml.Length);
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

		internal void Command_Markup_GetParent()
		{
			var docNode = GetHTMLNode();
			Selections.Replace(Selections.Select(range => GetParent(docNode, range)).ToList());
		}

		internal void Command_Markup_GetChildren(bool allChildren)
		{
			var docNode = GetHTMLNode();
			Selections.Replace(Selections.SelectMany(range => GetChildren(docNode, range, allChildren)).ToList());
		}

		internal void Command_Markup_GetOuterTag()
		{
			var docNode = GetHTMLNode();
			Selections.Replace(Selections.Select(range => GetOuterHtml(docNode, range)).ToList());
		}

		internal void Command_Markup_GetInnerTag()
		{
			var docNode = GetHTMLNode();
			Selections.Replace(Selections.Select(range => GetInnerHtml(docNode, range)).ToList());
		}
	}
}
