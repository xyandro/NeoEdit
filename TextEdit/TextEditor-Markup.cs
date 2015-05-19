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
		MarkupNode HTMLRoot()
		{
			var allRange = new Range(BeginOffset(), EndOffset());
			var data = GetString(allRange);
			var mn = MarkupNode.FromHTML(data, BeginOffset());
			if (data.Substring(mn.StartOuterPosition, mn.OuterLength) != data)
				throw new Exception("Invalid data found; please validate.");
			return mn;
		}

		List<MarkupNode> GetSelectionMarkupNodes(MarkupNode.MarkupNodeType type = MarkupNode.MarkupNodeType.All)
		{
			var nodes = HTMLRoot().List(MarkupNode.MarkupNodeList.SelfAndDescendants).Where(node => type.HasFlag(node.NodeType)).ToList();
			var location = nodes.GroupBy(node => node.StartOuterPosition).ToDictionary(group => group.Key, group => group.Last());

			var result = new List<MarkupNode>();
			foreach (var range in Selections)
			{
				MarkupNode found = null;
				if (location.ContainsKey(range.Start))
					found = location[range.Start];
				else
				{
					var inRangeNodes = nodes.Where(node => (range.Start >= node.StartOuterPosition) && (range.End <= node.EndOuterPosition)).ToList();
					var maxDepth = inRangeNodes.Max(node => node.Depth);
					inRangeNodes = inRangeNodes.Where(node => node.Depth == maxDepth).ToList();
					found = inRangeNodes.Where(child => child.NodeType == MarkupNode.MarkupNodeType.Element).LastOrDefault();
					if (found == null)
						found = inRangeNodes.LastOrDefault();
				}
				if (found == null)
					throw new Exception("No node found");
				result.Add(found);
			}
			return result;
		}

		List<Range> MarkupGetChildrenAndDescendants(MarkupNode node, MarkupNode.MarkupNodeList list, MarkupNode.MarkupNodeType type, bool trimWhitespace, FindMarkupAttribute.Result findAttr)
		{
			var childNodes = node.List(list).Select(childNode => new { Node = childNode, Range = childNode.RangeOuter }).ToList();
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
			var nodes = GetSelectionMarkupNodes(MarkupNode.MarkupNodeType.Element | MarkupNode.MarkupNodeType.Comment);
			var allAtBeginning = nodes.Select((node, index) => Selections[index].Cursor == node.StartOuterPosition).All(b => b);
			Selections.Replace(nodes.Select((node, index) => MoveCursor(Selections[index], allAtBeginning ? node.EndOuterPosition : node.StartOuterPosition, shiftDown)).ToList());
		}

		internal void Command_Markup_Parent()
		{
			Selections.Replace(GetSelectionMarkupNodes().Select(node => node.Parent).Where(node => node != null).Select(node => node.RangeOuterStart).ToList());
		}

		internal FindMarkupAttribute.Result Command_Markup_ChildrenDescendents_ByAttribute_Dialog()
		{
			return FindMarkupAttribute.Run(UIHelper.FindParent<Window>(this));
		}

		internal void Command_Markup_ChildrenAndDescendants(MarkupNode.MarkupNodeList list, MarkupNode.MarkupNodeType type = MarkupNode.MarkupNodeType.All, bool trimWhitespace = true, FindMarkupAttribute.Result findAttr = null)
		{
			Selections.Replace(GetSelectionMarkupNodes().SelectMany(node => MarkupGetChildrenAndDescendants(node, list, type, trimWhitespace, findAttr)).ToList());
		}

		internal void Command_Markup_OuterTag()
		{
			Selections.Replace(GetSelectionMarkupNodes().Select(node => node.RangeOuterFull).ToList());
		}

		internal void Command_Markup_InnerTag()
		{
			Selections.Replace(GetSelectionMarkupNodes().Select(node => node.RangeInnerFull).ToList());
		}

		internal void Command_Markup_Select_Type(MarkupNode.MarkupNodeType type)
		{
			Selections.Replace(GetSelectionMarkupNodes().Where(node => node.NodeType == type).Select(node => node.RangeOuterStart).ToList());
		}

		internal void Command_Markup_Select_ByAttribute(FindMarkupAttribute.Result result)
		{
			Selections.Replace(GetSelectionMarkupNodes().Where(node => node.HasAttribute(result.Attribute, result.Value)).Select(node => node.RangeOuterStart).ToList());
		}
	}
}
