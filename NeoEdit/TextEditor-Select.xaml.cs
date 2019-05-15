using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using NeoEdit;
using NeoEdit.Expressions;
using NeoEdit.Parsing;
using NeoEdit.Dialogs;

namespace NeoEdit
{
	partial class TextEditor
	{
		static IEnumerable<Range> FindRepetitions(ITextEditor te, Range inputRange)
		{
			var startLine = te.Data.GetOffsetLine(inputRange.Start);
			var endLine = te.Data.GetOffsetLine(inputRange.End);
			var lineRanges = Enumerable.Range(startLine, endLine - startLine + 1).Select(line => new Range(Math.Max(inputRange.Start, te.Data.GetOffset(line, 0)), Math.Min(inputRange.End, te.Data.GetOffset(line, te.Data.GetLineLength(line))))).ToList();
			if ((lineRanges.Count >= 2) && (!lineRanges[lineRanges.Count - 1].HasSelection))
				lineRanges.RemoveAt(lineRanges.Count - 1);
			var lineStrs = lineRanges.Select(range => te.GetString(range)).ToList();
			var lines = Enumerable.Range(1, lineStrs.Count).MaxBy(x => GetRepetitionScore(lineStrs, x));
			for (var ctr = 0; ctr < lineRanges.Count; ctr += lines)
				yield return new Range(lineRanges[ctr + lines - 1].End, lineRanges[ctr].Start);
		}

		static int GetRepetitionScore(List<string> data, int lines)
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

		static string RepeatsValue(bool caseSensitive, string input) => caseSensitive ? input : input?.ToLowerInvariant();

		static IEnumerable<Range> SelectRectangle(ITextEditor te, Range range)
		{
			var startLine = te.Data.GetOffsetLine(range.Start);
			var endLine = te.Data.GetOffsetLine(range.End);
			if (startLine == endLine)
			{
				yield return range;
				yield break;
			}
			var startIndex = te.Data.GetOffsetIndex(range.Start, startLine);
			var endIndex = te.Data.GetOffsetIndex(range.End, endLine);
			for (var line = startLine; line <= endLine; ++line)
			{
				var length = te.Data.GetLineLength(line);
				var lineStartOffset = te.Data.GetOffset(line, Math.Min(length, startIndex));
				var lineEndOffset = te.Data.GetOffset(line, Math.Min(length, endIndex));
				yield return new Range(lineEndOffset, lineStartOffset);
			}
		}

