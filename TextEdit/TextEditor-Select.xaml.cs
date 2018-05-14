using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using NeoEdit.Common;
using NeoEdit.Common.Expressions;
using NeoEdit.Common.Parsing;
using NeoEdit.TextEdit.Dialogs;

namespace NeoEdit.TextEdit
{
	partial class TextEditor
	{
		IEnumerable<Range> FindRepetitions(Range inputRange)
		{
			var startLine = Data.GetOffsetLine(inputRange.Start);
			var endLine = Data.GetOffsetLine(inputRange.End);
			var lineRanges = Enumerable.Range(startLine, endLine - startLine + 1).Select(line => new Range(Math.Max(inputRange.Start, Data.GetOffset(line, 0)), Math.Min(inputRange.End, Data.GetOffset(line, Data.GetLineLength(line))))).ToList();
			if ((lineRanges.Count >= 2) && (!lineRanges[lineRanges.Count - 1].HasSelection))
				lineRanges.RemoveAt(lineRanges.Count - 1);
			var lineStrs = lineRanges.Select(range => GetString(range)).ToList();
			var lines = Enumerable.Range(1, lineStrs.Count).MaxBy(x => GetRepetitionScore(lineStrs, x));
			for (var ctr = 0; ctr < lineRanges.Count; ctr += lines)
				yield return new Range(lineRanges[ctr + lines - 1].End, lineRanges[ctr].Start);
		}

		int GetRepetitionScore(List<string> data, int lines)
		{
			var count = data.Count / lines;
			if (count * lines != data.Count)
				return 0;

			var score = 0;
			for (var repetition = 1; repetition < count; ++repetition)
				for (var index = 0; index < lines; ++index)
					score += LCS.GetLCS(data[index], data[repetition * lines + index]).Count(val => val[0] == LCS.MatchType.Match);
			return score;
		}

		string RepeatsValue(bool caseSensitive, string input) => caseSensitive ? input : input?.ToLowerInvariant();

		IEnumerable<Range> SelectRectangle(Range range)
		{
			var startLine = Data.GetOffsetLine(range.Start);
			var endLine = Data.GetOffsetLine(range.End);
			if (startLine == endLine)
			{
				yield return range;
				yield break;
			}
			var startIndex = Data.GetOffsetIndex(range.Start, startLine);
			var endIndex = Data.GetOffsetIndex(range.End, endLine);
			for (var line = startLine; line <= endLine; ++line)
			{
				var length = Data.GetLineLength(line);
				var lineStartOffset = Data.GetOffset(line, Math.Min(length, startIndex));
				var lineEndOffset = Data.GetOffset(line, Math.Min(length, endIndex));
				yield return new Range(lineEndOffset, lineStartOffset);
			}
		}

		IEnumerable<Range> SelectSplit(Range range, SelectSplitDialog.Result result)
		{
			var str = GetString(range);
			var start = 0;
			foreach (Match match in result.Regex.Matches(str))
			{
				if ((!result.ExcludeEmpty) || (match.Index != start))
					yield return Range.FromIndex(range.Start + start, match.Index - start);
				if (result.IncludeResults)
					yield return Range.FromIndex(range.Start + match.Index, match.Length);
				start = match.Index + match.Length;
			}
			if ((!result.ExcludeEmpty) || (str.Length != start))
				yield return Range.FromIndex(range.Start + start, str.Length - start);
		}

		void Command_Select_All() => SetSelections(new List<Range> { FullRange });

		void Command_Select_Nothing() => SetSelections(new List<Range>());

		SelectLimitDialog.Result Command_Select_Limit_Dialog() => SelectLimitDialog.Run(WindowParent, GetVariables());

		void Command_Select_Limit(SelectLimitDialog.Result result)
		{
			var variables = GetVariables();
			var firstSelection = new NEExpression(result.FirstSelection).Evaluate<int>(variables);
			var everyNth = new NEExpression(result.EveryNth).Evaluate<int>(variables);
			var takeCount = new NEExpression(result.TakeCount).Evaluate<int>(variables);
			var numSels = new NEExpression(result.NumSelections).Evaluate<int>(variables);

			var sels = Selections.Skip(firstSelection - 1);
			if (result.JoinSelections)
				sels = sels.Batch(everyNth).Select(batch => batch.Take(takeCount)).Select(batch => new Range(batch.Last().End, batch.First().Start));
			else
				sels = sels.EveryNth(everyNth, takeCount);
			sels = sels.Take(numSels);

			SetSelections(sels.ToList());
		}

		void Command_Select_Lines(bool includeEndings)
		{
			var lineSets = Selections.AsParallel().Select(range => new { start = Data.GetOffsetLine(range.Start), end = Data.GetOffsetLine(Math.Max(range.Start, range.End - 1)) }).ToList();

			var hasLine = new bool[Data.NumLines];
			foreach (var set in lineSets)
				for (var ctr = set.start; ctr <= set.end; ++ctr)
					hasLine[ctr] = true;

			var lines = new List<int>();
			for (var line = 0; line < hasLine.Length; ++line)
				if ((hasLine[line]) && (!Data.IsDiffGapLine(line)))
					lines.Add(line);

			SetSelections(lines.AsParallel().AsOrdered().Select(line => Range.FromIndex(Data.GetOffset(line, 0), Data.GetLineLength(line) + (includeEndings ? Data.GetEndingLength(line) : 0))).ToList());
		}

