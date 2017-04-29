using System;
using System.Data;
using System.Linq;
using NeoEdit.Common;
using NeoEdit.TextEdit.Dialogs;

namespace NeoEdit.TextEdit
{
	partial class TextEditor
	{
		PositionGotoDialog.Result Command_Position_Goto_Dialog(GotoType gotoType)
		{
			int line = 1, index = 1, position = 0;
			var range = Selections.FirstOrDefault();
			if (range != null)
			{
				line = Data.GetOffsetLine(range.Start) + 1;
				index = Data.GetOffsetIndex(range.Start, line - 1) + 1;
				position = range.Start;
			}
			int startValue;
			switch (gotoType)
			{
				case GotoType.Line: startValue = Data.GetDiffLine(line - 1) + 1; break;
				case GotoType.Column: startValue = index; break;
				case GotoType.Position: startValue = position; break;
				default: throw new ArgumentException("GotoType invalid");
			}
			return PositionGotoDialog.Run(WindowParent, gotoType, startValue, GetVariables());
		}

		void Command_Position_Goto(GotoType gotoType, bool selecting, PositionGotoDialog.Result result)
		{
			var offsets = GetVariableExpressionResults<int>(result.Expression);
			if (!offsets.Any())
				return;

			var sels = Selections.ToList();

			if ((sels.Count == 0) && (gotoType == GotoType.Line))
				sels.Add(BeginRange);
			if (sels.Count == 1)
				sels = sels.Resize(offsets.Count, sels[0]).ToList();
			if (offsets.Count == 1)
				offsets = offsets.Expand(sels.Count, offsets[0]).ToList();
			if (offsets.Count != sels.Count)
				throw new Exception("Expression count doesn't match selection count");

			if (gotoType != GotoType.Position)
				offsets = offsets.Select(ofs => ofs - 1).ToList();

			switch (gotoType)
			{
				case GotoType.Line:
					Selections.Replace(sels.AsParallel().AsOrdered().Select((range, ctr) => MoveCursor(range, Data.GetNonDiffLine(offsets[ctr]), 0, selecting, false, false)).ToList());
					break;
				case GotoType.Column:
					Selections.Replace(sels.AsParallel().AsOrdered().Select((range, ctr) => MoveCursor(range, 0, offsets[ctr], selecting, true, false)).ToList());
					break;
				case GotoType.Position:
					Selections.Replace(sels.AsParallel().AsOrdered().Select((range, ctr) => MoveCursor(range, offsets[ctr], selecting)).ToList());
					break;
			}
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
				textEditor.Selections.Replace(pair.Value.Select(line => new Range(textEditor.Data.GetOffset(line - 1, 0))));
				TabsParent.CreateTab(textEditor);
			}
		}

		void Command_Position_Copy(GotoType gotoType)
		{
			if (gotoType == GotoType.Position)
			{
				SetClipboardStrings(Selections.Select(range => $"{range.Start}{(range.HasSelection ? $"-{range.End}" : "")}"));
				return;
			}

			var starts = Selections.Select(range => range.Start).ToList();
			var lines = starts.Select(pos => Data.GetOffsetLine(pos)).ToList();
			if (gotoType == GotoType.Line)
			{
				SetClipboardStrings(lines.Select(pos => (Data.GetDiffLine(pos) + 1).ToString()));
				return;
			}

			var indexes = starts.Select((pos, line) => Data.GetOffsetIndex(pos, lines[line])).ToList();
			SetClipboardStrings(indexes.Select(pos => (pos + 1).ToString()));
		}
	}
}
