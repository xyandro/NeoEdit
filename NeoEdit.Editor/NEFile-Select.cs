﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NeoEdit.Common;
using NeoEdit.Common.Configuration;
using NeoEdit.Editor.PreExecution;
using NeoEdit.Editor.Searchers;
using NeoEdit.TaskRunning;

namespace NeoEdit.Editor
{
	partial class NEFile
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

		static string RepeatsValue(bool caseSensitive, string input) => caseSensitive ? input : input?.ToLowerInvariant();

		IEnumerable<Range> SelectSplit(Range range, Configuration_Select_Split result, ISearcher searcher)
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
			if ((position < 0) || (position > Text.Length))
				return -1;

			var dict = new Dictionary<char, char>
			{
				{ '(', ')' },
				{ '{', '}' },
				{ '[', ']' },
				{ '<', '>' },
			};

			var found = default(KeyValuePair<char, char>);
			if ((found.Key == 0) && (position < Text.Length))
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
			for (; (position >= 0) && (position < Text.Length); position += direction)
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

		string GetSummaryName(int index)
		{
			if (!string.IsNullOrWhiteSpace(DisplayName))
				return DisplayName;
			if (!string.IsNullOrWhiteSpace(FileName))
				return $"Summary for {Path.GetFileName(FileName)}";
			return $"Summary {index + 1}";
		}

		void Execute_Select_All() => Selections = new List<Range> { Range.FromIndex(0, Text.Length) };

		void Execute_Select_Nothing() => Selections = new List<Range>();

		static Configuration_Select_Limit Configure_Select_Limit(EditorExecuteState state) => state.NEFiles.FilesWindow.Configure_Select_Limit(state.NEFiles.Focused.GetVariables());

		void Execute_Select_Limit()
		{
			var result = state.Configuration as Configuration_Select_Limit;
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
			var lineSets = Selections.AsTaskRunner().Select(range => new { start = Text.GetPositionLine(range.Start), end = Text.GetPositionLine(Math.Max(range.Start, range.End - 1)) }).ToList();

			var hasLine = new bool[Text.NumLines];
			foreach (var set in lineSets)
				for (var ctr = set.start; ctr <= set.end; ++ctr)
					hasLine[ctr] = true;

			var lines = new List<int>();
			for (var line = 0; line < hasLine.Length; ++line)
				if ((hasLine[line]) && (!Text.IsDiffGapLine(line)))
					lines.Add(line);

			Selections = lines.AsTaskRunner().Select(line => Range.FromIndex(Text.GetPosition(line, 0), Text.GetLineLength(line))).ToList();
		}

		void Execute_Select_WholeLines()
		{
			var sels = Selections.AsTaskRunner().Select(range =>
			{
				var startLine = Text.GetPositionLine(range.Start);
				var startPosition = Text.GetPosition(startLine, 0);
				var endLine = Text.GetPositionLine(Math.Max(range.Start, range.End - 1));
				var endPosition = Text.GetPosition(endLine, 0) + Text.GetLineLength(endLine) + Text.GetEndingLength(endLine);
				return new Range(endPosition, startPosition);
			}).ToList();

			Selections = sels;
		}

		void Execute_Select_Invert()
		{
			var start = new[] { 0 }.Concat(Selections.Select(sel => sel.End));
			var end = Selections.Select(sel => sel.Start).Concat(new[] { Text.Length });
			Selections = Enumerable.Zip(start, end, (startPos, endPos) => new Range(endPos, startPos)).Where(range => (range.HasSelection) || ((range.Start != 0) && (range.Start != Text.Length))).ToList();
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
			Selections = Selections.AsTaskRunner().Select(range =>
			{
				var newPos = GetOppositeBracket(range.Cursor);
				if (newPos == -1)
					return range;

				return MoveCursor(range, newPos, state.ShiftDown);
			}).ToList();
		}

		void Execute_Select_Repeats_Unique(bool caseSensitive) => Selections = Selections.AsTaskRunner().DistinctBy(range => RepeatsValue(caseSensitive, Text.GetString(range))).ToList();

		void Execute_Select_Repeats_Duplicates(bool caseSensitive) => Selections = Selections.AsTaskRunner().DuplicateBy(range => RepeatsValue(caseSensitive, Text.GetString(range))).ToList();

		void Execute_Select_Repeats_MatchPrevious(bool caseSensitive) => Selections = Selections.AsTaskRunner().MatchBy(range => RepeatsValue(caseSensitive, Text.GetString(range))).ToList();

		void Execute_Select_Repeats_NonMatchPrevious(bool caseSensitive) => Selections = Selections.AsTaskRunner().NonMatchBy(range => RepeatsValue(caseSensitive, Text.GetString(range))).ToList();

		static Configuration_Select_Repeats_ByCount Configure_Select_Repeats_ByCount(EditorExecuteState state) => state.NEFiles.FilesWindow.Configure_Select_Repeats_ByCount();

