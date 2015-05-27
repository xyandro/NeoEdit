using System;
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
		string ContentLastData;
		Parser.ParserType ContentLastType;
		ParserNode ContentLastRoot;
		ParserNode RootNode()
		{
			if ((ContentLastData != Data.Data) || (ContentLastType != ContentType))
			{
				ContentLastData = Data.Data;
				ContentLastType = ContentType;
				ContentLastRoot = Parser.Parse(Data.Data, ContentType);
			}
			return ContentLastRoot;
		}

		List<ParserNode> GetSelectionNodes()
		{
			var doc = RootNode();
			var nodes = doc.List(ParserNode.ParserNodeListType.SelfAttributesAndDescendants).Where(node => node.HasLocation).ToList();
			var location = nodes.GroupBy(node => node.Start).ToDictionary(group => group.Key, group => group.Last());
			location[BeginOffset()] = doc;

			var result = new List<ParserNode>();
			foreach (var range in Selections)
			{
				ParserNode found = null;
				if (location.ContainsKey(range.Cursor))
					found = location[range.Cursor];
				else
				{
					var inRangeNodes = nodes.Where(node => (range.Start >= node.Start) && (range.End <= node.End)).ToList();
					if (inRangeNodes.Any())
					{
						var maxDepth = inRangeNodes.Max(node => node.Depth);
						inRangeNodes = inRangeNodes.Where(node => node.Depth == maxDepth).ToList();
						found = inRangeNodes.LastOrDefault();
					}
				}
				if (found == null)
					throw new Exception("No node found");
				result.Add(found);
			}
			return result;
		}

		List<Range> ContentGetList(ParserNode node, ParserNode.ParserNodeListType list, bool first, FindContentAttributeDialog.Result findAttr)
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

		internal void Command_Content_Comment()
		{
			ReplaceSelections(GetSelectionStrings().Select(str => CommentMarkup(str)).ToList());
		}

		internal void Command_Content_Uncomment()
		{
			ReplaceSelections(GetSelectionStrings().Select(str => UncommentMarkup(str)).ToList());
		}

		internal void Command_Content_TogglePosition(bool shiftDown)
		{
			var nodes = GetSelectionNodes();
			var allAtBeginning = nodes.Select((node, index) => Selections[index].Cursor == node.Start).All(b => b);
			Selections.Replace(nodes.Select((node, index) => MoveCursor(Selections[index], allAtBeginning ? node.End : node.Start, shiftDown)).ToList());
		}

		internal void Command_Content_Parent(bool shiftDown)
		{
			Selections.Replace(GetSelectionNodes().Select((node, index) => MoveCursor(Selections[index], (node.Parent ?? node).Start, shiftDown)).ToList());
		}

		internal FindContentAttributeDialog.Result Command_Content_FindByAttribute_Dialog(ParserNode.ParserNodeListType list)
		{
			return FindContentAttributeDialog.Run(UIHelper.FindParent<Window>(this), GetSelectionNodes().SelectMany(node => node.List(list)).Distinct().ToList());
		}

		internal void Command_Content_List(ParserNode.ParserNodeListType list, bool first = false, FindContentAttributeDialog.Result findAttr = null)
		{
			var newSels = GetSelectionNodes().SelectMany(node => ContentGetList(node, list, first, findAttr)).ToList();
			if (newSels.Any())
				Selections.Replace(newSels);
		}

		internal void Command_Content_NextPrev(bool next, bool shiftDown)
		{
			var offset = next ? 1 : -1;
			var nodes = GetSelectionNodes();
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

		internal void Command_Content_Select_ByAttribute(FindContentAttributeDialog.Result result)
		{
			Selections.Replace(GetSelectionNodes().Where(node => node.HasAttr(result.Attribute, result.Regex)).Select(node => new Range(node.Start)).ToList());
		}

		internal void Command_Content_Select_TopMost()
		{
			var nodes = GetSelectionNodes();
			var descendants = new HashSet<ParserNode>(nodes.SelectMany(node => node.List(ParserNode.ParserNodeListType.Descendants)));
			Selections.Replace(Selections.Where((range, index) => !descendants.Contains(nodes[index])).ToList());
		}

		internal void Command_Content_Select_Deepest()
		{
			var nodes = GetSelectionNodes();
			var parents = new HashSet<ParserNode>(nodes.SelectMany(node => node.List(ParserNode.ParserNodeListType.Parents)));
			Selections.Replace(Selections.Where((range, index) => !parents.Contains(nodes[index])).ToList());
		}

		internal void Command_Content_Select_MaxTopMost()
		{
			var nodes = GetSelectionNodes();
			var targetDepth = nodes.Min(node => node.Depth);
			Selections.Replace(Selections.Where((range, index) => nodes[index].Depth == targetDepth).ToList());
		}

		internal void Command_Content_Select_MaxDeepest()
		{
			var nodes = GetSelectionNodes();
			var targetDepth = nodes.Max(node => node.Depth);
			Selections.Replace(Selections.Where((range, index) => nodes[index].Depth == targetDepth).ToList());
		}

		internal SelectContentAttributeDialog.Result Command_Content_Select_Attribute_Dialog()
		{
			return SelectContentAttributeDialog.Run(UIHelper.FindParent<Window>(this), GetSelectionNodes());
		}

		internal void Command_Content_Select_Attribute(SelectContentAttributeDialog.Result result)
		{
			Selections.Replace(GetSelectionNodes().SelectMany(node => node.GetAttrs(result.Attribute, result.FirstOnly).Select(attr => new Range(attr.Start))).ToList());
		}
	}
}