		enum SelectSplitEnum
		{
			None = 0,
			Parentheses = 1,
			Brackets = 2,
			Braces = 4,
			String = 8,
			VerbatimString = 16 | String,
			InterpolatedString = 32 | String,
			InterpolatedVerbatimString = InterpolatedString | VerbatimString,
		}
		static IEnumerable<Range> SelectSplit(ITextEditor te, Range range, SelectSplitDialog.Result result)
		{
			var stack = new Stack<SelectSplitEnum>();
			stack.Push(SelectSplitEnum.None);

			var charValue = new Dictionary<char, SelectSplitEnum>
			{
				['('] = SelectSplitEnum.Parentheses,
				[')'] = SelectSplitEnum.Parentheses,
				['['] = SelectSplitEnum.Brackets,
				[']'] = SelectSplitEnum.Brackets,
				['{'] = SelectSplitEnum.Braces,
				['}'] = SelectSplitEnum.Braces,
			};

			var start = range.Start;
			var pos = start;
			var matchPos = -1;
			var matchLen = 0;
			while (true)
			{
				var stackTop = stack.Peek();
				if (stackTop.HasFlag(SelectSplitEnum.String))
				{
					if (pos >= range.End)
						throw new Exception("Incomplete string");
					else if ((pos + 1 < range.End) && (te.Data.Data[pos] == '\\') && (!stackTop.HasFlag(SelectSplitEnum.VerbatimString)))
						pos += 2;
					else if ((pos + 1 < range.End) && (te.Data.Data[pos] == '"') && (te.Data.Data[pos + 1] == '"') && (stackTop.HasFlag(SelectSplitEnum.VerbatimString)))
						pos += 2;
					else if ((pos + 1 < range.End) && (te.Data.Data[pos] == '{') && (te.Data.Data[pos + 1] == '{') && (stackTop.HasFlag(SelectSplitEnum.InterpolatedString)))
						pos += 2;
					else if ((te.Data.Data[pos] == '{') && (stackTop.HasFlag(SelectSplitEnum.InterpolatedString)))
					{
						stack.Push(SelectSplitEnum.Braces);
						++pos;
					}
					else if (te.Data.Data[pos] == '"')
					{
						stack.Pop();
						++pos;
					}
					else
						++pos;
				}
				else
				{
					if ((stackTop == SelectSplitEnum.None) && (pos > matchPos))
					{
						var match = result.Regex.Match(te.GetString(new Range(pos, range.End)));
						if (match.Success)
						{
							if (match.Length == 0)
								throw new Exception("Cannot split on empty selection");
							matchPos = match.Index + pos;
							matchLen = match.Length;
						}
						else
						{
							matchPos = range.End;
							matchLen = 0;
						}
					}

					if ((pos >= range.End) || ((pos == matchPos) && (stackTop == SelectSplitEnum.None)))
					{
						if (stack.Count != 1)
							throw new Exception($"Didn't find close for {stackTop}");
						var useStart = start;
						var useEnd = pos;
						if (result.TrimWhitespace)
						{
							while ((useStart < pos) && (char.IsWhiteSpace(te.Data.Data[useStart])))
								++useStart;
							while ((useEnd > useStart) && (char.IsWhiteSpace(te.Data.Data[useEnd - 1])))
								--useEnd;
						}
						if ((result.IncludeEmpty) || (useStart != useEnd))
							yield return new Range(useEnd, useStart);
						if (pos >= range.End)
							break;
						if (result.IncludeResults)
							yield return Range.FromIndex(pos, matchLen);
						pos += matchLen;
						start = pos;
					}
					else if (!result.Balanced)
						++pos;
					else if ((te.Data.Data[pos] == '(') || (te.Data.Data[pos] == '[') || (te.Data.Data[pos] == '{'))
						stack.Push(charValue[te.Data.Data[pos++]]);
					else if ((te.Data.Data[pos] == ')') || (te.Data.Data[pos] == ']') || (te.Data.Data[pos] == '}'))
					{
						if (charValue[te.Data.Data[pos]] != stackTop)
							throw new Exception($"Didn't find open for {te.Data.Data[pos]}");
						stack.Pop();
						++pos;
					}
					else if (te.Data.Data[pos] == '\"')
					{
						stack.Push(SelectSplitEnum.String);
						++pos;
					}
					else if ((pos + 1 < range.End) && (te.Data.Data[pos] == '@') && (te.Data.Data[pos + 1] == '\"'))
					{
						stack.Push(SelectSplitEnum.VerbatimString);
						pos += 2;
					}
					else if ((pos + 1 < range.End) && (te.Data.Data[pos] == '$') && (te.Data.Data[pos + 1] == '\"'))
					{
						stack.Push(SelectSplitEnum.InterpolatedString);
						pos += 2;
					}
					else if ((pos + 2 < range.End) && (te.Data.Data[pos] == '$') && (te.Data.Data[pos + 1] == '@') && (te.Data.Data[pos + 2] == '\"'))
					{
						stack.Push(SelectSplitEnum.InterpolatedVerbatimString);
						pos += 3;
					}
					else
						++pos;
				}
			}
		}

		static void Command_Select_All(ITextEditor te) => te.SetSelections(new List<Range> { te.FullRange });

		static void Command_Select_Nothing(ITextEditor te) => te.SetSelections(new List<Range>());

		static SelectLimitDialog.Result Command_Select_Limit_Dialog(ITextEditor te) => SelectLimitDialog.Run(te.WindowParent, te.GetVariables());