		void Command_Select_Rectangle() => SetSelections(Selections.AsParallel().AsOrdered().SelectMany(range => SelectRectangle(range)).ToList());

		void Command_Select_Invert()
		{
			var start = new[] { 0 }.Concat(Selections.Select(sel => sel.End));
			var end = Selections.Select(sel => sel.Start).Concat(new[] { Data.NumChars });
			SetSelections(Enumerable.Zip(start, end, (startPos, endPos) => new Range(endPos, startPos)).Where(range => (range.HasSelection) || ((range.Start != 0) && (range.Start != Data.NumChars))).ToList());
		}

		void Command_Select_Join()
		{
			var sels = new List<Range>();
			var start = 0;
			while (start < Selections.Count)
			{
				var end = start;
				while ((end + 1 < Selections.Count) && (Selections[end].End == Selections[end + 1].Start))
					++end;
				sels.Add(new Range(Selections[end].End, Selections[start].Start));
				start = end + 1;
			}
			SetSelections(sels);
		}

		void Command_Select_Empty(bool include) => SetSelections(Selections.Where(range => range.HasSelection != include).ToList());

		void Command_Select_ToggleOpenClose(bool shiftDown)
		{
			SetSelections(Selections.AsParallel().AsOrdered().Select(range =>
			{
				var newPos = Data.GetOppositeBracket(range.Cursor);
				if (newPos == -1)
					return range;

				return MoveCursor(range, newPos, shiftDown);
			}).ToList());
		}

		void Command_Select_Repeats_Unique(bool caseSensitive) => SetSelections(Selections.AsParallel().AsOrdered().Distinct(range => RepeatsValue(caseSensitive, GetString(range))).ToList());

		void Command_Select_Repeats_Duplicates(bool caseSensitive) => SetSelections(Selections.AsParallel().AsOrdered().Duplicate(range => RepeatsValue(caseSensitive, GetString(range))).ToList());

		void Command_Select_Repeats_MatchPrevious(bool caseSensitive) => SetSelections(Selections.AsParallel().AsOrdered().Match(range => RepeatsValue(caseSensitive, GetString(range))).ToList());

		void Command_Select_Repeats_NonMatchPrevious(bool caseSensitive) => SetSelections(Selections.AsParallel().AsOrdered().NonMatch(range => RepeatsValue(caseSensitive, GetString(range))).ToList());

		void Command_Select_Repeats_RepeatedLines() => SetSelections(Selections.AsParallel().AsOrdered().SelectMany(range => FindRepetitions(range)).ToList());

		SelectByCountDialog.Result Command_Select_Repeats_ByCount_Dialog() => SelectByCountDialog.Run(WindowParent);

		void Command_Select_Repeats_ByCount(SelectByCountDialog.Result result)
		{
			var strs = Selections.Select((range, index) => Tuple.Create(GetString(range), index)).ToList();
			var counts = new Dictionary<string, int>(result.CaseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase);
			foreach (var tuple in strs)
			{
				if (!counts.ContainsKey(tuple.Item1))
					counts[tuple.Item1] = 0;
				++counts[tuple.Item1];
			}
			strs = strs.Where(tuple => ((!result.MinCount.HasValue) || (counts[tuple.Item1] >= result.MinCount)) && ((!result.MaxCount.HasValue) || (counts[tuple.Item1] <= result.MaxCount))).ToList();
			SetSelections(strs.Select(tuple => Selections[tuple.Item2]).ToList());
		}

		SelectSplitDialog.Result Command_Select_Split_Dialog() => SelectSplitDialog.Run(WindowParent);

		void Command_Select_Split(SelectSplitDialog.Result result) => SetSelections(Selections.AsParallel().AsOrdered().SelectMany(range => SelectSplit(range, result)).ToList());

		void Command_Select_Selection_First()
		{
			CurrentSelection = 0;
			EnsureVisible();
			canvasRenderTimer.Start();
		}

		void Command_Select_Selection_CenterVertically() => EnsureVisible(true);

		void Command_Select_Selection_Center() => EnsureVisible(true, true);

		void Command_Select_Selection_ToggleAnchor() => SetSelections(Selections.Select(range => new Range(range.Anchor, range.Cursor)).ToList());

		void Command_Select_Selection_NextPrevious(bool next)
		{
			var offset = next ? 1 : -1;
			CurrentSelection += offset;
			if (CurrentSelection < 0)
				CurrentSelection = Selections.Count - 1;
			if (CurrentSelection >= Selections.Count)
				CurrentSelection = 0;
			EnsureVisible();
			canvasRenderTimer.Start();
		}

		void Command_Select_Selection_Single()
		{
			if (!Selections.Any())
				return;
			SetSelections(new List<Range> { Selections[CurrentSelection] });
			CurrentSelection = 0;
		}

		void Command_Select_Selection_Remove()
		{
			SetSelections(Selections.Where((sel, index) => index != CurrentSelection).ToList());
			CurrentSelection = Math.Max(0, Math.Min(CurrentSelection, Selections.Count - 1));
		}

		void Command_Select_Selection_RemoveBeforeCurrent()
		{
			SetSelections(Selections.Where((sel, index) => index >= CurrentSelection).ToList());
			CurrentSelection = 0;
		}

		void Command_Select_Selection_RemoveAfterCurrent()
		{
			SetSelections(Selections.Where((sel, index) => index <= CurrentSelection).ToList());
		}
	}
}
