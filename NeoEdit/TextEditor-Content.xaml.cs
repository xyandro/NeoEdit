﻿using System;
using System.Collections.Generic;
using System.Linq;
using NeoEdit;
using NeoEdit.Content;
using NeoEdit.Controls;
using NeoEdit.Dialogs;
using NeoEdit.Parsing;

namespace NeoEdit
{
	partial class TextEditor
	{
		[DepProp]
		public bool KeepSelections { get { return UIHelper<TextEditor>.GetPropValue<bool>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public bool HighlightSyntax { get { return UIHelper<TextEditor>.GetPropValue<bool>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public bool StrictParsing { get { return UIHelper<TextEditor>.GetPropValue<bool>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }

		public CacheValue previousData { get; } = new CacheValue();
		public Parser.ParserType previousType { get; set; }
		public ParserNode previousRoot { get; set; }

		static ParserNode RootNode(ITextEditor te)
		{
			if ((!te.previousData.Match(te.Data.Data)) || (te.previousType != te.ContentType))
			{
				te.previousRoot = Parser.Parse(te.Data.Data, te.ContentType, te.StrictParsing);
				te.previousData.SetValue(te.Data.Data);
				te.previousType = te.ContentType;
			}
			return te.previousRoot;
		}

		static void ContentReplaceSelections(ITextEditor te, IEnumerable<ParserBase> nodes)
		{
			nodes = nodes.NonNull().Distinct().OrderBy(node => node.Start).ThenBy(node => node.End);
			var sels = new List<Range>();
			var prevNode = default(ParserBase);
			using (var e = nodes.GetEnumerator())
				while (true)
					if (e.MoveNext())
					{
						if (prevNode != null)
						{
							var overlap = e.Current.Start < prevNode.End;
							sels.Add(new Range(prevNode.Start, overlap ? prevNode.Start : prevNode.End));
						}

						prevNode = e.Current;
					}
					else
					{
						if (prevNode != null)
							sels.Add(new Range(prevNode.Start, prevNode.End));
						break;
					}

			te.SetSelections(sels);
		}

		static List<ParserNode> GetSelectionNodes(ITextEditor te)
		{
			var nodes = RootNode(te).GetAllNodes();
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
			foreach (var range in te.Selections)
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

		static void Command_Content_Type_SetFromExtension(ITextEditor te) => te.ContentType = Parser.GetParserType(te.FileName);

		static void Command_Content_Type(ITextEditor te, Parser.ParserType contentType) => te.ContentType = contentType;

		static void Command_Content_HighlightSyntax(ITextEditor te, bool? multiStatus) => te.HighlightSyntax = multiStatus == false;

		static void Command_Content_StrictParsing(ITextEditor te, bool? multiStatus)
		{
			te.StrictParsing = multiStatus == false;
			te.previousData.Invalidate();
		}

		static void Command_Content_Reformat(ITextEditor te)
		{
			var root = RootNode(te);
			var str = Parser.Reformat(root, te.Data.Data, te.ContentType);
			te.Replace(new List<Range> { te.FullRange }, new List<string> { str });
		}

		static void Command_Content_Comment(ITextEditor te) => te.ReplaceSelections(te.Selections.Select(range => Parser.Comment(te.ContentType, te.Data, range)).ToList());

		static void Command_Content_Uncomment(ITextEditor te) => te.ReplaceSelections(te.Selections.Select(range => Parser.Uncomment(te.ContentType, te.Data, range)).ToList());

		static void Command_Content_TogglePosition(ITextEditor te, bool shiftDown)
		{
			var nodes = GetSelectionNodes(te);
			var allAtBeginning = nodes.Select((node, index) => te.Selections[index].Cursor == node.Start).All();
			te.SetSelections(nodes.Select((node, index) => te.MoveCursor(te.Selections[index], allAtBeginning ? node.End : node.Start, shiftDown)).ToList());
		}

		static void Command_Content_Current(ITextEditor te) => ContentReplaceSelections(te, GetSelectionNodes(te));

		static void Command_Content_Parent(ITextEditor te) => ContentReplaceSelections(te, GetSelectionNodes(te).Select(node => node.Parent ?? node));

		static ContentAttributeDialog.Result Command_Content_Ancestor_Dialog(ITextEditor te) => ContentAttributeDialog.Run(te.WindowParent, GetSelectionNodes(te).SelectMany(node => node.Parents()).Distinct().ToList());

		static void Command_Content_Ancestor(ITextEditor te, ContentAttributeDialog.Result result) => ContentReplaceSelections(te, GetSelectionNodes(te).SelectMany(node => node.Parents()).Where(child => child.HasAttr(result.Attribute, result.Regex, result.Invert)));

		static ContentAttributesDialog.Result Command_Content_Attributes_Dialog(ITextEditor te) => ContentAttributesDialog.Run(te.WindowParent, GetSelectionNodes(te));

		static void Command_Content_Attributes(ITextEditor te, ContentAttributesDialog.Result result) => ContentReplaceSelections(te, GetSelectionNodes(te).SelectMany(node => node.GetAttrs(result.Attribute, result.FirstOnly)));

		static ContentAttributeDialog.Result Command_Content_WithAttribute_Dialog(ITextEditor te) => ContentAttributeDialog.Run(te.WindowParent, GetSelectionNodes(te));

		static void Command_Content_WithAttribute(ITextEditor te, ContentAttributeDialog.Result result) => ContentReplaceSelections(te, GetSelectionNodes(te).Where(child => child.HasAttr(result.Attribute, result.Regex, result.Invert)));

		static void Command_Content_Children_Children(ITextEditor te) => ContentReplaceSelections(te, GetSelectionNodes(te).SelectMany(node => node.Children()));

		static void Command_Content_Children_SelfAndChildren(ITextEditor te) => ContentReplaceSelections(te, GetSelectionNodes(te).SelectMany(node => node.SelfAndChildren()));

		static void Command_Content_Children_First(ITextEditor te) => ContentReplaceSelections(te, GetSelectionNodes(te).Select(node => node.Children().FirstOrDefault()));

		static ContentAttributeDialog.Result Command_Content_Children_WithAttribute_Dialog(ITextEditor te) => ContentAttributeDialog.Run(te.WindowParent, GetSelectionNodes(te).SelectMany(node => node.Children()).Distinct().ToList());

		static void Command_Content_Children_WithAttribute(ITextEditor te, ContentAttributeDialog.Result result) => ContentReplaceSelections(te, GetSelectionNodes(te).SelectMany(node => node.Children()).Where(child => child.HasAttr(result.Attribute, result.Regex, result.Invert)));

		static void Command_Content_Descendants_Descendants(ITextEditor te) => ContentReplaceSelections(te, GetSelectionNodes(te).SelectMany(node => node.Descendants()));

		static void Command_Content_Descendants_SelfAndDescendants(ITextEditor te) => ContentReplaceSelections(te, GetSelectionNodes(te).SelectMany(node => node.SelfAndDescendants()));

		static void Command_Content_Descendants_First(ITextEditor te) => ContentReplaceSelections(te, GetSelectionNodes(te).Select(node => node.Descendants().FirstOrDefault()));

		static ContentAttributeDialog.Result Command_Content_Descendants_WithAttribute_Dialog(ITextEditor te) => ContentAttributeDialog.Run(te.WindowParent, GetSelectionNodes(te).SelectMany(node => node.Descendants()).Distinct().ToList());

		static void Command_Content_Descendants_WithAttribute(ITextEditor te, ContentAttributeDialog.Result result) => ContentReplaceSelections(te, GetSelectionNodes(te).SelectMany(node => node.Descendants()).Where(child => child.HasAttr(result.Attribute, result.Regex, result.Invert)));

		static void Command_Content_Navigate(ITextEditor te, ParserNode.ParserNavigationDirectionEnum direction, bool shiftDown)
		{
			//if (te.ContentType == Parser.ParserType.None)
			//{
			//	switch (direction)
			//	{
			//		case ParserNode.ParserNavigationDirectionEnum.Left: Command_Edit_Navigate_AllLeft(te, shiftDown); break;
			//		case ParserNode.ParserNavigationDirectionEnum.Right: Command_Edit_Navigate_AllRight(te, shiftDown); break;
			//	}
			//}
			//else
			ContentReplaceSelections(te, GetSelectionNodes(te).SelectMany(node => node.Navigate(direction, shiftDown, te.KeepSelections)));
		}

		static void Command_Content_KeepSelections(ITextEditor te, bool? multiStatus) => te.KeepSelections = multiStatus != true;
	}
}
