using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using NeoEdit.Common;
using NeoEdit.MenuPosition.Dialogs;

namespace NeoEdit.MenuPosition
{
	public static class PositionFunctions
	{
		static int GetOffset(ITextEditor te, int offset, GotoType gotoType, int value)
		{
			switch (gotoType)
			{
				case GotoType.Line: return te.Data.GetOffset(Math.Max(0, Math.Min(te.Data.GetNonDiffLine(value), te.Data.NumLines - 1)), 0);
				case GotoType.Column:
					{
						var line = te.Data.GetOffsetLine(offset);
						var index = te.Data.GetIndexFromColumn(line, Math.Max(0, value), true);
						return te.Data.GetOffset(line, index);
					}
				case GotoType.Index:
					{
						var line = te.Data.GetOffsetLine(offset);
						value = Math.Max(0, Math.Min(value, te.Data.GetLineLength(line)));
						return te.Data.GetOffset(line, value);
					}
				case GotoType.Position: return Math.Max(te.BeginOffset, Math.Min(value, te.EndOffset));
				default: throw new Exception("Invalid GotoType");
			}
		}

		static Range GetRange(ITextEditor te, Range range, GotoType gotoType, List<int> values, bool selecting)
		{
			var offsets = values.Select(value => GetOffset(te, range.Cursor, gotoType, value)).ToList();
			if (offsets.Count >= 2)
				return new Range(offsets[0], offsets[1]);
			else if (selecting)
				return new Range(offsets[0], range.Anchor);
			else
				return Range.FromIndex(offsets[0], 0);
		}

		static List<int> GetValues(string str, int delta)
		{
			var values = str.Split('-').Select(x => int.Parse(x) + delta).ToList();
			if (values.Count > 2)
				throw new Exception("Invalid format");
			return values;
		}

		static public PositionGotoDialog.Result Command_Position_Goto_Dialog(ITextEditor te, GotoType gotoType)
		{
			int line = 1, column = 1, index = 1, position = 0;
			var range = te.Selections.FirstOrDefault();
			if (range != null)
			{
				line = te.Data.GetOffsetLine(range.Start) + 1;
				index = te.Data.GetOffsetIndex(range.Start, line - 1) + 1;
				column = te.Data.GetColumnFromIndex(line - 1, index - 1) + 1;
				position = range.Start;
			}
			int startValue;
			switch (gotoType)
			{
				case GotoType.Line: startValue = te.Data.GetDiffLine(line - 1) + 1; break;
				case GotoType.Column: startValue = column; break;
				case GotoType.Index: startValue = index; break;
				case GotoType.Position: startValue = position; break;
				default: throw new ArgumentException("GotoType invalid");
			}
			return PositionGotoDialog.Run(te.WindowParent, gotoType, startValue, te.GetVariables());
		}

		static public void Command_Position_Goto(ITextEditor te, GotoType gotoType, bool selecting, PositionGotoDialog.Result result)
		{
			var delta = gotoType == GotoType.Position ? 0 : -1;
			var values = te.GetVariableExpressionResults<string>(result.Expression).Select(value => GetValues(value, delta)).ToList();
			if (!values.Any())
				return;

			var sels = te.Selections.ToList();

			if ((sels.Count == 0) && (gotoType == GotoType.Line))
				sels.Add(te.BeginRange);
			if (sels.Count == 1)
				sels = sels.Resize(values.Count, sels[0]).ToList();
			if (values.Count == 1)
				values = values.Expand(sels.Count, values[0]).ToList();
			if (values.Count != sels.Count)
				throw new Exception("Expression count doesn't match selection count");

			te.SetSelections(sels.AsParallel().AsOrdered().Select((range, ctr) => GetRange(te, range, gotoType, values[ctr], selecting)).ToList());
		}

		static public void Command_Position_Goto_FilesLines(ITextEditor te)
		{
			var strs = te.GetSelectionStrings();
			var startPos = strs.Select(str => str.LastIndexOf("(")).ToList();
			if ((strs.Any(str => string.IsNullOrWhiteSpace(str))) || (startPos.Any(val => val == -1)) || (strs.Any(str => str[str.Length - 1] != ')')))
				throw new Exception("Format: FileName(Line)");
			var files = strs.Select((str, index) => str.Substring(0, startPos[index]).Trim()).ToList();
			var lines = strs.Select((str, index) => int.Parse(str.Substring(startPos[index] + 1, str.Length - startPos[index] - 2))).ToList();
			var data = files.Zip(lines, (file, line) => new { file, line }).GroupBy(obj => obj.file).ToDictionary(group => group.Key, group => group.Select(obj => obj.line).ToList());
			foreach (var pair in data)
			{
				var textEditor = te.TabsParent.Add(pair.Key);
				textEditor.SetSelections(pair.Value.Select(line => new Range(textEditor.Data.GetOffset(line - 1, 0))).ToList());
			}
		}

		static public void Command_Position_Copy(ITextEditor te, GotoType gotoType)
		{
			var starts = new Dictionary<GotoType, List<int>>();
			var ends = new Dictionary<GotoType, List<int>>();

			var count = te.Selections.Count;
			starts[GotoType.Position] = te.Selections.Select(range => range.Start).ToList();
			ends[GotoType.Position] = te.Selections.Select(range => range.End).ToList();

			if ((gotoType == GotoType.Line) || (gotoType == GotoType.Column) || (gotoType == GotoType.Index))
			{
				starts[GotoType.Line] = starts[GotoType.Position].Select(pos => te.Data.GetOffsetLine(pos)).ToList();
				ends[GotoType.Line] = ends[GotoType.Position].Select(pos => te.Data.GetOffsetLine(pos)).ToList();

				if ((gotoType == GotoType.Column) || (gotoType == GotoType.Index))
				{
					starts[GotoType.Index] = Enumerable.Range(0, count).Select(x => te.Data.GetOffsetIndex(starts[GotoType.Position][x], starts[GotoType.Line][x])).ToList();
					ends[GotoType.Index] = Enumerable.Range(0, count).Select(x => te.Data.GetOffsetIndex(ends[GotoType.Position][x], ends[GotoType.Line][x])).ToList();

					if (gotoType == GotoType.Column)
					{
						starts[GotoType.Column] = Enumerable.Range(0, count).Select(x => te.Data.GetColumnFromIndex(starts[GotoType.Line][x], starts[GotoType.Index][x])).ToList();
						ends[GotoType.Column] = Enumerable.Range(0, count).Select(x => te.Data.GetColumnFromIndex(ends[GotoType.Line][x], ends[GotoType.Index][x])).ToList();
					}
				}
			}

			var delta = gotoType == GotoType.Position ? 0 : 1;
			te.SetClipboardStrings(starts[gotoType].Zip(ends[gotoType], (start, end) => $"{start + delta}{(start != end ? $"-{end + delta}" : "")}"));
		}
	}
}
