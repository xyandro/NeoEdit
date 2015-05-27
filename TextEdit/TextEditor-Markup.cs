﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using NeoEdit.GUI.Controls;
using NeoEdit.Parsing;
using NeoEdit.TextEdit.Dialogs;

namespace NeoEdit.TextEdit
{
	public partial class TextEditor
	{
		ParserNode HTMLRoot()
		{
			var allRange = new Range(BeginOffset(), EndOffset());
			var data = GetString(allRange);
			return HTML.ParseHTML(data, allRange.Start);
		}

		List<ParserNode> GetSelectionMarkupNodes()
		{
			var doc = HTMLRoot();
			var nodes = doc.List(ParserNode.ParserNodeListType.SelfAndDescendants).ToList();
			var location = nodes.GroupBy(node => node.Start).ToDictionary(group => group.Key, group => group.Last());

			var result = new List<ParserNode>();
			foreach (var range in Selections)
			{
				ParserNode found = null;
				if (location.ContainsKey(range.Cursor))
					found = location[range.Cursor];
				else
				{
					var inRangeNodes = nodes.Where(node => (range.Start >= node.Start) && (range.End <= node.End)).ToList();
					var maxDepth = inRangeNodes.Max(node => node.Depth);
					inRangeNodes = inRangeNodes.Where(node => node.Depth == maxDepth).ToList();
					found = inRangeNodes.LastOrDefault();
				}
				if (found == null)
					throw new Exception("No node found");
				result.Add(found);
			}
			return result;
		}

		List<Range> MarkupGetList(ParserNode node, ParserNode.ParserNodeListType list, bool first, FindMarkupAttributeDialog.Result findAttr)
		{
			var childNodes = node.List(list).Select(childNode => new { Node = childNode, Range = new Range(childNode.Start) }).ToList();

			if (findAttr != null)
				childNodes = childNodes.Where(childNode => childNode.Node.HasAttr(findAttr.Attribute, findAttr.Regex)).ToList();

			if (first)
				childNodes = childNodes.Take(1).ToList();

			var ranges = childNodes.Select(childNode => childNode.Range).ToList();

			return ranges;
		}

		string CommentMarkup(string str)
		{
			if (String.IsNullOrWhiteSpace(str))
				return str;
			return String.Format("<!--{0}-->", str.Replace("-->", "--><!--"));
		}

		string UncommentMarkup(string str)
		{
			if ((String.IsNullOrWhiteSpace(str)) || (!str.StartsWith("<!--")) || (!str.EndsWith("-->")))
				return str;
			return str.Substring(4, str.Length - 7).Replace("--><!--", "-->");
		}

		internal void Command_Markup_Comment()
		{
			ReplaceSelections(GetSelectionStrings().Select(str => CommentMarkup(str)).ToList());
		}

		internal void Command_Markup_Uncomment()
		{
			ReplaceSelections(GetSelectionStrings().Select(str => UncommentMarkup(str)).ToList());
		}

		internal void Command_Markup_ToggleTagPosition(bool shiftDown)
		{
			var nodes = GetSelectionMarkupNodes();
			var allAtBeginning = nodes.Select((node, index) => Selections[index].Cursor == node.Start).All(b => b);
			Selections.Replace(nodes.Select((node, index) => MoveCursor(Selections[index], allAtBeginning ? node.End : node.Start, shiftDown)).ToList());
		}

		internal void Command_Markup_Parent(bool shiftDown)
		{
			Selections.Replace(GetSelectionMarkupNodes().Select((node, index) => MoveCursor(Selections[index], (node.Parent ?? node).Start, shiftDown)).ToList());
		}

		internal FindMarkupAttributeDialog.Result Command_Markup_ChildrenDescendents_ByAttribute_Dialog()
		{
			return FindMarkupAttributeDialog.Run(UIHelper.FindParent<Window>(this));
		}

		internal void Command_Markup_List(ParserNode.ParserNodeListType list, bool first = false, FindMarkupAttributeDialog.Result findAttr = null)
		{
			var newSels = GetSelectionMarkupNodes().SelectMany(node => MarkupGetList(node, list, first, findAttr)).ToList();
			if (newSels.Any())
				Selections.Replace(newSels);
		}

		internal void Command_Markup_NextPrev(bool next, bool shiftDown)
		{
			var offset = next ? 1 : -1;
			var nodes = GetSelectionMarkupNodes();
			Selections.Replace(nodes.Select((node, idx) =>
			{
				var range = Selections[idx];
				var children = node.Parent.List(ParserNode.ParserNodeListType.Children).ToList();
				if (!children.Any())
					return range;
				var index = children.IndexOf(node);
				index += offset;
				if (index < 0)
					index = children.Count - 1;
				if (index >= children.Count)
					index = 0;
				return MoveCursor(range, children[index].Start, shiftDown);
			}).ToList());
		}

		internal void Command_Markup_Select_ByAttribute(FindMarkupAttributeDialog.Result result)
		{
			Selections.Replace(GetSelectionMarkupNodes().Where(node => node.HasAttr(result.Attribute, result.Regex)).Select(node => new Range(node.Start)).ToList());
		}

		internal void Command_Markup_Select_TopMost()
		{
			var nodes = GetSelectionMarkupNodes();
			var descendants = new HashSet<ParserNode>(nodes.SelectMany(node => node.List(ParserNode.ParserNodeListType.Descendants)));
			Selections.Replace(Selections.Where((range, index) => !descendants.Contains(nodes[index])).ToList());
		}

		internal void Command_Markup_Select_Deepest()
		{
			var nodes = GetSelectionMarkupNodes();
			var parents = new HashSet<ParserNode>(nodes.SelectMany(node => node.List(ParserNode.ParserNodeListType.Parents)));
			Selections.Replace(Selections.Where((range, index) => !parents.Contains(nodes[index])).ToList());
		}

		internal void Command_Markup_Select_MaxTopMost()
		{
			var nodes = GetSelectionMarkupNodes();
			var targetDepth = nodes.Min(node => node.Depth);
			Selections.Replace(Selections.Where((range, index) => nodes[index].Depth == targetDepth).ToList());
		}

		internal void Command_Markup_Select_MaxDeepest()
		{
			var nodes = GetSelectionMarkupNodes();
			var targetDepth = nodes.Max(node => node.Depth);
			Selections.Replace(Selections.Where((range, index) => nodes[index].Depth == targetDepth).ToList());
		}

		internal SelectMarkupAttributeDialog.Result Command_Markup_Select_Attribute_Dialog()
		{
			return SelectMarkupAttributeDialog.Run(UIHelper.FindParent<Window>(this));
		}

		internal void Command_Markup_Select_Attribute(SelectMarkupAttributeDialog.Result result)
		{
			Selections.Replace(GetSelectionMarkupNodes().SelectMany(node => node.GetAttrs(result.Attribute, result.FirstOnly).Select(attr => new Range(attr.Start.Value))).ToList());
		}
	}
}