		static void Command_Select_Limit(ITextEditor te, SelectLimitDialog.Result result)
		{
			var variables = te.GetVariables();
			var firstSelection = new NEExpression(result.FirstSelection).Evaluate<int>(variables);
			var everyNth = new NEExpression(result.EveryNth).Evaluate<int>(variables);
			var takeCount = new NEExpression(result.TakeCount).Evaluate<int>(variables);
			var numSels = new NEExpression(result.NumSelections).Evaluate<int>(variables);

			var sels = te.Selections.Skip(firstSelection - 1);
			if (result.JoinSelections)
				sels = sels.Batch(everyNth).Select(batch => batch.Take(takeCount)).Select(batch => new Range(batch.Last().End, batch.First().Start));
			else
				sels = sels.EveryNth(everyNth, takeCount);
			sels = sels.Take(numSels);

			te.SetSelections(sels.ToList());
		}

		static void Command_Select_Lines(ITextEditor te, bool includeEndings)
		{
			var lineSets = te.Selections.AsParallel().Select(range => new { start = te.Data.GetOffsetLine(range.Start), end = te.Data.GetOffsetLine(Math.Max(range.Start, range.End - 1)) }).ToList();

			var hasLine = new bool[te.Data.NumLines];
			foreach (var set in lineSets)
				for (var ctr = set.start; ctr <= set.end; ++ctr)
					hasLine[ctr] = true;

			var lines = new List<int>();
			for (var line = 0; line < hasLine.Length; ++line)
				if ((hasLine[line]) && (!te.Data.IsDiffGapLine(line)))
					lines.Add(line);

			te.SetSelections(lines.AsParallel().AsOrdered().Select(line => Range.FromIndex(te.Data.GetOffset(line, 0), te.Data.GetLineLength(line) + (includeEndings ? te.Data.GetEndingLength(line) : 0))).ToList());
		}

		static void Command_Select_Rectangle(ITextEditor te) => te.SetSelections(te.Selections.AsParallel().AsOrdered().SelectMany(range => SelectRectangle(te, range)).ToList());

		static void Command_Select_Invert(ITextEditor te)
		{
			var start = new[] { 0 }.Concat(te.Selections.Select(sel => sel.End));
			var end = te.Selections.Select(sel => sel.Start).Concat(new[] { te.Data.NumChars });
			te.SetSelections(Enumerable.Zip(start, end, (startPos, endPos) => new Range(endPos, startPos)).Where(range => (range.HasSelection) || ((range.Start != 0) && (range.Start != te.Data.NumChars))).ToList());
		}

		static void Command_Select_Join(ITextEditor te)
		{
			var sels = new List<Range>();
			var start = 0;
			while (start < te.Selections.Count)
			{
				var end = start;
				while ((end + 1 < te.Selections.Count) && (te.Selections[end].End == te.Selections[end + 1].Start))
					++end;
				sels.Add(new Range(te.Selections[end].End, te.Selections[start].Start));
				start = end + 1;
			}
			te.SetSelections(sels);
		}

		static void Command_Select_Empty(ITextEditor te, bool include) => te.SetSelections(te.Selections.Where(range => range.HasSelection != include).ToList());

		static void Command_Select_ToggleOpenClose(ITextEditor te, bool shiftDown)
		{
			te.SetSelections(te.Selections.AsParallel().AsOrdered().Select(range =>
			{
				var newPos = te.Data.GetOppositeBracket(range.Cursor);
				if (newPos == -1)
					return range;

				return te.MoveCursor(range, newPos, shiftDown);
			}).ToList());
		}

		static void Command_Select_Repeats_Unique(ITextEditor te, bool caseSensitive) => te.SetSelections(te.Selections.AsParallel().AsOrdered().Distinct(range => RepeatsValue(caseSensitive, te.GetString(range))).ToList());

		static void Command_Select_Repeats_Duplicates(ITextEditor te, bool caseSensitive) => te.SetSelections(te.Selections.AsParallel().AsOrdered().Duplicate(range => RepeatsValue(caseSensitive, te.GetString(range))).ToList());

		static void Command_Select_Repeats_MatchPrevious(ITextEditor te, bool caseSensitive) => te.SetSelections(te.Selections.AsParallel().AsOrdered().Match(range => RepeatsValue(caseSensitive, te.GetString(range))).ToList());