		void Execute_Select_Repeats_ByCount(bool caseSensitive)
		{
			var result = state.Configuration as Configuration_Select_Repeats_ByCount;
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

		static PreExecution_Select_Repeats_Files_MatchMismatch PreExecute_Select_Repeats_Files_MatchMismatch(EditorExecuteState state, bool caseSensitive)
		{
			var preExecution = new PreExecution_Select_Repeats_Files_MatchMismatch();
			foreach (var neFile in state.NEFiles.ActiveFiles)
			{
				var strs = neFile.GetSelectionStrings().ToList();
				var matches = preExecution.Matches ?? strs;
				while (matches.Count < strs.Count)
					matches.Add(null);
				while (strs.Count < matches.Count)
					strs.Add(null);

				var stringComparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
				for (var ctr = 0; ctr < matches.Count; ++ctr)
					if ((matches[ctr] != null) && (!string.Equals(matches[ctr], strs[ctr], stringComparison)))
						matches[ctr] = null;

				preExecution.Matches = matches;
			}
			return preExecution;
		}

		void Execute_Select_Repeats_Files_MatchMismatch(bool match)
		{
			var matches = (state.PreExecution as PreExecution_Select_Repeats_Files_MatchMismatch).Matches;
			Selections = Selections.Where((range, index) => (matches[index] != null) == match).ToList();
		}

		static PreExecution_Select_Repeats_Files_CommonNonCommon PreExecute_Select_Repeats_Files_CommonNonCommon(EditorExecuteState state, bool caseSensitive)
		{
			var preExecution = new PreExecution_Select_Repeats_Files_CommonNonCommon();
			var stringComparer = caseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
			foreach (var neFile in state.NEFiles.ActiveFiles)
			{
				var repeats = neFile.Selections.AsTaskRunner().GroupBy(neFile.Text.GetString, stringComparer).ToDictionary(g => g.Key, g => g.Count(), stringComparer);

				if (preExecution.Repeats != null)
					repeats = repeats.Join(preExecution.Repeats, pair => pair.Key, pair => pair.Key, (r1, r2) => new { r1.Key, Value = Math.Min(r1.Value, r2.Value) }, repeats.Comparer).ToDictionary(obj => obj.Key, obj => obj.Value, repeats.Comparer);

				preExecution.Repeats = repeats;
			}
			return preExecution;
		}

		void Execute_Select_Repeats_Files_CommonNonCommon(bool match)
		{
			var repeats = (state.PreExecution as PreExecution_Select_Repeats_Files_CommonNonCommon).Repeats;
			repeats = repeats.ToDictionary(pair => pair.Key, pair => pair.Value, repeats.Comparer);
			Selections = Selections.Where(range =>
			{
				var str = Text.GetString(range);
				return ((repeats.ContainsKey(str)) && (repeats[str]-- > 0)) == match;
			}).ToList();
		}

		static Configuration_Select_Split Configure_Select_Split(EditorExecuteState state) => state.NEFiles.FilesWindow.Configure_Select_Split(state.NEFiles.Focused.GetVariables());

		void Execute_Select_Split()
		{
			var result = state.Configuration as Configuration_Select_Split;
			var indexes = GetExpressionResults<int>(result.Index, Selections.Count());

			ISearcher searcher;
			if (result.IsRegex)
				searcher = new RegexesSearcher(new List<string> { result.Text }, result.WholeWords, result.MatchCase, firstMatchOnly: true);
			else
				searcher = new StringSearcher(result.Text, result.WholeWords, result.MatchCase, firstMatchOnly: true);

			Selections = Selections.AsTaskRunner().SelectMany((range, index) => SelectSplit(range, result, searcher).Skip(indexes[index] == 0 ? 0 : indexes[index] - 1).Take(indexes[index] == 0 ? int.MaxValue : 1)).ToList();
		}

		static PreExecutionStop PreExecute_Select_Summarize(EditorExecuteState state, bool caseSensitive, bool showAllFiles)
		{
			var selectionsByFile = state.NEFiles.ActiveFiles.Select((neFile, index) => (DisplayName: neFile.GetSummaryName(index), Selections: neFile.GetSelectionStrings())).ToList();

			if (!showAllFiles)
				selectionsByFile = new List<(string DisplayName, IReadOnlyList<string> Selections)> { (DisplayName: "Summary", Selections: selectionsByFile.SelectMany(x => x.Selections).ToList()) };

			var comparer = caseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
			var summaryByFile = selectionsByFile.Select(tuple => (tuple.DisplayName, selections: tuple.Selections.GroupBy(x => x, comparer).Select(group => (str: group.Key, count: group.Count())).OrderByDescending(x => x.count).ToList())).ToList();

			var neFiles = new NEFiles(false);
			neFiles.BeginTransaction(state);
			foreach (var neFile in summaryByFile)
				neFiles.AddFile(CreateSummaryFile(neFile.DisplayName, neFile.selections));
			neFiles.SetLayout(new WindowLayout(maxColumns: 4, maxRows: 4));
			neFiles.Commit();

			return PreExecutionStop.Stop;
		}

		void Execute_Select_Selection_First()
		{
			CurrentSelection = 0;
			EnsureVisible();
		}

		void Execute_Select_Selection_CenterVertically() => EnsureVisible(true);

		void Execute_Select_Selection_Center() => EnsureVisible(true, true);

		static Configuration_Select_Selection_ToggleAnchor Configure_Select_Selection_ToggleAnchor(EditorExecuteState state) => new Configuration_Select_Selection_ToggleAnchor { AnchorStart = state.NEFiles.ActiveFiles.Any(neFile => neFile.Selections.Any(range => range.Anchor > range.Cursor)) };

		void Execute_Select_Selection_ToggleAnchor()
		{
			var anchorStart = (state.Configuration as Configuration_Select_Selection_ToggleAnchor).AnchorStart;
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
