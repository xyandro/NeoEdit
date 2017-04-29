﻿using System;
using System.Collections.Generic;
using System.Linq;
using NeoEdit.Common;
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

		void ContentReplaceSelections(IEnumerable<ParserBase> nodes)
		{
			nodes = nodes.Where(node => node != null).Distinct().OrderBy(node => node.Start).ToList();
			var overlap = nodes.WithPrev().Any(tuple => tuple.Item2.start < tuple.Item1.end);
			Selections.Replace(nodes.Select(node => new Range(node.Start, overlap ? node.Start : node.End)).ToList());
		}

		List<ParserNode> GetSelectionNodes()
		{
			var nodes = RootNode().GetAllNodes();
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

		void Command_Content_Type_SetFromExtension() => ContentType = Parser.GetParserType(FileName);

		void Command_Content_Type(Parser.ParserType contentType) => ContentType = contentType;

		void Command_Content_Reformat()
		{
			var root = RootNode();
			var str = Parser.Reformat(root, Data.Data, ContentType);
			Replace(new List<Range> { FullRange }, new List<string> { str });
		}

		void Command_Content_Comment() => ReplaceSelections(Selections.Select(range => Parser.Comment(ContentType, Data, range)).ToList());

		void Command_Content_Uncomment() => ReplaceSelections(Selections.Select(range => Parser.Uncomment(ContentType, Data, range)).ToList());

		void Command_Content_TogglePosition(bool shiftDown)
		{
			var nodes = GetSelectionNodes();
			var allAtBeginning = nodes.Select((node, index) => Selections[index].Cursor == node.Start).All();
			Selections.Replace(nodes.Select((node, index) => MoveCursor(Selections[index], allAtBeginning ? node.End : node.Start, shiftDown)).ToList());
		}

		void Command_Content_Current() => ContentReplaceSelections(GetSelectionNodes());

		void Command_Content_Parent() => ContentReplaceSelections(GetSelectionNodes().Select(node => node.Parent ?? node));

		ContentAttributeDialog.Result Command_Content_Ancestor_Dialog() => ContentAttributeDialog.Run(WindowParent, GetSelectionNodes().SelectMany(node => node.Parents()).Distinct().ToList());

		void Command_Content_Ancestor(ContentAttributeDialog.Result result) => ContentReplaceSelections(GetSelectionNodes().SelectMany(node => node.Parents()).Where(child => child.HasAttr(result.Attribute, result.Regex, result.Invert)));

		ContentAttributesDialog.Result Command_Content_Attributes_Dialog() => ContentAttributesDialog.Run(WindowParent, GetSelectionNodes());

		void Command_Content_Attributes(ContentAttributesDialog.Result result) => ContentReplaceSelections(GetSelectionNodes().SelectMany(node => node.GetAttrs(result.Attribute, result.FirstOnly)));

		ContentAttributeDialog.Result Command_Content_WithAttribute_Dialog() => ContentAttributeDialog.Run(WindowParent, GetSelectionNodes());

		void Command_Content_WithAttribute(ContentAttributeDialog.Result result) => ContentReplaceSelections(GetSelectionNodes().Where(child => child.HasAttr(result.Attribute, result.Regex, result.Invert)));

		void Command_Content_Children_Children() => ContentReplaceSelections(GetSelectionNodes().SelectMany(node => node.Children()));

		void Command_Content_Children_SelfAndChildren() => ContentReplaceSelections(GetSelectionNodes().SelectMany(node => node.SelfAndChildren()));

		void Command_Content_Children_First() => ContentReplaceSelections(GetSelectionNodes().Select(node => node.Children().FirstOrDefault()));

		ContentAttributeDialog.Result Command_Content_Children_WithAttribute_Dialog() => ContentAttributeDialog.Run(WindowParent, GetSelectionNodes().SelectMany(node => node.Children()).Distinct().ToList());

		void Command_Content_Children_WithAttribute(ContentAttributeDialog.Result result) => ContentReplaceSelections(GetSelectionNodes().SelectMany(node => node.Children()).Where(child => child.HasAttr(result.Attribute, result.Regex, result.Invert)));

		void Command_Content_Descendants_Descendants() => ContentReplaceSelections(GetSelectionNodes().SelectMany(node => node.Descendants()));

		void Command_Content_Descendants_SelfAndDescendants() => ContentReplaceSelections(GetSelectionNodes().SelectMany(node => node.SelfAndDescendants()));

		void Command_Content_Descendants_First() => ContentReplaceSelections(GetSelectionNodes().Select(node => node.Descendants().FirstOrDefault()));

		ContentAttributeDialog.Result Command_Content_Descendants_WithAttribute_Dialog() => ContentAttributeDialog.Run(WindowParent, GetSelectionNodes().SelectMany(node => node.Descendants()).Distinct().ToList());

		void Command_Content_Descendants_WithAttribute(ContentAttributeDialog.Result result) => ContentReplaceSelections(GetSelectionNodes().SelectMany(node => node.Descendants()).Where(child => child.HasAttr(result.Attribute, result.Regex, result.Invert)));

		void Command_Content_Navigate(ParserNode.ParserNavigationDirectionEnum direction, bool shiftDown) => ContentReplaceSelections(GetSelectionNodes().SelectMany(node => node.Navigate(direction, shiftDown)));
	}
}