		static void Command_Select_Repeats_NonMatchPrevious(ITextEditor te, bool caseSensitive) => te.SetSelections(te.Selections.AsParallel().AsOrdered().NonMatch(range => RepeatsValue(caseSensitive, te.GetString(range))).ToList());

		static void Command_Select_Repeats_RepeatedLines(ITextEditor te) => te.SetSelections(te.Selections.AsParallel().AsOrdered().SelectMany(range => FindRepetitions(te, range)).ToList());

		static SelectByCountDialog.Result Command_Select_Repeats_ByCount_Dialog(ITextEditor te) => SelectByCountDialog.Run(te.WindowParent);

		static void Command_Select_Repeats_ByCount(ITextEditor te, SelectByCountDialog.Result result)
		{
			var strs = te.Selections.Select((range, index) => Tuple.Create(te.GetString(range), index)).ToList();
			var counts = new Dictionary<string, int>(result.CaseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase);
			foreach (var tuple in strs)
			{
				if (!counts.ContainsKey(tuple.Item1))
					counts[tuple.Item1] = 0;
				++counts[tuple.Item1];
			}
			strs = strs.Where(tuple => ((!result.MinCount.HasValue) || (counts[tuple.Item1] >= result.MinCount)) && ((!result.MaxCount.HasValue) || (counts[tuple.Item1] <= result.MaxCount))).ToList();
			te.SetSelections(strs.Select(tuple => te.Selections[tuple.Item2]).ToList());
		}

		static SelectSplitDialog.Result Command_Select_Split_Dialog(ITextEditor te) => SelectSplitDialog.Run(te.WindowParent, te.GetVariables());

		static void Command_Select_Split(ITextEditor te, SelectSplitDialog.Result result)
		{
			var indexes = te.GetFixedExpressionResults<int>(result.Index);
			te.SetSelections(te.Selections.AsParallel().AsOrdered().SelectMany((range, index) => SelectSplit(te, range, result).Skip(indexes[index] == 0 ? 0 : indexes[index] - 1).Take(indexes[index] == 0 ? int.MaxValue : 1)).ToList());
		}

		static void Command_Select_Selection_First(ITextEditor te)
		{
			te.CurrentSelection = 0;
			te.EnsureVisible();
		}

		static void Command_Select_Selection_CenterVertically(ITextEditor te) => te.EnsureVisible(true);

		static void Command_Select_Selection_Center(ITextEditor te) => te.EnsureVisible(true, true);

		static void Command_Select_Selection_ToggleAnchor(ITextEditor te) => te.SetSelections(te.Selections.Select(range => new Range(range.Anchor, range.Cursor)).ToList());

		static void Command_Select_Selection_NextPrevious(ITextEditor te, bool next)
		{
			var offset = next ? 1 : -1;
			te.CurrentSelection += offset;
			if (te.CurrentSelection < 0)
				te.CurrentSelection = te.Selections.Count - 1;
			if (te.CurrentSelection >= te.Selections.Count)
				te.CurrentSelection = 0;
			te.EnsureVisible();
		}

		static void Command_Select_Selection_Single(ITextEditor te)
		{
			if (!te.Selections.Any())
				return;
			te.SetSelections(new List<Range> { te.Selections[te.CurrentSelection] });
			te.CurrentSelection = 0;
		}

		static void Command_Select_Selection_Remove(ITextEditor te)
		{
			te.SetSelections(te.Selections.Where((sel, index) => index != te.CurrentSelection).ToList());
			te.CurrentSelection = Math.Max(0, Math.Min(te.CurrentSelection, te.Selections.Count - 1));
		}

		static void Command_Select_Selection_RemoveBeforeCurrent(ITextEditor te)
		{
			te.SetSelections(te.Selections.Where((sel, index) => index >= te.CurrentSelection).ToList());
			te.CurrentSelection = 0;
		}

		static void Command_Select_Selection_RemoveAfterCurrent(ITextEditor te)
		{
			te.SetSelections(te.Selections.Where((sel, index) => index <= te.CurrentSelection).ToList());
		}
	}
}
