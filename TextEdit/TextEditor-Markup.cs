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
			foreach (var child in node.ChildNodes)
			{
				var result = GetInnerMostNode(child, range);
				if (result != null)
					return result;
			}
			if ((range.Start >= node.StreamPosition) && (range.End <= node.StreamPosition + node.OuterHtml.Length))
				return node;
			return null;
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
