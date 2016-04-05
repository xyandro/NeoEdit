using System;
using System.Collections.Generic;
using System.Linq;
using NeoEdit.Common.Parsing;
using NeoEdit.TextEdit.Content;
using NeoEdit.TextEdit.Dialogs;

namespace NeoEdit.TextEdit
{
	partial class TextEditor
	{
		CacheValue previousData = new CacheValue();
		Parser.ParserType previousType;
		ParserNode previousRoot;

		ParserNode RootNode()
		{
			if ((!previousData.Match(Data.Data)) || (previousType != ContentType))
			{
				previousRoot = Parser.Parse(Data.Data, ContentType);
				previousData.SetValue(Data.Data);
				previousType = ContentType;
			}
			return previousRoot;
		}

		List<ParserNode> GetSelectionNodes()
		{
			var nodes = RootNode().GetAllNodes().Where(node => (node.HasLocation) && (!node.IsAttr)).ToList();
			var fullLocation = new Dictionary<int, Dictionary<int, ParserNode>>();
			var startLocation = new Dictionary<int, ParserNode>();
			foreach (var node in nodes)
			{
				if (!fullLocation.ContainsKey(node.Start))
					fullLocation[node.Start] = new Dictionary<int, ParserNode>();
				fullLocation[node.Start][node.End] = node;

				startLocation[node.Start] = node;
			}

			var result = new List<ParserNode>();
			foreach (var range in Selections)
			{
				ParserNode found = null;
				if ((fullLocation.ContainsKey(range.Start)) && (fullLocation[range.Start].ContainsKey(range.End)))
					found = fullLocation[range.Start][range.End];
				else if (startLocation.ContainsKey(range.Cursor))
					found = startLocation[range.Cursor];
				else
					found = nodes.Where(node => (range.Start >= node.Start) && (range.End <= node.End)).OrderBy(node => node.Depth).LastOrDefault();
				if (found == null)
					throw new Exception("No node found");
				result.Add(found);
			}
			return result;
		}

		List<ParserNode> ContentGetList(ParserNode node, ParserNode.ParserNodeListType list, bool first, FindContentAttributeDialog.Result findAttr)
		{
			var nodes = node.List(list).Where(child => child.HasLocation);

			if (findAttr != null)
				nodes = nodes.Where(child => child.HasAttr(findAttr.Attribute, findAttr.Regex, findAttr.Invert));

			if (first)
				nodes = nodes.Take(1);

			var nodeList = nodes.ToList();
			return nodeList.Any() ? nodeList : new List<ParserNode> { node };
		}

		void ContentReplaceSelections(List<ParserNode> nodes)
		{
			nodes = nodes.Distinct().OrderBy(node => node.Start).ToList();
			var overlap = false;
			ParserNode last = null;
			foreach (var node in nodes)
			{
				if (last != null)
				{
					if (node.Start < last.End)
					{
						overlap = true;
						break;
					}
				}
				last = node;
			}
			Selections.Replace(nodes.Select(node => new Range(node.Start, overlap ? node.Start : node.End)).ToList());
		}

		internal void Command_Content_Type_SetFromExtension() => ContentType = Parser.GetParserType(FileName);

		internal void Command_Content_Type(Parser.ParserType contentType) => ContentType = contentType;

		internal void Command_Content_Reformat()
		{
			var root = RootNode();
			var str = Parser.Reformat(root, Data.Data, ContentType);
			Replace(new List<Range> { FullRange }, new List<string> { str });
		}

		internal void Command_Content_Comment() => ReplaceSelections(Selections.Select(range => Parser.Comment(ContentType, Data, range)).ToList());

		internal void Command_Content_Uncomment() => ReplaceSelections(Selections.Select(range => Parser.Uncomment(ContentType, Data, range)).ToList());

		internal void Command_Content_TogglePosition(bool shiftDown)
		{
			var nodes = GetSelectionNodes();
			var allAtBeginning = nodes.Select((node, index) => Selections[index].Cursor == node.Start).All(b => b);
			Selections.Replace(nodes.Select((node, index) => MoveCursor(Selections[index], allAtBeginning ? node.End : node.Start, shiftDown)).ToList());
		}

		internal void Command_Content_Current() => ContentReplaceSelections(GetSelectionNodes().ToList());

		internal void Command_Content_Parent() => ContentReplaceSelections(GetSelectionNodes().Select(node => node.Parent ?? node).Distinct().ToList());

		internal FindContentAttributeDialog.Result Command_Content_FindByAttribute_Dialog(ParserNode.ParserNodeListType list) => FindContentAttributeDialog.Run(WindowParent, GetSelectionNodes().SelectMany(node => node.List(list)).Distinct().ToList());

		internal void Command_Content_List(ParserNode.ParserNodeListType list, bool first = false, FindContentAttributeDialog.Result findAttr = null) => ContentReplaceSelections(GetSelectionNodes().SelectMany(node => ContentGetList(node, list, first, findAttr)).ToList());

		internal void Command_Content_NextPrevious(bool next)
		{
			var offset = next ? 1 : -1;
			var nodes = GetSelectionNodes();
			ContentReplaceSelections(nodes.Select(node =>
			{
				if (node.Parent == null)
					return node;

				var children = node.Parent.List(ParserNode.ParserNodeListType.Children | ParserNode.ParserNodeListType.Attributes).Where(child => child.HasLocation).ToList();
				if (!children.Any())
					return node;

				var index = children.IndexOf(node);
				if (index == -1)
					return node;

				var found = index;
				do
				{
					found += offset;
					if (found < 0)
						found = children.Count - 1;
					if (found >= children.Count)
						found = 0;
					if (children[found].IsAttr == node.IsAttr)
						break;
				} while (index != found);
				return children[found];
			}).ToList());
		}

		internal void Command_Content_Select_ByAttribute(FindContentAttributeDialog.Result result) => ContentReplaceSelections(GetSelectionNodes().Where(node => node.HasAttr(result.Attribute, result.Regex, result.Invert)).ToList());

		internal void Command_Content_Select_Topmost()
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

		internal void Command_Content_Select_MaxTopmost()
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

		internal SelectContentAttributeDialog.Result Command_Content_Attributes_ByAttribute_Dialog() => SelectContentAttributeDialog.Run(WindowParent, GetSelectionNodes());

		internal void Command_Content_Attributes_ByAttribute(SelectContentAttributeDialog.Result result) => ContentReplaceSelections(GetSelectionNodes().SelectMany(node => node.GetAttrs(result.Attribute, result.FirstOnly)).ToList());
	}
}
