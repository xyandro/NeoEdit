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
				if (match.Index != start)
					yield return Range.FromIndex(range.Start + start, match.Index - start);
				if (result.IncludeResults)
					yield return Range.FromIndex(range.Start + match.Index, match.Length);
				start = match.Index + match.Length;
			}
			if (str.Length != start)
				yield return Range.FromIndex(range.Start + start, str.Length - start);
		}

		void Command_Select_All() => Selections.Replace(FullRange);

		void Command_Select_Nothing() => Selections.Clear();

		LimitDialog.Result Command_Select_Limit_Dialog() => LimitDialog.Run(WindowParent, Selections.Count, GetVariables());

		void Command_Select_Limit(LimitDialog.Result result)
		{
			var variables = GetVariables();
			var firstSel = new NEExpression(result.FirstSel).EvaluateRow<int>(variables);
			var selMult = new NEExpression(result.SelMult).EvaluateRow<int>(variables);
			var numSels = new NEExpression(result.NumSels).EvaluateRow<int>(variables);

			IEnumerable<Range> retval = Selections;

			retval = retval.Skip(firstSel - 1);
			if (result.JoinSels)
				retval = retval.Batch(selMult).Select(batch => new Range(batch.Last().End, batch.First().Start));
			else
				retval = retval.EveryNth(selMult);
			retval = retval.Take(numSels);
			Selections.Replace(retval.ToList());
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
				if (hasLine[line])
					lines.Add(line);

			Selections.Replace(lines.AsParallel().AsOrdered().Select(line => Range.FromIndex(Data.GetOffset(line, 0), Data.GetLineLength(line) + (includeEndings ? Data.GetEndingLength(line) : 0))).ToList());
		}

		void Command_Select_Rectangle() => Selections.Replace(Selections.AsParallel().AsOrdered().SelectMany(range => SelectRectangle(range)).ToList());

		void Command_Select_Invert()
		{
			var start = new[] { 0 }.Concat(Selections.Select(sel => sel.End));
			var end = Selections.Select(sel => sel.Start).Concat(new[] { Data.NumChars });
			Selections.Replace(Enumerable.Zip(start, end, (startPos, endPos) => new Range(endPos, startPos)).Where(range => (range.HasSelection) || ((range.Start != 0) && (range.Start != Data.NumChars))).ToList());
		}

		void Command_Select_Join()
		{
			var sels = Selections.ToList();
			var ctr = 0;
			while (ctr < sels.Count - 1)
			{
				if (sels[ctr].End == sels[ctr + 1].Start)
				{
					sels[ctr] = new Range(sels[ctr + 1].End, sels[ctr].Start);
					sels.RemoveAt(ctr + 1);
				}
				else
					++ctr;
			}
			Selections.Replace(sels);
		}

		void Command_Select_Empty(bool include) => Selections.Replace(Selections.Where(range => range.HasSelection != include).ToList());

		void Command_Select_ToggleOpenClose(bool shiftDown)
		{
			Selections.Replace(Selections.AsParallel().AsOrdered().Select(range =>
			{
				var newPos = Data.GetOppositeBracket(range.Cursor);
				if (newPos == -1)
					return range;

				return MoveCursor(range, newPos, shiftDown);
			}).ToList());
		}

		void Command_Select_Unique() => Selections.Replace(Selections.AsParallel().AsOrdered().Distinct(range => GetString(range)).ToList());

		void Command_Select_Duplicates() => Selections.Replace(Selections.AsParallel().AsOrdered().Duplicate(range => GetString(range)).ToList());

		void Command_Select_RepeatedLines() => Selections.Replace(Selections.AsParallel().AsOrdered().SelectMany(range => FindRepetitions(range)).ToList());

		CountDialog.Result Command_Select_ByCount_Dialog() => CountDialog.Run(WindowParent);

		void Command_Select_ByCount(CountDialog.Result result)
		{
			var strs = Selections.Select((range, index) => Tuple.Create(GetString(range), index)).ToList();
			var counts = new Dictionary<string, int>(result.CaseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase);
			foreach (var tuple in strs)
			{
				if (!counts.ContainsKey(tuple.Item1))
					counts[tuple.Item1] = 0;
				++counts[tuple.Item1];
			}
			strs = strs.Where(tuple => (counts[tuple.Item1] >= result.MinCount) && (counts[tuple.Item1] <= result.MaxCount)).ToList();
			Selections.Replace(strs.Select(tuple => Selections[tuple.Item2]).ToList());
		}

		SelectSplitDialog.Result Command_Select_Split_Dialog() => SelectSplitDialog.Run(WindowParent);

		void Command_Select_Split(SelectSplitDialog.Result result) => Selections.Replace(Selections.AsParallel().AsOrdered().SelectMany(range => SelectSplit(range, result)).ToList());

		void Command_Select_Regions() => Selections.Replace(Regions);

		void Command_Select_FindResults()
		{
			Selections.Replace(Searches);
			Searches.Clear();
		}

		void Command_Select_Selection_First()
		{
			CurrentSelection = 0;
			EnsureVisible();
			canvasRenderTimer.Start();
		}

		void Command_Select_Selection_CenterVertically() => EnsureVisible(true);

		void Command_Select_Selection_Center() => EnsureVisible(true, true);

		void Command_Select_Selection_ToggleAnchor() => Selections.Replace(Selections.Select(range => new Range(range.Anchor, range.Cursor)).ToList());

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
			CurrentSelection = Math.Max(0, Math.Min(CurrentSelection, Selections.Count - 1));
			Selections.Replace(Selections[CurrentSelection]);
			CurrentSelection = 0;
		}

		void Command_Select_Selection_Remove()
		{
			if (!Selections.Any())
				return;
			Selections.RemoveAt(CurrentSelection);
		}
	}
}
