using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using NeoEdit.Common;
using NeoEdit.Program.Models;
using NeoEdit.Program.Searchers;

namespace NeoEdit.Program
{
	partial class Tab
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

		IEnumerable<Range> SelectSplit(Range range, SelectSplitDialogResult result, ISearcher searcher)
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
						var found = searcher.Find(Text.GetString(pos, range.End - pos), pos).FirstOrDefault();
						if (found != null)
						{
							if (found.Length == 0)
								throw new Exception("Cannot split on empty selection");
							matchPos = found.Start;
							matchLen = found.Length;
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

		void Execute_Select_All() => Selections = new List<Range> { Range.FromIndex(0, Text.Length) };

		void Execute_Select_Nothing() => Selections = new List<Range>();

		object Configure_Select_Limit() => state.TabsWindow.RunSelectLimitDialog(GetVariables());

		void Execute_Select_Limit()
		{
			var result = state.Configuration as SelectLimitDialogResult;
			var variables = GetVariables();
			var firstSelection = state.GetExpression(result.FirstSelection).Evaluate<int>(variables);
			var everyNth = state.GetExpression(result.EveryNth).Evaluate<int>(variables);
			var takeCount = state.GetExpression(result.TakeCount).Evaluate<int>(variables);
			var numSels = state.GetExpression(result.NumSelections).Evaluate<int>(variables);

			var sels = Selections.Skip(firstSelection - 1);
			if (result.JoinSelections)
				sels = sels.Batch(everyNth).Select(batch => batch.Take(takeCount)).Select(batch => new Range(batch.Last().End, batch.First().Start));
			else
				sels = sels.EveryNth(everyNth, takeCount);
			sels = sels.Take(numSels);

			Selections = sels.ToList();
		}

		void Execute_Select_Lines()
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

			Selections = lines.AsParallel().AsOrdered().Select(line => Range.FromIndex(TextView.GetPosition(line, 0), TextView.GetLineLength(line))).ToList();
		}

		void Execute_Select_WholeLines()
		{
			var sels = Selections.AsParallel().AsOrdered().Select(range =>
			{
				var startLine = TextView.GetPositionLine(range.Start);
				var startPosition = TextView.GetPosition(startLine, 0);
				var endLine = TextView.GetPositionLine(Math.Max(range.Start, range.End - 1));
				var endPosition = TextView.GetPosition(endLine, 0) + TextView.GetLineLength(endLine) + TextView.GetEndingLength(endLine);
				return new Range(endPosition, startPosition);
			}).ToList();

			Selections = sels;
		}

		void Execute_Select_Rectangle() => Selections = Selections.AsParallel().AsOrdered().SelectMany(range => SelectRectangle(range)).ToList();

		void Execute_Select_Invert()
		{
			var start = new[] { 0 }.Concat(Selections.Select(sel => sel.End));
			var end = Selections.Select(sel => sel.Start).Concat(new[] { TextView.MaxPosition });
			Selections = Enumerable.Zip(start, end, (startPos, endPos) => new Range(endPos, startPos)).Where(range => (range.HasSelection) || ((range.Start != 0) && (range.Start != TextView.MaxPosition))).ToList();
		}

