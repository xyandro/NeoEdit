using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using NeoEdit.TextEdit;
using NeoEdit.TextEdit.Dialogs;

namespace NeoEdit.TextEdit
{
	partial class TextEditor
	{
		List<int> GetValues(string str, int delta)
		{
			var values = str.Split('-').Select(x => int.Parse(x) + delta).ToList();
			if (values.Count > 2)
				throw new Exception("Invalid format");
			return values;
		}

		int GetOffset(int offset, GotoType gotoType, int value)
		{
			switch (gotoType)
			{
				case GotoType.Line: return Data.GetOffset(Math.Max(0, Math.Min(Data.GetNonDiffLine(value), Data.NumLines - 1)), 0);
				case GotoType.Column:
					{
						var line = Data.GetOffsetLine(offset);
						var index = Data.GetIndexFromColumn(line, Math.Max(0, value), true);
						return Data.GetOffset(line, index);
					}
				case GotoType.Index:
					{
						var line = Data.GetOffsetLine(offset);
						value = Math.Max(0, Math.Min(value, Data.GetLineLength(line)));
						return Data.GetOffset(line, value);
					}
				case GotoType.Position: return Math.Max(BeginOffset, Math.Min(value, EndOffset));
				default: throw new Exception("Invalid GotoType");
			}
		}

		Range GetRange(Range range, GotoType gotoType, List<int> values, bool selecting)
		{
			var offsets = values.Select(value => GetOffset(range.Cursor, gotoType, value)).ToList();
			if (offsets.Count >= 2)
				return new Range(offsets[0], offsets[1]);
			else if (selecting)
				return new Range(offsets[0], range.Anchor);
			else
				return Range.FromIndex(offsets[0], 0);
		}

		PositionGotoDialog.Result Command_Position_Goto_Dialog(GotoType gotoType)
		{
			int line = 1, column = 1, index = 1, position = 0;
			var range = Selections.FirstOrDefault();
			if (range != null)
			{
				line = Data.GetOffsetLine(range.Start) + 1;
				index = Data.GetOffsetIndex(range.Start, line - 1) + 1;
				column = Data.GetColumnFromIndex(line - 1, index - 1) + 1;
				position = range.Start;
			}
			int startValue;
			switch (gotoType)
			{
				case GotoType.Line: startValue = Data.GetDiffLine(line - 1) + 1; break;
				case GotoType.Column: startValue = column; break;
				case GotoType.Index: startValue = index; break;
				case GotoType.Position: startValue = position; break;
				default: throw new ArgumentException("GotoType invalid");
			}
			return PositionGotoDialog.Run(TabsParent, gotoType, startValue, GetVariables());
		}

		void Command_Position_Goto(GotoType gotoType, bool selecting, PositionGotoDialog.Result result)
		{
			var delta = gotoType == GotoType.Position ? 0 : -1;
			var values = GetVariableExpressionResults<string>(result.Expression).Select(value => GetValues(value, delta)).ToList();
			if (!values.Any())
				return;

			var sels = Selections.ToList();

			if ((sels.Count == 0) && (gotoType == GotoType.Line))
				sels.Add(BeginRange);
			if (sels.Count == 1)
				sels = sels.Resize(values.Count, sels[0]).ToList();
			if (values.Count == 1)
				values = values.Expand(sels.Count, values[0]).ToList();
			if (values.Count != sels.Count)
				throw new Exception("Expression count doesn't match selection count");

			SetSelections(sels.AsParallel().AsOrdered().Select((range, ctr) => GetRange(range, gotoType, values[ctr], selecting)).ToList());
		}

		void Command_Position_Goto_FilesLines()
		{
			var strs = GetSelectionStrings();
			var startPos = strs.Select(str => str.LastIndexOf("(")).ToList();
			if ((strs.Any(str => string.IsNullOrWhiteSpace(str))) || (startPos.Any(val => val == -1)) || (strs.Any(str => str[str.Length - 1] != ')')))
				throw new Exception("Format: FileName(Line)");
			var files = strs.Select((str, index) => str.Substring(0, startPos[index]).Trim()).ToList();
			var lines = strs.Select((str, index) => int.Parse(str.Substring(startPos[index] + 1, str.Length - startPos[index] - 2))).ToList();
			var data = files.Zip(lines, (file, line) => new { file, line }).GroupBy(obj => obj.file).ToDictionary(group => group.Key, group => group.Select(obj => obj.line).ToList());
			foreach (var pair in data)
			{
				var textEditor = new TextEditor(pair.Key);
				textEditor.SetSelections(pair.Value.Select(line => new Range(textEditor.Data.GetOffset(line - 1, 0))).ToList());
				TabsParent.AddTab(textEditor);
			}
		}

		void Command_Position_Copy(GotoType gotoType)
		{
			var starts = new Dictionary<GotoType, List<int>>();
			var ends = new Dictionary<GotoType, List<int>>();

			var count = Selections.Count;
			starts[GotoType.Position] = Selections.Select(range => range.Start).ToList();
			ends[GotoType.Position] = Selections.Select(range => range.End).ToList();

			if ((gotoType == GotoType.Line) || (gotoType == GotoType.Column) || (gotoType == GotoType.Index))
			{

				starts[GotoType.Line] = starts[GotoType.Position].Select(pos => Data.GetOffsetLine(pos)).ToList();
				ends[GotoType.Line] = ends[GotoType.Position].Select(pos => Data.GetOffsetLine(pos)).ToList();

				if ((gotoType == GotoType.Column) || (gotoType == GotoType.Index))
				{
					starts[GotoType.Index] = Enumerable.Range(0, count).Select(x => Data.GetOffsetIndex(starts[GotoType.Position][x], starts[GotoType.Line][x])).ToList();
					ends[GotoType.Index] = Enumerable.Range(0, count).Select(x => Data.GetOffsetIndex(ends[GotoType.Position][x], ends[GotoType.Line][x])).ToList();

					if (gotoType == GotoType.Column)
					{
						starts[GotoType.Column] = Enumerable.Range(0, count).Select(x => Data.GetColumnFromIndex(starts[GotoType.Line][x], starts[GotoType.Index][x])).ToList();
						ends[GotoType.Column] = Enumerable.Range(0, count).Select(x => Data.GetColumnFromIndex(ends[GotoType.Line][x], ends[GotoType.Index][x])).ToList();
					}
				}
			}

			var delta = gotoType == GotoType.Position ? 0 : 1;
			SetClipboardStrings(starts[gotoType].Zip(ends[gotoType], (start, end) => $"{start + delta}{(start != end ? $"-{end + delta}" : "")}"));
		}
	}
}
