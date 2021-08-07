using System;
using System.Collections.Generic;
using System.Linq;
using NeoEdit.Common;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Enums;
using NeoEdit.Common.Parsing;
using NeoEdit.Editor.Content;

namespace NeoEdit.Editor
{
	partial class NEFile
	{
		void ContentReplaceSelections(IEnumerable<ParserBase> nodes)
		{
			nodes = nodes.NonNull().Distinct().OrderBy(node => node.Start).ThenBy(node => node.End);
			var sels = new List<NERange>();
			var prevNode = default(ParserBase);
			using (var e = nodes.GetEnumerator())
				while (true)
					if (e.MoveNext())
					{
						if (prevNode != null)
						{
							var overlap = e.Current.Start < prevNode.End;
							sels.Add(new NERange(prevNode.Start, overlap ? prevNode.Start : prevNode.End));
						}

						prevNode = e.Current;
					}
					else
					{
						if (prevNode != null)
							sels.Add(new NERange(prevNode.Start, prevNode.End));
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
			if ((!previousData.Match(Text.GetString())) || (previousType != ContentType))
			{
				previousRoot = Parser.Parse(Text.GetString(), ContentType, StrictParsing);
				previousData.SetValue(Text.GetString());
				previousType = ContentType;
			}
			return previousRoot;
		}

		void Execute__Content_Type_SetFromExtension() => ContentType = ParserExtensions.GetParserType(FileName);

		void Execute__Content_Type_None__Content_Type_Balanced__Content_Type_Columns__Content_Type_CPlusPlus__Content_Type_CSharp__Content_Type_CSV__Content_Type_ExactColumns__Content_Type_HTML__Content_Type_JSON__Content_Type_SQL__Content_Type_TSV__Content_Type_XML(ParserType contentType) => ContentType = contentType;

		void Execute__Content_HighlightSyntax() => HighlightSyntax = state.MultiStatus != true;

		void Execute__Content_StrictParsing()
		{
			StrictParsing = state.MultiStatus != true;
			previousData.Invalidate();
		}

		void Execute__Content_Reformat()
		{
			var root = RootNode();
			var str = Parser.Reformat(root, Text.GetString(), ContentType);
			Replace(new List<NERange> { NERange.FromIndex(0, Text.Length) }, new List<string> { str });
		}

		void Execute__Content_Comment() => ReplaceSelections(Selections.Select(range => Parser.Comment(ContentType, Text, range)).ToList());

		void Execute__Content_Uncomment() => ReplaceSelections(Selections.Select(range => Parser.Uncomment(ContentType, Text, range)).ToList());

		void Execute__Content_Copy() => Clipboard = GetSelectionNodes().Select(node => Text.GetString(node.Start, node.Length)).ToList();

		void Execute__Content_TogglePosition()
		{
			var nodes = GetSelectionNodes();
			var allAtBeginning = nodes.Select((node, index) => Selections[index].Cursor == node.Start).All();
			Selections = nodes.Select((node, index) => MoveCursor(Selections[index], allAtBeginning ? node.End : node.Start, state.ShiftDown)).ToList();
		}

		void Execute__Content_Current() => ContentReplaceSelections(GetSelectionNodes());

		void Execute__Content_Parent() => ContentReplaceSelections(GetSelectionNodes().Select(node => node.Parent ?? node));

		static void Configure__Content_Ancestor() => state.Configuration = state.NEWindow.neWindowUI.RunDialog_Configure_Content_Various_WithAttribute(state.NEWindow.Focused.GetSelectionNodes().SelectMany(node => node.Parents()).Distinct().ToList());

		void Execute__Content_Ancestor()
		{
			var result = state.Configuration as Configuration_Content_Various_WithAttribute;
			ContentReplaceSelections(GetSelectionNodes().SelectMany(node => node.Parents()).Where(child => child.HasAttr(result.Attribute, result.Regex, result.Invert)));
		}

		static void Configure__Content_Attributes() => state.Configuration = state.NEWindow.neWindowUI.RunDialog_Configure_Content_Attributes(state.NEWindow.Focused.GetSelectionNodes());

		void Execute__Content_Attributes()
		{
			var result = state.Configuration as Configuration_Content_Attributes;
			ContentReplaceSelections(GetSelectionNodes().SelectMany(node => node.GetAttrs(result.Attribute, result.FirstOnly)));
		}

		static void Configure__Content_WithAttribute() => state.Configuration = state.NEWindow.neWindowUI.RunDialog_Configure_Content_Various_WithAttribute(state.NEWindow.Focused.GetSelectionNodes());

		void Execute__Content_WithAttribute()
		{
			var result = state.Configuration as Configuration_Content_Various_WithAttribute;
			ContentReplaceSelections(GetSelectionNodes().Where(child => child.HasAttr(result.Attribute, result.Regex, result.Invert)));
		}

		void Execute__Content_Children_Children() => ContentReplaceSelections(GetSelectionNodes().SelectMany(node => node.Children()));

		void Execute__Content_Children_SelfAndChildren() => ContentReplaceSelections(GetSelectionNodes().SelectMany(node => node.SelfAndChildren()));

		void Execute__Content_Children_First() => ContentReplaceSelections(GetSelectionNodes().Select(node => node.Children().FirstOrDefault()));

		static void Configure__Content_Children_WithAttribute() => state.Configuration = state.NEWindow.neWindowUI.RunDialog_Configure_Content_Various_WithAttribute(state.NEWindow.Focused.GetSelectionNodes().SelectMany(node => node.Children()).Distinct().ToList());

		void Execute__Content_Children_WithAttribute()
		{
			var result = state.Configuration as Configuration_Content_Various_WithAttribute;
			ContentReplaceSelections(GetSelectionNodes().SelectMany(node => node.Children()).Where(child => child.HasAttr(result.Attribute, result.Regex, result.Invert)));
		}

		void Execute__Content_Descendants_Descendants() => ContentReplaceSelections(GetSelectionNodes().SelectMany(node => node.Descendants()));

		void Execute__Content_Descendants_SelfAndDescendants() => ContentReplaceSelections(GetSelectionNodes().SelectMany(node => node.SelfAndDescendants()));

		void Execute__Content_Descendants_First() => ContentReplaceSelections(GetSelectionNodes().Select(node => node.Descendants().FirstOrDefault()));

		static void Configure__Content_Descendants_WithAttribute() => state.Configuration = state.NEWindow.neWindowUI.RunDialog_Configure_Content_Various_WithAttribute(state.NEWindow.Focused.GetSelectionNodes().SelectMany(node => node.Descendants()).Distinct().ToList());

		void Execute__Content_Descendants_WithAttribute()
		{
			var result = state.Configuration as Configuration_Content_Various_WithAttribute;
			ContentReplaceSelections(GetSelectionNodes().SelectMany(node => node.Descendants()).Where(child => child.HasAttr(result.Attribute, result.Regex, result.Invert)));
		}

		void Execute__Content_Navigate_Up__Content_Navigate_Down__Content_Navigate_Left__Content_Navigate_Right__Content_Navigate_Home__Content_Navigate_End__Content_Navigate_Pgup__Content_Navigate_Pgdn__Content_Navigate_Row__Content_Navigate_Column(ParserNode.ParserNavigationDirectionEnum direction, bool shiftDown)
		{
			if (ContentType == ParserType.None)
			{
				switch (direction)
				{
					case ParserNode.ParserNavigationDirectionEnum.Left: Execute__Edit_Navigate_AllLeft(); break;
					case ParserNode.ParserNavigationDirectionEnum.Right: Execute__Edit_Navigate_AllRight(); break;
				}
			}
			else
				ContentReplaceSelections(GetSelectionNodes().SelectMany(node => node.Navigate(direction, shiftDown, KeepSelections)));
		}

		void Execute__Content_KeepSelections() => KeepSelections = state.MultiStatus != true;
	}
}