		void Execute_Select_Join()
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
			Selections = sels;
		}

		void Execute_Select_Empty(bool include) => Selections = Selections.Where(range => range.HasSelection != include).ToList();

		void Execute_Select_ToggleOpenClose()
		{
			Selections = Selections.AsParallel().AsOrdered().Select(range =>
			{
				var newPos = GetOppositeBracket(range.Cursor);
				if (newPos == -1)
					return range;

				return MoveCursor(range, newPos, state.ShiftDown);
			}).ToList();
		}

		void Execute_Select_Repeats_Unique(bool caseSensitive) => Selections = Selections.AsParallel().AsOrdered().Distinct(range => RepeatsValue(caseSensitive, Text.GetString(range))).ToList();

		void Execute_Select_Repeats_Duplicates(bool caseSensitive) => Selections = Selections.AsParallel().AsOrdered().Duplicate(range => RepeatsValue(caseSensitive, Text.GetString(range))).ToList();

		void Execute_Select_Repeats_MatchPrevious(bool caseSensitive) => Selections = Selections.AsParallel().AsOrdered().Match(range => RepeatsValue(caseSensitive, Text.GetString(range))).ToList();

		void Execute_Select_Repeats_NonMatchPrevious(bool caseSensitive) => Selections = Selections.AsParallel().AsOrdered().NonMatch(range => RepeatsValue(caseSensitive, Text.GetString(range))).ToList();

		void Execute_Select_Repeats_RepeatedLines(bool caseSensitive) => Selections = Selections.AsParallel().AsOrdered().SelectMany(range => FindRepetitions(caseSensitive, range)).ToList();

		object Configure_Select_Repeats_ByCount() => state.TabsWindow.RunSelectByCountDialog();

		void Execute_Select_Repeats_ByCount(bool caseSensitive)
		{
			var result = state.Configuration as SelectByCountDialogResult;
			var strs = Selections.Select((range, index) => Tuple.Create(Text.GetString(range), index)).ToList();
			var counts = new Dictionary<string, int>(caseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase);
			foreach (var tuple in strs)
			{
				if (!counts.ContainsKey(tuple.Item1))
					counts[tuple.Item1] = 0;
				++counts[tuple.Item1];
			}
			strs = strs.Where(tuple => ((!result.MinCount.HasValue) || (counts[tuple.Item1] >= result.MinCount)) && ((!result.MaxCount.HasValue) || (counts[tuple.Item1] <= result.MaxCount))).ToList();
			Selections = strs.Select(tuple => Selections[tuple.Item2]).ToList();
		}

		object Configure_Select_Repeats_Tabs_MatchMismatch(bool caseSensitive)
		{
			List<string> result = null;
			foreach (var tab in state.ActiveTabs)
			{
				var strs = tab.GetSelectionStrings();
				var matches = result ?? strs;
				while (matches.Count < strs.Count)
					matches.Add(null);
				while (strs.Count < matches.Count)
					strs.Add(null);

				var stringComparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
				for (var ctr = 0; ctr < matches.Count; ++ctr)
					if ((matches[ctr] != null) && (!string.Equals(matches[ctr], strs[ctr], stringComparison)))
						matches[ctr] = null;

				result = matches;
			}
			return result;
		}

		void Execute_Select_Repeats_Tabs_MatchMismatch(bool match)
		{
			var matches = state.Configuration as List<string>;
			Selections = Selections.Where((range, index) => (matches[index] != null) == match).ToList();
		}

		object Configure_Select_Repeats_Tabs_CommonNonCommon(bool caseSensitive)
		{
			Dictionary<string, int> result = null;
			var stringComparer = caseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
			foreach (var tab in state.ActiveTabs)
			{
				var repeats = tab.Selections.AsParallel().GroupBy(tab.Text.GetString, stringComparer).ToDictionary(g => g.Key, g => g.Count(), stringComparer);

				if (result != null)
					repeats = repeats.Join(result, pair => pair.Key, pair => pair.Key, (r1, r2) => new { r1.Key, Value = Math.Min(r1.Value, r2.Value) }, repeats.Comparer).ToDictionary(obj => obj.Key, obj => obj.Value, repeats.Comparer);

				result = repeats;
			}
			return result;
		}

		void Execute_Select_Repeats_Tabs_CommonNonCommon(bool match)
		{
			var repeats = state.Configuration as Dictionary<string, int>;
			repeats = repeats.ToDictionary(pair => pair.Key, pair => pair.Value, repeats.Comparer);
			Selections = Selections.Where(range =>
			{
				var str = Text.GetString(range);
				return ((repeats.ContainsKey(str)) && (repeats[str]-- > 0)) == match;
			}).ToList();
		}

		object Configure_Select_Split() => state.TabsWindow.RunSelectSplitDialog(GetVariables());

		void Execute_Select_Split()
		{
			var result = state.Configuration as SelectSplitDialogResult;
			var indexes = GetExpressionResults<int>(result.Index, Selections.Count());

			ISearcher searcher;
			if (result.IsRegex)
				searcher = new RegexesSearcher(new List<string> { result.Text }, result.WholeWords, result.MatchCase, firstMatchOnly: true);
			else
				searcher = new StringSearcher(result.Text, result.WholeWords, result.MatchCase, firstMatchOnly: true);


			Selections = Selections.AsParallel().AsOrdered().SelectMany((range, index) => SelectSplit(range, result, searcher).Skip(indexes[index] == 0 ? 0 : indexes[index] - 1).Take(indexes[index] == 0 ? int.MaxValue : 1)).ToList();
		}

		void Execute_Select_Selection_First()
		{
			CurrentSelection = 0;
			EnsureVisible();
		}

		void Execute_Select_Selection_CenterVertically() => EnsureVisible(true);

		void Execute_Select_Selection_Center() => EnsureVisible(true, true);

		object Configure_Select_Selection_ToggleAnchor() => state.ActiveTabs.Any(tab => tab.Selections.Any(range => range.Anchor > range.Cursor));

		void Execute_Select_Selection_ToggleAnchor()
		{
			var anchorStart = (bool)state.Configuration;
			Selections = Selections.Select(range => new Range(anchorStart ? range.End : range.Start, anchorStart ? range.Start : range.End)).ToList();
		}

		void Execute_Select_Selection_NextPrevious(bool next)
		{
			var newSelection = CurrentSelection + (next ? 1 : -1);
			if (newSelection < 0)
				newSelection = Selections.Count - 1;
			if (newSelection >= Selections.Count)
				newSelection = 0;
			CurrentSelection = newSelection;
			EnsureVisible();
		}

		void Execute_Select_Selection_Single()
		{
			if (!Selections.Any())
				return;
			Selections = new List<Range> { Selections[CurrentSelection] };
			CurrentSelection = 0;
		}

		void Execute_Select_Selection_Remove()
		{
			Selections = Selections.Where((sel, index) => index != CurrentSelection).ToList();
			CurrentSelection = Math.Max(0, Math.Min(CurrentSelection, Selections.Count - 1));
		}

		void Execute_Select_Selection_RemoveBeforeCurrent()
		{
			Selections = Selections.Where((sel, index) => index >= CurrentSelection).ToList();
			CurrentSelection = 0;
		}

		void Execute_Select_Selection_RemoveAfterCurrent()
		{
			Selections = Selections.Where((sel, index) => index <= CurrentSelection).ToList();
		}
	}
}
