using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using NeoEdit.Common;
using NeoEdit.Common.Enums;
using NeoEdit.Common.Models;
using NeoEdit.Common.Parsing;
using NeoEdit.Editor.Content;

namespace NeoEdit.Editor
{
	partial class Tab
	{
		IReadOnlyList<NewNode> GetSelectionNodes()
		{
			if (Nodes != null)
				return Nodes;

			var node = RootNode();

			//node.ToXML().Save(@"C:\Dev\NeoEdit\a.xml");

			var result = new OrderedHashSet<NewNode>();
			foreach (var range in Selections)
			{
				while (!node.Range.Contains(range))
					node = node.Parent;

				while (true)
				{
					var child = node.Children.FirstOrDefault(x => x.Range.Contains(range));
					if (child != null)
						node = child;
					else
						break;
				}

				if (!result.Contains(node))
					result.Add(node);
			}

			return result;
		}

		NewNode RootNode()
		{
			if ((!previousData.Match(Text)) || (previousType != ContentType))
			{
				previousRoot = NewParser.Parse(ContentType, Text.GetString(), StrictParsing);
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
			//var root = RootNode();
			//var str = Parser.Reformat(root, Text.GetString(), ContentType);
			//Replace(new List<Range> { Range.FromIndex(0, Text.Length) }, new List<string> { str });
		}

		void Execute_Content_Comment() => ReplaceSelections(Selections.Select(range => Parser.Comment(ContentType, Text, TextView, range)).ToList());

		void Execute_Content_Uncomment() => ReplaceSelections(Selections.Select(range => Parser.Uncomment(ContentType, Text, TextView, range)).ToList());

		void Execute_Content_Copy() => Clipboard = GetSelectionNodes().Select(node => Text.GetString(node.Range.Start, node.Range.Length)).ToList();

		void Execute_Content_TogglePosition()
		{
			var nodes = GetSelectionNodes();
			var allAtBeginning = nodes.Select((node, index) => Selections[index].Cursor == node.Range.Start).All();
			Selections = nodes.Select((node, index) => MoveCursor(Selections[index], allAtBeginning ? node.Range.End : node.Range.Start, state.ShiftDown)).ToList();
		}

		void Execute_Content_Current() => Nodes = GetSelectionNodes();

		void Execute_Content_Parent()
		{
			//ContentReplaceSelections(GetSelectionNodes().Select(node => node.Parent ?? node));
		}

		object Configure_Content_Ancestor()
		{
			return default;
			//return Tabs.TabsWindow.RunContentAttributeDialog(GetSelectionNodes().SelectMany(node => node.Parents()).Distinct().ToList());
		}

		void Execute_Content_Ancestor()
		{
			//var result = state.Configuration as ContentAttributeDialogResult;
			//ContentReplaceSelections(GetSelectionNodes().SelectMany(node => node.Parents()).Where(child => child.HasAttr(result.Attribute, result.Regex, result.Invert)));
		}

		object Configure_Content_Attributes()
		{
			return default;
			//return Tabs.TabsWindow.RunContentAttributesDialog(GetSelectionNodes());
		}

		void Execute_Content_Attributes()
		{
			//var result = state.Configuration as ContentAttributesDialogResult;
			//ContentReplaceSelections(GetSelectionNodes().SelectMany(node => node.GetAttrs(result.Attribute, result.FirstOnly)));
		}

		object Configure_Content_WithAttribute()
		{
			return default;
			//return Tabs.TabsWindow.RunContentAttributeDialog(GetSelectionNodes());
		}

		void Execute_Content_WithAttribute()
		{
			//var result = state.Configuration as ContentAttributeDialogResult;
			//ContentReplaceSelections(GetSelectionNodes().Where(child => child.HasAttr(result.Attribute, result.Regex, result.Invert)));
		}

		void Execute_Content_Children_Children()
		{
			//ContentReplaceSelections(GetSelectionNodes().SelectMany(node => node.Children()));
		}

		void Execute_Content_Children_SelfAndChildren()
		{
			//ContentReplaceSelections(GetSelectionNodes().SelectMany(node => node.SelfAndChildren()));
		}

		void Execute_Content_Children_First()
		{
			//ContentReplaceSelections(GetSelectionNodes().Select(node => node.Children().FirstOrDefault()));
		}

		object Configure_Content_Children_WithAttribute()
		{
			return default;
			//return Tabs.TabsWindow.RunContentAttributeDialog(GetSelectionNodes().SelectMany(node => node.Children()).Distinct().ToList());
		}

		void Execute_Content_Children_WithAttribute()
		{
			//var result = state.Configuration as ContentAttributeDialogResult;
			//ContentReplaceSelections(GetSelectionNodes().SelectMany(node => node.Children()).Where(child => child.HasAttr(result.Attribute, result.Regex, result.Invert)));
		}

		void Execute_Content_Descendants_Descendants()
		{
			//ContentReplaceSelections(GetSelectionNodes().SelectMany(node => node.Descendants()));
		}

		void Execute_Content_Descendants_SelfAndDescendants()
		{
			//ContentReplaceSelections(GetSelectionNodes().SelectMany(node => node.SelfAndDescendants()));
		}

		void Execute_Content_Descendants_First()
		{
			//ContentReplaceSelections(GetSelectionNodes().Select(node => node.Descendants().FirstOrDefault()));
		}

		object Configure_Content_Descendants_WithAttribute()
		{
			return default;
			//return Tabs.TabsWindow.RunContentAttributeDialog(GetSelectionNodes().SelectMany(node => node.Descendants()).Distinct().ToList());
		}

		void Execute_Content_Descendants_WithAttribute()
		{
			//var result = state.Configuration as ContentAttributeDialogResult;
			//ContentReplaceSelections(GetSelectionNodes().SelectMany(node => node.Descendants()).Where(child => child.HasAttr(result.Attribute, result.Regex, result.Invert)));
		}

		void Execute_Content_Navigate(ParserNode.ParserNavigationDirectionEnum direction, bool shiftDown)
		{
			//if (ContentType == ParserType.None)
			//{
			//	switch (direction)
			//	{
			//		case ParserNode.ParserNavigationDirectionEnum.Left: Execute_Edit_Navigate_AllLeft(); break;
			//		case ParserNode.ParserNavigationDirectionEnum.Right: Execute_Edit_Navigate_AllRight(); break;
			//	}
			//}
			//else
			//	ContentReplaceSelections(GetSelectionNodes().SelectMany(node => node.Navigate(direction, shiftDown, KeepSelections)));
		}

		void Execute_Content_KeepSelections() => KeepSelections = state.MultiStatus != true;
	}
}
