using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using NeoEdit.Program.Dialogs;
using NeoEdit.Program.Expressions;

namespace NeoEdit.Program
{
	partial class TextEditorData
	{
		enum SelectSplitEnum
		{
			None = 0,
			Parentheses = 1,
			Brackets = 2,
			Braces = 4,
			LTGT = 8,
			String = 16,
			VerbatimString = 32 | String,
			InterpolatedString = 64 | String,
			InterpolatedVerbatimString = InterpolatedString | VerbatimString,
		}

		IEnumerable<Range> FindRepetitions(bool caseSensitive, Range inputRange)
		{
			var startLine = TextView.GetPositionLine(inputRange.Start);
			var endLine = TextView.GetPositionLine(inputRange.End);
			var lineRanges = Enumerable.Range(startLine, endLine - startLine + 1).Select(line => new Range(Math.Max(inputRange.Start, TextView.GetPosition(line, 0)), Math.Min(inputRange.End, TextView.GetPosition(line, TextView.GetLineLength(line))))).ToList();
			if ((lineRanges.Count >= 2) && (!lineRanges[lineRanges.Count - 1].HasSelection))
				lineRanges.RemoveAt(lineRanges.Count - 1);
			var lineStrs = lineRanges.Select(range => Text.GetString(range)).Select(str => caseSensitive ? str : str.ToLowerInvariant()).ToList();
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

		IEnumerable<Range> SelectRectangle(Range range)
		{
			var startLine = TextView.GetPositionLine(range.Start);
			var endLine = TextView.GetPositionLine(range.End);
			if (startLine == endLine)
			{
				yield return range;
				yield break;
			}
			var startIndex = TextView.GetPositionIndex(range.Start, startLine);
			var endIndex = TextView.GetPositionIndex(range.End, endLine);
			for (var line = startLine; line <= endLine; ++line)
			{
				var length = TextView.GetLineLength(line);
				var lineStartPosition = TextView.GetPosition(line, Math.Min(length, startIndex));
				var lineEndPosition = TextView.GetPosition(line, Math.Min(length, endIndex));
				yield return new Range(lineEndPosition, lineStartPosition);
			}
		}

		IEnumerable<Range> SelectSplit(Range range, SelectSplitDialog.Result result)
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
				['<'] = SelectSplitEnum.LTGT,
				['>'] = SelectSplitEnum.LTGT,
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
					else if ((pos + 1 < range.End) && (Text[pos] == '\\') && (!stackTop.HasFlag(SelectSplitEnum.VerbatimString)))
						pos += 2;
					else if ((pos + 1 < range.End) && (Text[pos] == '"') && (Text[pos + 1] == '"') && (stackTop.HasFlag(SelectSplitEnum.VerbatimString)))
						pos += 2;
					else if ((pos + 1 < range.End) && (Text[pos] == '{') && (Text[pos + 1] == '{') && (stackTop.HasFlag(SelectSplitEnum.InterpolatedString)))
						pos += 2;
					else if ((Text[pos] == '{') && (stackTop.HasFlag(SelectSplitEnum.InterpolatedString)))
					{
						stack.Push(SelectSplitEnum.Braces);
						++pos;
					}
					else if (Text[pos] == '"')
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
						var match = result.Regex.Match(Text.GetString(pos, range.End - pos));
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
							while ((useStart < pos) && (char.IsWhiteSpace(Text[useStart])))
								++useStart;
							while ((useEnd > useStart) && (char.IsWhiteSpace(Text[useEnd - 1])))
								--useEnd;
						}
						if ((!result.ExcludeEmpty) || (useStart != useEnd))
							yield return new Range(useEnd, useStart);
						if (pos >= range.End)
							break;
						if (result.IncludeResults)
							yield return Range.FromIndex(pos, matchLen);
						pos += matchLen;
						start = pos;
					}
					else if (((result.BalanceParens) && (Text[pos] == '(')) || ((result.BalanceBrackets) && (Text[pos] == '[')) || ((result.BalanceBraces) && (Text[pos] == '{')) || ((result.BalanceLTGT) && (Text[pos] == '<')))
						stack.Push(charValue[Text[pos++]]);
					else if (((result.BalanceParens) && (Text[pos] == ')')) || ((result.BalanceBrackets) && (Text[pos] == ']')) || ((result.BalanceBraces) && (Text[pos] == '}')) || ((result.BalanceLTGT) && (Text[pos] == '>')))
					{
						if (charValue[Text[pos]] != stackTop)
							throw new Exception($"Didn't find open for {Text[pos]}");
						stack.Pop();
						++pos;
					}
					else if ((result.BalanceStrings) && (Text[pos] == '\"'))
					{
						stack.Push(SelectSplitEnum.String);
						++pos;
					}
					else if ((result.BalanceStrings) && (pos + 1 < range.End) && (Text[pos] == '@') && (Text[pos + 1] == '\"'))
					{
						stack.Push(SelectSplitEnum.VerbatimString);
						pos += 2;
					}
					else if ((result.BalanceStrings) && (pos + 1 < range.End) && (Text[pos] == '$') && (Text[pos + 1] == '\"'))
					{
						stack.Push(SelectSplitEnum.InterpolatedString);
						pos += 2;
					}
					else if ((result.BalanceStrings) && (pos + 2 < range.End) && (Text[pos] == '$') && (Text[pos + 1] == '@') && (Text[pos + 2] == '\"'))
					{
						stack.Push(SelectSplitEnum.InterpolatedVerbatimString);
						pos += 3;
					}
					else
						++pos;
				}
			}
		}

		int GetOppositeBracket(int position)
		{
			if ((position < 0) || (position > TextView.MaxPosition))
				return -1;

			var dict = new Dictionary<char, char>
			{
				{ '(', ')' },
				{ '{', '}' },
				{ '[', ']' },
				{ '<', '>' },
			};

			var found = default(KeyValuePair<char, char>);
			if ((found.Key == 0) && (position < TextView.MaxPosition))
				found = dict.FirstOrDefault(entry => (entry.Key == Text[position]) || (entry.Value == Text[position]));
			var posAdjust = 1;
			if (found.Key == 0)
			{
				if (--position < 0)
					return -1;
				found = dict.FirstOrDefault(entry => (entry.Key == Text[position]) || (entry.Value == Text[position]));
				posAdjust = 0;
			}
			if (found.Key == 0)
				return -1;

			var direction = found.Key == Text[position] ? 1 : -1;

			var num = 0;
			for (; (position >= 0) && (position < TextView.MaxPosition); position += direction)
			{
				if (Text[position] == found.Key)
					++num;
				if (Text[position] == found.Value)
					--num;

				if (num == 0)
					return position + posAdjust;
			}

			return -1;
		}

		void Command_Select_All() => SetSelections(new List<Range> { Range.FromIndex(0, Text.Length) });

		void Command_Select_Nothing() => SetSelections(new List<Range>());

		SelectLimitDialog.Result Command_Select_Limit_Dialog(TabsWindow TabsParent) => SelectLimitDialog.Run(TabsParent, GetVariables());

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

		void Command_Select_Lines()
		{
			var lineSets = Selections.AsParallel().Select(range => new { start = TextView.GetPositionLine(range.Start), end = TextView.GetPositionLine(Math.Max(range.Start, range.End - 1)) }).ToList();

			var hasLine = new bool[TextView.NumLines];
			foreach (var set in lineSets)
				for (var ctr = set.start; ctr <= set.end; ++ctr)
					hasLine[ctr] = true;

			var lines = new List<int>();
			for (var line = 0; line < hasLine.Length; ++line)
				if (hasLine[line])
					lines.Add(line);

			SetSelections(lines.AsParallel().AsOrdered().Select(line => Range.FromIndex(TextView.GetPosition(line, 0), TextView.GetLineLength(line))).ToList());
		}

		void Command_Select_WholeLines()
		{
			var sels = Selections.AsParallel().AsOrdered().Select(range =>
			{
				var startLine = TextView.GetPositionLine(range.Start);
				var startPosition = TextView.GetPosition(startLine, 0);
				var endLine = TextView.GetPositionLine(Math.Max(range.Start, range.End - 1));
				var endPosition = TextView.GetPosition(endLine, 0) + TextView.GetLineLength(endLine) + TextView.GetEndingLength(endLine);
				return new Range(endPosition, startPosition);
			}).ToList();

			SetSelections(sels);
		}

		void Command_Select_Rectangle() => SetSelections(Selections.AsParallel().AsOrdered().SelectMany(range => SelectRectangle(range)).ToList());

		void Command_Select_Invert()
		{
			var start = new[] { 0 }.Concat(Selections.Select(sel => sel.End));
			var end = Selections.Select(sel => sel.Start).Concat(new[] { TextView.MaxPosition });
			SetSelections(Enumerable.Zip(start, end, (startPos, endPos) => new Range(endPos, startPos)).Where(range => (range.HasSelection) || ((range.Start != 0) && (range.Start != TextView.MaxPosition))).ToList());
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
				var newPos = GetOppositeBracket(range.Cursor);
				if (newPos == -1)
					return range;

				return MoveCursor(range, newPos, shiftDown);
			}).ToList());
		}

		void Command_Select_Repeats_Unique(bool caseSensitive) => SetSelections(Selections.AsParallel().AsOrdered().Distinct(range => RepeatsValue(caseSensitive, Text.GetString(range))).ToList());

		void Command_Select_Repeats_Duplicates(bool caseSensitive) => SetSelections(Selections.AsParallel().AsOrdered().Duplicate(range => RepeatsValue(caseSensitive, Text.GetString(range))).ToList());

		void Command_Select_Repeats_MatchPrevious(bool caseSensitive) => SetSelections(Selections.AsParallel().AsOrdered().Match(range => RepeatsValue(caseSensitive, Text.GetString(range))).ToList());

		void Command_Select_Repeats_NonMatchPrevious(bool caseSensitive) => SetSelections(Selections.AsParallel().AsOrdered().NonMatch(range => RepeatsValue(caseSensitive, Text.GetString(range))).ToList());

		void Command_Select_Repeats_RepeatedLines(bool caseSensitive) => SetSelections(Selections.AsParallel().AsOrdered().SelectMany(range => FindRepetitions(caseSensitive, range)).ToList());

		SelectByCountDialog.Result Command_Select_Repeats_ByCount_Dialog(TabsWindow TabsParent) => SelectByCountDialog.Run(TabsParent);

		void Command_Select_Repeats_ByCount(SelectByCountDialog.Result result, bool caseSensitive)
		{
			var strs = Selections.Select((range, index) => Tuple.Create(Text.GetString(range), index)).ToList();
			var counts = new Dictionary<string, int>(caseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase);
			foreach (var tuple in strs)
			{
				if (!counts.ContainsKey(tuple.Item1))
					counts[tuple.Item1] = 0;
				++counts[tuple.Item1];
			}
			strs = strs.Where(tuple => ((!result.MinCount.HasValue) || (counts[tuple.Item1] >= result.MinCount)) && ((!result.MaxCount.HasValue) || (counts[tuple.Item1] <= result.MaxCount))).ToList();
			SetSelections(strs.Select(tuple => Selections[tuple.Item2]).ToList());
		}

		void Command_Pre_Select_Repeats_Tabs_MatchMismatch(ref object preResult, bool caseSensitive)
		{
			var strs = GetSelectionStrings();
			var matches = preResult as List<string> ?? strs;
			while (matches.Count < strs.Count)
				matches.Add(null);
			while (strs.Count < matches.Count)
				strs.Add(null);

			var stringComparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
			for (var ctr = 0; ctr < matches.Count; ++ctr)
				if ((matches[ctr] != null) && (!string.Equals(matches[ctr], strs[ctr], stringComparison)))
					matches[ctr] = null;

			preResult = matches;
		}

		void Command_Select_Repeats_Tabs_MatchMismatch(object preResult, bool match)
		{
			var matches = preResult as List<string>;
			SetSelections(Selections.Where((range, index) => (matches[index] != null) == match).ToList());
		}

		void Command_Pre_Select_Repeats_Tabs_CommonNonCommon(ref object preResult, bool caseSensitive)
		{
			var stringComparer = caseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
			var repeats = Selections.AsParallel().GroupBy(Text.GetString, stringComparer).ToDictionary(g => g.Key, g => g.Count(), stringComparer);

			if (preResult != null)
				repeats = repeats.Join(preResult as Dictionary<string, int>, pair => pair.Key, pair => pair.Key, (r1, r2) => new { r1.Key, Value = Math.Min(r1.Value, r2.Value) }, repeats.Comparer).ToDictionary(obj => obj.Key, obj => obj.Value, repeats.Comparer);

			preResult = repeats;
		}

		void Command_Select_Repeats_Tabs_CommonNonCommon(object preResult, bool match)
		{
			var repeats = preResult as Dictionary<string, int>;
			repeats = repeats.ToDictionary(pair => pair.Key, pair => pair.Value, repeats.Comparer);
			SetSelections(Selections.Where(range =>
			{
				var str = Text.GetString(range);
				return ((repeats.ContainsKey(str)) && (repeats[str]-- > 0)) == match;
			}).ToList());
		}

		SelectSplitDialog.Result Command_Select_Split_Dialog(TabsWindow TabsParent) => SelectSplitDialog.Run(TabsParent, GetVariables());

		void Command_Select_Split(SelectSplitDialog.Result result)
		{
			var indexes = GetExpressionResults<int>(result.Index, Selections.Count());
			SetSelections(Selections.AsParallel().AsOrdered().SelectMany((range, index) => SelectSplit(range, result).Skip(indexes[index] == 0 ? 0 : indexes[index] - 1).Take(indexes[index] == 0 ? int.MaxValue : 1)).ToList());
		}

		void Command_Select_Selection_First()
		{
			CurrentSelection = 0;
			EnsureVisible();
		}

		void Command_Select_Selection_CenterVertically() => EnsureVisible(true);

		void Command_Select_Selection_Center() => EnsureVisible(true, true);

		void Command_Pre_Select_Selection_ToggleAnchor(ref object preResult)
		{
			if (preResult == null)
				preResult = false;
			if ((!(bool)preResult) && (Selections.Any(range => range.Anchor > range.Cursor)))
				preResult = true;
		}

		void Command_Select_Selection_ToggleAnchor(object preResult)
		{
			var anchorStart = (bool)preResult;
			SetSelections(Selections.Select(range => new Range(anchorStart ? range.End : range.Start, anchorStart ? range.Start : range.End)).ToList());
		}

		void Command_Select_Selection_NextPrevious(bool next)
		{
			var offset = next ? 1 : -1;
			CurrentSelection += offset;
			if (CurrentSelection < 0)
				CurrentSelection = Selections.Count - 1;
			if (CurrentSelection >= Selections.Count)
				CurrentSelection = 0;
			EnsureVisible();
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
