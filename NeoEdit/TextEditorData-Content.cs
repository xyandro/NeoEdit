using System;
using System.Collections.Generic;
using System.Linq;
using NeoEdit.Program.Content;
using NeoEdit.Program.Dialogs;
using NeoEdit.Program.Parsing;

namespace NeoEdit.Program
{
	partial class TextEditorData
	{
		void ContentReplaceSelections(IEnumerable<ParserBase> nodes)
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
							sels.Add(new Range(overlap ? prevNode.Start : prevNode.End, prevNode.Start));
						}

						prevNode = e.Current;
					}
					else
					{
						if (prevNode != null)
							sels.Add(new Range(prevNode.End, prevNode.Start));
						break;
					}

			SetSelections(sels);
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

		ParserNode RootNode()
		{
			if ((!previousData.Match(Text)) || (previousType != ContentType))
			{
				previousRoot = Parser.Parse(Text.GetString(), ContentType, StrictParsing);
				previousData.SetValue(Text);
				previousType = ContentType;
			}
			return previousRoot;
		}

		void Command_Content_Type_SetFromExtension() => ContentType = ParserExtensions.GetParserType(FileName);

		void Command_Content_Type(ParserType contentType) => ContentType = contentType;

		void Command_Content_HighlightSyntax(bool? multiStatus) => HighlightSyntax = multiStatus == false;

		void Command_Content_StrictParsing(bool? multiStatus)
		{
			StrictParsing = multiStatus == false;
			previousData.Invalidate();
		}

		void Command_Content_Reformat()
		{
			var root = RootNode();
			var str = Parser.Reformat(root, Text.GetString(), ContentType);
			Replace(new List<Range> { Range.FromIndex(0, Text.Length) }, new List<string> { str });
		}

		void Command_Content_Comment() => ReplaceSelections(Selections.Select(range => Parser.Comment(ContentType, Text, TextView, range)).ToList());

		void Command_Content_Uncomment() => ReplaceSelections(Selections.Select(range => Parser.Uncomment(ContentType, Text, TextView, range)).ToList());

		void Command_Content_TogglePosition(bool shiftDown)
		{
			var nodes = GetSelectionNodes();
			var allAtBeginning = nodes.Select((node, index) => Selections[index].Cursor == node.Start).All();
			SetSelections(nodes.Select((node, index) => MoveCursor(Selections[index], allAtBeginning ? node.End : node.Start, shiftDown)).ToList());
		}

		void Command_Content_Current() => ContentReplaceSelections(GetSelectionNodes());

		void Command_Content_Parent() => ContentReplaceSelections(GetSelectionNodes().Select(node => node.Parent ?? node));

		ContentAttributeDialog.Result Command_Content_Ancestor_Dialog() => ContentAttributeDialog.Run(TabsParent, GetSelectionNodes().SelectMany(node => node.Parents()).Distinct().ToList());

		void Command_Content_Ancestor(ContentAttributeDialog.Result result) => ContentReplaceSelections(GetSelectionNodes().SelectMany(node => node.Parents()).Where(child => child.HasAttr(result.Attribute, result.Regex, result.Invert)));

		ContentAttributesDialog.Result Command_Content_Attributes_Dialog() => ContentAttributesDialog.Run(TabsParent, GetSelectionNodes());

		void Command_Content_Attributes(ContentAttributesDialog.Result result) => ContentReplaceSelections(GetSelectionNodes().SelectMany(node => node.GetAttrs(result.Attribute, result.FirstOnly)));

		ContentAttributeDialog.Result Command_Content_WithAttribute_Dialog() => ContentAttributeDialog.Run(TabsParent, GetSelectionNodes());

		void Command_Content_WithAttribute(ContentAttributeDialog.Result result) => ContentReplaceSelections(GetSelectionNodes().Where(child => child.HasAttr(result.Attribute, result.Regex, result.Invert)));

		void Command_Content_Children_Children() => ContentReplaceSelections(GetSelectionNodes().SelectMany(node => node.Children()));

		void Command_Content_Children_SelfAndChildren() => ContentReplaceSelections(GetSelectionNodes().SelectMany(node => node.SelfAndChildren()));

		void Command_Content_Children_First() => ContentReplaceSelections(GetSelectionNodes().Select(node => node.Children().FirstOrDefault()));

		ContentAttributeDialog.Result Command_Content_Children_WithAttribute_Dialog() => ContentAttributeDialog.Run(TabsParent, GetSelectionNodes().SelectMany(node => node.Children()).Distinct().ToList());

		void Command_Content_Children_WithAttribute(ContentAttributeDialog.Result result) => ContentReplaceSelections(GetSelectionNodes().SelectMany(node => node.Children()).Where(child => child.HasAttr(result.Attribute, result.Regex, result.Invert)));

		void Command_Content_Descendants_Descendants() => ContentReplaceSelections(GetSelectionNodes().SelectMany(node => node.Descendants()));

		void Command_Content_Descendants_SelfAndDescendants() => ContentReplaceSelections(GetSelectionNodes().SelectMany(node => node.SelfAndDescendants()));

		void Command_Content_Descendants_First() => ContentReplaceSelections(GetSelectionNodes().Select(node => node.Descendants().FirstOrDefault()));

		ContentAttributeDialog.Result Command_Content_Descendants_WithAttribute_Dialog() => ContentAttributeDialog.Run(TabsParent, GetSelectionNodes().SelectMany(node => node.Descendants()).Distinct().ToList());

		void Command_Content_Descendants_WithAttribute(ContentAttributeDialog.Result result) => ContentReplaceSelections(GetSelectionNodes().SelectMany(node => node.Descendants()).Where(child => child.HasAttr(result.Attribute, result.Regex, result.Invert)));

		void Command_Content_Navigate(ParserNode.ParserNavigationDirectionEnum direction, bool shiftDown)
		{
			if (ContentType == ParserType.None)
			{
				switch (direction)
				{
					case ParserNode.ParserNavigationDirectionEnum.Left: Command_Edit_Navigate_AllLeft(shiftDown); break;
					case ParserNode.ParserNavigationDirectionEnum.Right: Command_Edit_Navigate_AllRight(shiftDown); break;
				}
			}
			else
				ContentReplaceSelections(GetSelectionNodes().SelectMany(node => node.Navigate(direction, shiftDown, KeepSelections)));
		}

		void Command_Content_KeepSelections(bool? multiStatus) => KeepSelections = multiStatus != true;
	}
}
