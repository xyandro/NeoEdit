using System;
using System.Collections.Generic;
using System.Linq;
using NeoEdit.Program.Content;
using NeoEdit.Program.Dialogs;
using NeoEdit.Program.Parsing;

namespace NeoEdit.Program
{
	partial class TextEditor
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

			Selections = sels;
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

		void Execute_Content_Type_SetFromExtension() => ContentType = ParserExtensions.GetParserType(FileName);

		void Execute_Content_Type(ParserType contentType) => ContentType = contentType;

		void Execute_Content_HighlightSyntax() => HighlightSyntax = state.MultiStatus == false;

		void Execute_Content_StrictParsing()
		{
			StrictParsing = state.MultiStatus == false;
			previousData.Invalidate();
		}

		void Execute_Content_Reformat()
		{
			var root = RootNode();
			var str = Parser.Reformat(root, Text.GetString(), ContentType);
			Replace(new List<Range> { Range.FromIndex(0, Text.Length) }, new List<string> { str });
		}

		void Execute_Content_Comment() => ReplaceSelections(Selections.Select(range => Parser.Comment(ContentType, Text, TextView, range)).ToList());

		void Execute_Content_Uncomment() => ReplaceSelections(Selections.Select(range => Parser.Uncomment(ContentType, Text, TextView, range)).ToList());

		void Execute_Content_TogglePosition()
		{
			var nodes = GetSelectionNodes();
			var allAtBeginning = nodes.Select((node, index) => Selections[index].Cursor == node.Start).All();
			Selections = nodes.Select((node, index) => MoveCursor(Selections[index], allAtBeginning ? node.End : node.Start, state.ShiftDown)).ToList();
		}

		void Execute_Content_Current() => ContentReplaceSelections(GetSelectionNodes());

		void Execute_Content_Parent() => ContentReplaceSelections(GetSelectionNodes().Select(node => node.Parent ?? node));

		void ConfigureExecute_Content_Ancestor() => state.ConfigureExecuteData = ContentAttributeDialog.Run(tabsWindow, GetSelectionNodes().SelectMany(node => node.Parents()).Distinct().ToList());

		void Execute_Content_Ancestor()
		{
			var result = state.ConfigureExecuteData as ContentAttributeDialog.Result;
			ContentReplaceSelections(GetSelectionNodes().SelectMany(node => node.Parents()).Where(child => child.HasAttr(result.Attribute, result.Regex, result.Invert)));
		}

		void ConfigureExecute_Content_Attributes() => state.ConfigureExecuteData = ContentAttributesDialog.Run(tabsWindow, GetSelectionNodes());

		void Execute_Content_Attributes()
		{
			var result = state.ConfigureExecuteData as ContentAttributesDialog.Result;
			ContentReplaceSelections(GetSelectionNodes().SelectMany(node => node.GetAttrs(result.Attribute, result.FirstOnly)));
		}

		void ConfigureExecute_Content_WithAttribute() => state.ConfigureExecuteData = ContentAttributeDialog.Run(tabsWindow, GetSelectionNodes());

		void Execute_Content_WithAttribute()
		{
			var result = state.ConfigureExecuteData as ContentAttributeDialog.Result;
			ContentReplaceSelections(GetSelectionNodes().Where(child => child.HasAttr(result.Attribute, result.Regex, result.Invert)));
		}

		void Execute_Content_Children_Children() => ContentReplaceSelections(GetSelectionNodes().SelectMany(node => node.Children()));

		void Execute_Content_Children_SelfAndChildren() => ContentReplaceSelections(GetSelectionNodes().SelectMany(node => node.SelfAndChildren()));

		void Execute_Content_Children_First() => ContentReplaceSelections(GetSelectionNodes().Select(node => node.Children().FirstOrDefault()));

		void ConfigureExecute_Content_Children_WithAttribute() => state.ConfigureExecuteData = ContentAttributeDialog.Run(tabsWindow, GetSelectionNodes().SelectMany(node => node.Children()).Distinct().ToList());

		void Execute_Content_Children_WithAttribute()
		{
			var result = state.ConfigureExecuteData as ContentAttributeDialog.Result;
			ContentReplaceSelections(GetSelectionNodes().SelectMany(node => node.Children()).Where(child => child.HasAttr(result.Attribute, result.Regex, result.Invert)));
		}

		void Execute_Content_Descendants_Descendants() => ContentReplaceSelections(GetSelectionNodes().SelectMany(node => node.Descendants()));

		void Execute_Content_Descendants_SelfAndDescendants() => ContentReplaceSelections(GetSelectionNodes().SelectMany(node => node.SelfAndDescendants()));

		void Execute_Content_Descendants_First() => ContentReplaceSelections(GetSelectionNodes().Select(node => node.Descendants().FirstOrDefault()));

		void ConfigureExecute_Content_Descendants_WithAttribute() => state.ConfigureExecuteData = ContentAttributeDialog.Run(tabsWindow, GetSelectionNodes().SelectMany(node => node.Descendants()).Distinct().ToList());

		void Execute_Content_Descendants_WithAttribute()
		{
			var result = state.ConfigureExecuteData as ContentAttributeDialog.Result;
			ContentReplaceSelections(GetSelectionNodes().SelectMany(node => node.Descendants()).Where(child => child.HasAttr(result.Attribute, result.Regex, result.Invert)));
		}

		void Execute_Content_Navigate(ParserNode.ParserNavigationDirectionEnum direction, bool shiftDown)
		{
			if (ContentType == ParserType.None)
			{
				switch (direction)
				{
					case ParserNode.ParserNavigationDirectionEnum.Left: Execute_Edit_Navigate_AllLeft(); break;
					case ParserNode.ParserNavigationDirectionEnum.Right: Execute_Edit_Navigate_AllRight(); break;
				}
			}
			else
				ContentReplaceSelections(GetSelectionNodes().SelectMany(node => node.Navigate(direction, shiftDown, KeepSelections)));
		}

		void Execute_Content_KeepSelections() => KeepSelections = state.MultiStatus != true;
	}
}
