using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using NeoEdit.GUI.Controls;
using NeoEdit.TextEdit.Dialogs;

namespace NeoEdit.TextEdit
{
	public partial class TextEditor
	{
		MarkupNode HTMLRoot()
		{
			var allRange = new Range(BeginOffset(), EndOffset());
			var data = GetString(allRange);
			return HTMLParser.ParseHTML(data, allRange.Start);
		}

		List<MarkupNode> GetSelectionMarkupNodes(MarkupNode.MarkupNodeType type = MarkupNode.MarkupNodeType.All)
		{
			var doc = HTMLRoot();
			var nodes = doc.List(MarkupNode.MarkupNodeList.SelfAndDescendants).Where(node => type.HasFlag(node.NodeType)).ToList();
			var location = nodes.GroupBy(node => node.StartOuterPosition).ToDictionary(group => group.Key, group => group.Last());

			var result = new List<MarkupNode>();
			foreach (var range in Selections)
			{
				MarkupNode found = null;
				if (location.ContainsKey(range.Cursor))
					found = location[range.Cursor];
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

		List<Range> MarkupGetChildrenAndDescendants(MarkupNode node, MarkupNode.MarkupNodeList list, MarkupNode.MarkupNodeType type, bool trimWhitespace, bool first, FindMarkupAttributeDialog.Result findAttr)
		{
			var childNodes = node.List(list).Select(childNode => new { Node = childNode, Range = childNode.RangeOuter }).ToList();
			childNodes = childNodes.Where(child => type.HasFlag(child.Node.NodeType)).ToList();

			if (trimWhitespace)
			{
				childNodes = childNodes.Select(childNode => new { Node = childNode.Node, Range = childNode.Node.NodeType == MarkupNode.MarkupNodeType.Text ? TrimRange(childNode.Range) : childNode.Range }).ToList();
				childNodes = childNodes.Where(childNode => (childNode.Node.NodeType != MarkupNode.MarkupNodeType.Text) || (childNode.Range.HasSelection)).ToList();
			}

			if (findAttr != null)
				childNodes = childNodes.Where(childNode => childNode.Node.HasAttribute(findAttr.Attribute, findAttr.Regex)).ToList();

			if (first)
				childNodes = childNodes.Take(1).ToList();

			var ranges = childNodes.Select(childNode => childNode.Range).ToList();

			return ranges;
		}

		internal void Command_Markup_Reformat()
		{
			var allRange = new Range(BeginOffset(), EndOffset());
			var data = GetString(allRange);
			var doc = HTMLParser.ParseHTML(data, allRange.Start);
			Replace(new List<Range> { allRange }, new List<string> { HTMLParser.FormatHTML(doc, data) });
		}

		internal void Command_Markup_ToggleTagPosition(bool shiftDown)
		{
			var nodes = GetSelectionMarkupNodes(~MarkupNode.MarkupNodeType.Text);
			var allAtBeginning = nodes.Select((node, index) => Selections[index].Cursor == node.StartOuterPosition).All(b => b);
			Selections.Replace(nodes.Select((node, index) => MoveCursor(Selections[index], allAtBeginning ? node.EndOuterPosition : node.StartOuterPosition, shiftDown)).ToList());
		}

		internal void Command_Markup_Parent(bool shiftDown)
		{
			Selections.Replace(GetSelectionMarkupNodes().Select((node, index) => MoveCursor(Selections[index], (node.Parent ?? node).StartOuterPosition, shiftDown)).ToList());
		}

		internal FindMarkupAttributeDialog.Result Command_Markup_ChildrenDescendents_ByAttribute_Dialog()
		{
			return FindMarkupAttributeDialog.Run(UIHelper.FindParent<Window>(this));
		}

		internal void Command_Markup_ChildrenAndDescendants(MarkupNode.MarkupNodeList list, MarkupNode.MarkupNodeType type = MarkupNode.MarkupNodeType.All, bool trimWhitespace = true, bool first = false, FindMarkupAttributeDialog.Result findAttr = null)
		{
			var newSels = GetSelectionMarkupNodes().SelectMany(node => MarkupGetChildrenAndDescendants(node, list, type, trimWhitespace, first, findAttr)).ToList();
			if (newSels.Any())
				Selections.Replace(newSels);
		}

		internal void Command_Markup_NextPrev(bool next, bool shiftDown)
		{
			var offset = next ? 1 : -1;
			var nodes = GetSelectionMarkupNodes(~MarkupNode.MarkupNodeType.Text);
			Selections.Replace(nodes.Select((node, idx) =>
			{
				var range = Selections[idx];
				var children = node.Parent.List(MarkupNode.MarkupNodeList.Children).Where(child => child.NodeType != MarkupNode.MarkupNodeType.Text).ToList();
				if (!children.Any())
					return range;
				var index = children.IndexOf(node);
				index += offset;
				if (index < 0)
					index = children.Count - 1;
				if (index >= children.Count)
					index = 0;
				return MoveCursor(range, children[index].StartOuterPosition, shiftDown);
			}).ToList());
		}

		internal void Command_Markup_OuterTag()
		{
			Selections.Replace(GetSelectionMarkupNodes().Select(node => node.RangeOuterFull).ToList());
		}

		internal void Command_Markup_InnerTag()
		{
			Selections.Replace(GetSelectionMarkupNodes().Select(node => TrimRange(node.RangeInnerFull)).ToList());
		}

		internal void Command_Markup_AllInnerTag()
		{
			Selections.Replace(GetSelectionMarkupNodes().Select(node => node.RangeInnerFull).ToList());
		}

		internal void Command_Markup_Select_Type(MarkupNode.MarkupNodeType type)
		{
			Selections.Replace(GetSelectionMarkupNodes().Where(node => node.NodeType == type).Select(node => node.RangeOuterStart).ToList());
		}

		internal void Command_Markup_Select_ByAttribute(FindMarkupAttributeDialog.Result result)
		{
			Selections.Replace(GetSelectionMarkupNodes().Where(node => node.HasAttribute(result.Attribute, result.Regex)).Select(node => node.RangeOuterStart).ToList());
		}

		internal void Command_Markup_Select_TopMost()
		{
			var nodes = GetSelectionMarkupNodes();
			var targetDepth = nodes.Min(node => node.Depth);
			Selections.Replace(Selections.Where((range, index) => nodes[index].Depth == targetDepth).ToList());
		}

		internal void Command_Markup_Select_Deepest()
		{
			var nodes = GetSelectionMarkupNodes();
			var targetDepth = nodes.Max(node => node.Depth);
			Selections.Replace(Selections.Where((range, index) => nodes[index].Depth == targetDepth).ToList());
		}

		internal void Command_Markup_Select_AllTopMost()
		{
			var nodes = GetSelectionMarkupNodes();
			var descendants = new HashSet<MarkupNode>(nodes.SelectMany(node => node.List(MarkupNode.MarkupNodeList.Descendants)));
			Selections.Replace(Selections.Where((range, index) => !descendants.Contains(nodes[index])).ToList());
		}

		internal void Command_Markup_Select_AllDeepest()
		{
			var nodes = GetSelectionMarkupNodes();
			var parents = new HashSet<MarkupNode>(nodes.SelectMany(node => node.List(MarkupNode.MarkupNodeList.Parents)));
			Selections.Replace(Selections.Where((range, index) => !parents.Contains(nodes[index])).ToList());
		}

		internal SelectMarkupAttributeDialog.Result Command_Markup_Select_Attribute_Dialog()
		{
			return SelectMarkupAttributeDialog.Run(UIHelper.FindParent<Window>(this));
		}

		internal void Command_Markup_Select_Attribute(SelectMarkupAttributeDialog.Result result)
		{
			Selections.Replace(GetSelectionMarkupNodes().SelectMany(node => node.GetAttributeRanges(result.Attribute, result.FirstOnly)).ToList());
		}
	}
}
