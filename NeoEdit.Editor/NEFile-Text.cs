using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using NeoEdit.Common;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Enums;
using NeoEdit.Common.Parsing;
using NeoEdit.Common.RevRegEx;
using NeoEdit.Common.Transform;
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

		static string GetRandomData(string chars, int length) => new string(Enumerable.Range(0, length).Select(num => chars[random.Next(chars.Length)]).ToArray());

		static string RepeatsValue(bool caseSensitive, string input) => caseSensitive ? input : input?.ToLowerInvariant();

		IEnumerable<NERange> SelectSplit(NERange range, Configuration_Text_Select_Split result, ISearcher searcher)
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
							yield return new NERange(useStart, useEnd);
						if (pos >= range.End)
							break;
						if (result.IncludeResults)
							yield return NERange.FromIndex(pos, matchLen);
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

		static string SetWidth(string str, Configuration_Text_SelectWidth_ByWidth result, int value)
		{
			if (str.Length == value)
				return str;

			if (str.Length > value)
			{
				switch (result.Location)
				{
					case Configuration_Text_SelectWidth_ByWidth.TextLocation.Start: return str.Substring(0, value);
					case Configuration_Text_SelectWidth_ByWidth.TextLocation.Middle: return str.Substring((str.Length - value) / 2, value);
					case Configuration_Text_SelectWidth_ByWidth.TextLocation.End: return str.Substring(str.Length - value);
					default: throw new ArgumentException("Invalid");
				}
			}
			else
			{
				var len = value - str.Length;
				switch (result.Location)
				{
					case Configuration_Text_SelectWidth_ByWidth.TextLocation.Start: return str + new string(result.PadChar, len);
					case Configuration_Text_SelectWidth_ByWidth.TextLocation.Middle: return new string(result.PadChar, len / 2) + str + new string(result.PadChar, (len + 1) / 2);
					case Configuration_Text_SelectWidth_ByWidth.TextLocation.End: return new string(result.PadChar, len) + str;
					default: throw new ArgumentException("Invalid");
				}
			}
		}

		NERange TrimRange(NERange range, Configuration_Text_SelectTrim_WholeBoundedWordTrim result)
		{
			var position = range.Start;
			var length = range.Length;
			if (result.End)
			{
				while ((length > 0) && (result.Chars.Contains(Text[position + length - 1])))
					--length;
			}
			if (result.Start)
			{
				while ((length > 0) && (result.Chars.Contains(Text[position])))
				{
					++position;
					--length;
				}
			}
			if ((position == range.Start) && (length == range.Length))
				return range;
			return NERange.FromIndex(position, length);
		}

		static string TrimString(string str, Configuration_Text_SelectTrim_WholeBoundedWordTrim result)
		{
			var start = 0;
			var end = str.Length;
			if (result.Start)
			{
				while ((start < end) && (result.Chars.Contains(str[start])))
					++start;
			}
			if (result.End)
			{
				while ((start < end) && (result.Chars.Contains(str[end - 1])))
					--end;
			}
			return str.Substring(start, end - start);
		}

		static void Configure_Text_Select_WholeBoundedWord(bool wholeWord) => state.Configuration = state.NEWindow.neWindowUI.RunDialog_Configure_Text_SelectTrim_WholeBoundedWordTrim(wholeWord ? 1 : 2);

		void Execute_Text_Select_WholeBoundedWord(bool wholeWord)
		{
			var result = state.Configuration as Configuration_Text_SelectTrim_WholeBoundedWordTrim;
			var minPosition = 0;
			var maxPosition = Text.Length;

			var sels = new List<NERange>();
			foreach (var range in Selections)
			{
				var startPosition = range.Start;
				var endPosition = range.End;

				if (result.Start)
					while ((startPosition > minPosition) && (result.Chars.Contains(Text[startPosition - 1]) == wholeWord))
						--startPosition;

				if (result.End)
					while ((endPosition < maxPosition) && (result.Chars.Contains(Text[endPosition]) == wholeWord))
						++endPosition;

				sels.Add(new NERange(startPosition, endPosition));
			}
			Selections = sels;
		}

		static void Configure_Text_Select_Trim() => state.Configuration = state.NEWindow.neWindowUI.RunDialog_Configure_Text_SelectTrim_WholeBoundedWordTrim(0);

		void Execute_Text_Select_Trim()
		{
			var result = state.Configuration as Configuration_Text_SelectTrim_WholeBoundedWordTrim;
			Selections = Selections.AsTaskRunner().Select(range => TrimRange(range, result)).ToList();
		}

		static void Configure_Text_Select_Split() => state.Configuration = state.NEWindow.neWindowUI.RunDialog_Configure_Text_Select_Split(state.NEWindow.Focused.GetVariables());

		void Execute_Text_Select_Split()
		{
			var result = state.Configuration as Configuration_Text_Select_Split;
			var indexes = GetExpressionResults<int>(result.Index, Selections.Count());

			ISearcher searcher;
			if (result.IsRegex)
				searcher = new RegexesSearcher(new List<string> { result.Text }, result.WholeWords, result.MatchCase, firstMatchOnly: true);
			else
				searcher = new StringSearcher(result.Text, result.WholeWords, result.MatchCase, firstMatchOnly: true);

			Selections = Selections.AsTaskRunner().SelectMany((range, index) => SelectSplit(range, result, searcher).Skip(indexes[index] == 0 ? 0 : indexes[index] - 1).Take(indexes[index] == 0 ? int.MaxValue : 1)).ToList();
		}

		void Execute_Text_Select_Repeats_Unique_IgnoreMatchCase(bool caseSensitive) => Selections = Selections.AsTaskRunner().DistinctBy(range => RepeatsValue(caseSensitive, Text.GetString(range))).ToList();

		void Execute_Text_Select_Repeats_Duplicates_IgnoreMatchCase(bool caseSensitive) => Selections = Selections.AsTaskRunner().DuplicateBy(range => RepeatsValue(caseSensitive, Text.GetString(range))).ToList();

		void Execute_Text_Select_Repeats_NonMatchPrevious_IgnoreMatchCase(bool caseSensitive) => Selections = Selections.AsTaskRunner().NonMatchBy(range => RepeatsValue(caseSensitive, Text.GetString(range))).ToList();

		void Execute_Text_Select_Repeats_MatchPrevious_IgnoreMatchCase(bool caseSensitive) => Selections = Selections.AsTaskRunner().MatchBy(range => RepeatsValue(caseSensitive, Text.GetString(range))).ToList();

		static void Configure_Text_Select_Repeats_ByCount_IgnoreMatchCase() => state.Configuration = state.NEWindow.neWindowUI.RunDialog_Configure_Text_Select_Repeats_ByCount_IgnoreMatchCase();

		void Execute_Text_Select_Repeats_ByCount_IgnoreMatchCase(bool caseSensitive)
		{
			var result = state.Configuration as Configuration_Text_Select_Repeats_ByCount_IgnoreMatchCase;
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

		static void PreExecute_Text_Select_Repeats_BetweenFiles_Ordered_MatchMismatch_IgnoreMatchCase(bool caseSensitive)
		{
			var preExecution = new PreExecution_Text_Select_Repeats_BetweenFiles_MatchMismatch_IgnoreMatchCase();
			foreach (var neFile in state.NEWindow.ActiveFiles)
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
			state.PreExecution = preExecution;
		}

		void Execute_Text_Select_Repeats_BetweenFiles_MatchMismatch_IgnoreMatchCase(bool match)
		{
			var matches = (state.PreExecution as PreExecution_Text_Select_Repeats_BetweenFiles_MatchMismatch_IgnoreMatchCase).Matches;
			Selections = Selections.Where((range, index) => (matches[index] != null) == match).ToList();
		}

		static void PreExecute_Text_Select_Repeats_BetweenFiles_Unordered_MatchMismatch_IgnoreMatchCase(bool caseSensitive)
		{
			var preExecution = new PreExecution_Text_Select_Repeats_BetweenFiles_CommonNonCommon_IgnoreMatchCase();
			var stringComparer = caseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
			foreach (var neFile in state.NEWindow.ActiveFiles)
			{
				var repeats = neFile.Selections.AsTaskRunner().GroupBy(neFile.Text.GetString, stringComparer).ToDictionary(g => g.Key, g => g.Count(), stringComparer);

				if (preExecution.Repeats != null)
					repeats = repeats.Join(preExecution.Repeats, pair => pair.Key, pair => pair.Key, (r1, r2) => new { r1.Key, Value = Math.Min(r1.Value, r2.Value) }, repeats.Comparer).ToDictionary(obj => obj.Key, obj => obj.Value, repeats.Comparer);

				preExecution.Repeats = repeats;
			}
			state.PreExecution = preExecution;
		}

		void Execute_Text_Select_Repeats_BetweenFiles_CommonNonCommon_IgnoreMatchCase(bool match)
		{
			var repeats = (state.PreExecution as PreExecution_Text_Select_Repeats_BetweenFiles_CommonNonCommon_IgnoreMatchCase).Repeats;
			repeats = repeats.ToDictionary(pair => pair.Key, pair => pair.Value, repeats.Comparer);
			Selections = Selections.Where(range =>
			{
				var str = Text.GetString(range);
				return ((repeats.ContainsKey(str)) && (repeats[str]-- > 0)) == match;
			}).ToList();
		}

		static void Configure_Text_Select_ByWidth() => state.Configuration = state.NEWindow.neWindowUI.RunDialog_Configure_Text_SelectWidth_ByWidth(false, true, state.NEWindow.Focused.GetVariables());

		void Execute_Text_Select_ByWidth()
		{
			var result = state.Configuration as Configuration_Text_SelectWidth_ByWidth;
			var results = GetExpressionResults<int>(result.Expression, Selections.Count());
			Selections = Selections.AsTaskRunner().Where((range, index) => range.Length == results[index]).ToList();
		}

		void Execute_Text_Select_MinMax_Text(bool max)
		{
			if (!Selections.Any())
				throw new Exception("No selections");

			var strings = GetSelectionStrings();
			var find = max ? strings.OrderByDescending().First() : strings.OrderBy().First();
			Selections = strings.Indexes(str => str == find).Select(index => Selections[index]).ToList();
		}

		void Execute_Text_Select_MinMax_Length(bool max)
		{
			if (!Selections.Any())
				throw new Exception("No selections");

			var lengths = Selections.Select(range => range.Length).ToList();
			var find = max ? lengths.OrderByDescending().First() : lengths.OrderBy().First();
			Selections = lengths.Indexes(length => length == find).Select(index => Selections[index]).ToList();
		}

		void Execute_Text_Select_ToggleOpenClose()
		{
			Selections = Selections.AsTaskRunner().Select(range =>
			{
				var newPos = GetOppositeBracket(range.Cursor);
				if (newPos == -1)
					return range;

				return MoveCursor(range, newPos, state.ShiftDown);
			}).ToList();
		}

		static void Configure_Text_Find_Find()
		{
			string text = null;
			var selectionOnly = state.NEWindow.Focused.Selections.Any(range => range.HasSelection);

			if (state.NEWindow.Focused.Selections.Count == 1)
			{
				var sel = state.NEWindow.Focused.Selections.Single();
				if ((selectionOnly) && (state.NEWindow.Focused.Text.GetPositionLine(sel.Cursor) == state.NEWindow.Focused.Text.GetPositionLine(sel.Anchor)) && (sel.Length < 1000))
				{
					selectionOnly = false;
					text = state.NEWindow.Focused.Text.GetString(sel);
				}
			}

			state.Configuration = state.NEWindow.neWindowUI.RunDialog_Configure_Text_Find_Find(text, selectionOnly, state.NEWindow.Focused.ViewBinaryCodePages, state.NEWindow.Focused.GetVariables());
		}

		void Execute_Text_Find_Find()
		{
			var result = state.Configuration as Configuration_Text_Find_Find;
			// Determine selections to search
			List<NERange> selections;
			var firstMatchOnly = (result.KeepMatching) || (result.RemoveMatching);
			if (result.Type == Configuration_Text_Find_Find.ResultType.FindNext)
			{
				firstMatchOnly = true;
				selections = new List<NERange>();
				for (var ctr = 0; ctr < Selections.Count; ++ctr)
					selections.Add(new NERange(ctr + 1 == Selections.Count ? Text.Length : Selections[ctr + 1].Start, Selections[ctr].End));
			}
			else if (result.SelectionOnly)
				selections = Selections.ToList();
			else
				selections = new List<NERange> { NERange.FromIndex(0, Text.Length) };

			if (!selections.Any())
				return;

			// For each selection, determine strings to find. The boolean is for MatchCase, since even if MatchCase is false some should be true (INT16LE 30000 = 0u, NOT 0U)
			List<List<(string, bool)>> stringsToFind;

			if (result.IsExpression)
			{
				var expressionResults = GetExpressionResults<string>(result.Text, result.AlignSelections ? selections.Count : default(int?));
				if (result.AlignSelections)
				{
					if (result.IsBoolean) // Either KeepMatching or RemoveMatching will also be true
					{
						Selections = Selections.Where((range, index) => (expressionResults[index] == "True") == result.KeepMatching).ToList();
						return;
					}

					stringsToFind = selections.Select((x, index) => new List<(string, bool)> { (expressionResults[index], result.MatchCase) }).ToList();
				}
				else
					stringsToFind = Enumerable.Repeat(expressionResults.Select(x => (x, result.MatchCase)).ToList(), selections.Count).ToList();
			}
			else
				stringsToFind = Enumerable.Repeat(new List<(string, bool)> { (result.Text, result.MatchCase) }, selections.Count).ToList();

			ViewBinarySearches = null;
			// If the strings are binary convert them to all codepages
			if (result.IsBinary)
			{
				ViewBinaryCodePages = result.CodePages;
				ViewBinarySearches = stringsToFind.SelectMany().Distinct().GroupBy(x => x.Item2).Select(g => new HashSet<string>(g.Select(x => x.Item1), g.Key ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase)).ToList();
				var mapping = stringsToFind
					.Distinct()
					.ToDictionary(
						list => list,
						list => list.SelectMany(
							item => result.CodePages
								.Select(codePage => (Coder.TryStringToBytes(item.Item1, codePage), (item.Item2) || (Coder.AlwaysCaseSensitive(codePage))))
								.NonNull(tuple => tuple.Item1)
								.Select(tuple => (Coder.TryBytesToString(tuple.Item1, CodePage), tuple.Item2))
								.NonNullOrEmpty(tuple => tuple.Item1)
							).Distinct().ToList());

				stringsToFind = stringsToFind.Select(list => mapping[list]).ToList();
			}

			// Create searchers
			Dictionary<List<(string, bool)>, ISearcher> searchers;
			if (result.IsRegex)
			{
				searchers = stringsToFind
					.Distinct()
					.ToDictionary(
						list => list,
						list => new RegexesSearcher(list.Select(x => x.Item1).ToList(), result.WholeWords, result.MatchCase, result.EntireSelection, firstMatchOnly, result.RegexGroups) as ISearcher);
			}
			else
			{
				searchers = stringsToFind
					.Distinct()
					.ToDictionary(
						list => list,
						list =>
						{
							if (list.Count == 1)
								return new StringSearcher(list[0].Item1, result.WholeWords, list[0].Item2, result.SkipSpace, result.EntireSelection, firstMatchOnly) as ISearcher;
							return new StringsSearcher(list, result.WholeWords, result.EntireSelection, firstMatchOnly);
						});
			}

			// Perform search
			var results = selections.AsTaskRunner().Select((range, index) => searchers[stringsToFind[index]].Find(Text.GetString(range), range.Start)).ToList();

			switch (result.Type)
			{
				case Configuration_Text_Find_Find.ResultType.CopyCount:
					Clipboard = results.Select(list => list.Count.ToString()).ToList();
					break;
				case Configuration_Text_Find_Find.ResultType.FindNext:
					var newSels = new List<NERange>();
					for (var ctr = 0; ctr < Selections.Count; ++ctr)
					{
						int endPos;
						if (results[ctr].Count >= 1)
							endPos = results[ctr][0].End;
						else if (ctr + 1 < Selections.Count)
							endPos = Selections[ctr + 1].Start;
						else
							endPos = Text.Length;
						newSels.Add(new NERange(Selections[ctr].Start, endPos));
					}
					Selections = newSels;
					break;
				case Configuration_Text_Find_Find.ResultType.FindAll:
					if ((result.KeepMatching) || (result.RemoveMatching))
						Selections = selections.Where((range, index) => results[index].Any() == result.KeepMatching).ToList();
					else
						Selections = results.SelectMany().ToList();
					break;
			}
		}

		static void Configure_Text_Find_RegexReplace()
		{
			string text = null;
			var selectionOnly = state.NEWindow.Focused.Selections.Any(range => range.HasSelection);

			if (state.NEWindow.Focused.Selections.Count == 1)
			{
				var sel = state.NEWindow.Focused.Selections.Single();
				if ((selectionOnly) && (state.NEWindow.Focused.Text.GetPositionLine(sel.Cursor) == state.NEWindow.Focused.Text.GetPositionLine(sel.Anchor)) && (sel.Length < 1000))
				{
					selectionOnly = false;
					text = state.NEWindow.Focused.Text.GetString(sel);
				}
			}

			state.Configuration = state.NEWindow.neWindowUI.RunDialog_Configure_Text_Find_RegexReplace(text, selectionOnly);
		}

		void Execute_Text_Find_RegexReplace()
		{
			var result = state.Configuration as Configuration_Text_Find_RegexReplace;
			var regions = result.SelectionOnly ? Selections.ToList() : new List<NERange> { NERange.FromIndex(0, Text.Length) };
			var searcher = new RegexesSearcher(new List<string> { result.Text }, result.WholeWords, result.MatchCase, result.EntireSelection);
			var sels = regions.AsTaskRunner().SelectMany(region => searcher.Find(Text.GetString(region), region.Start)).ToList();
			Selections = sels;
			ReplaceSelections(Selections.AsTaskRunner().Select(range => searcher.regexes[0].Replace(Text.GetString(range), result.Replace)).ToList());
		}

		static void Configure_Text_Trim() => state.Configuration = state.NEWindow.neWindowUI.RunDialog_Configure_Text_SelectTrim_WholeBoundedWordTrim(0);

		void Execute_Text_Trim()
		{
			var result = state.Configuration as Configuration_Text_SelectTrim_WholeBoundedWordTrim;
			ReplaceSelections(Selections.AsTaskRunner().Select(str => TrimString(Text.GetString(str), result)).ToList());
		}

		static void Configure_Text_Width()
		{
			var numeric = state.NEWindow.Focused.Selections.Any() ? state.NEWindow.Focused.Selections.AsTaskRunner().All(range => state.NEWindow.Focused.Text.GetString(range).IsNumeric()) : false;
			state.Configuration = state.NEWindow.neWindowUI.RunDialog_Configure_Text_SelectWidth_ByWidth(numeric, false, state.NEWindow.Focused.GetVariables());
		}

		void Execute_Text_Width()
		{
			var result = state.Configuration as Configuration_Text_SelectWidth_ByWidth;
			var results = GetExpressionResults<int>(result.Expression, Selections.Count());
			ReplaceSelections(Selections.AsTaskRunner().Select((range, index) => SetWidth(Text.GetString(range), result, results[index])).ToList());
		}

		void Execute_Text_SingleLine() => ReplaceSelections(Selections.AsTaskRunner().Select(range => Text.GetString(range).Replace("\r", "").Replace("\n", "")).ToList());

		void Execute_Text_Case_Upper() => ReplaceSelections(Selections.AsTaskRunner().Select(range => Text.GetString(range).ToUpperInvariant()).ToList());

		void Execute_Text_Case_Lower() => ReplaceSelections(Selections.AsTaskRunner().Select(range => Text.GetString(range).ToLowerInvariant()).ToList());

		void Execute_Text_Case_Proper() => ReplaceSelections(Selections.AsTaskRunner().Select(range => Text.GetString(range).ToProper()).ToList());

		void Execute_Text_Case_Invert() => ReplaceSelections(Selections.AsTaskRunner().Select(range => Text.GetString(range).ToToggled()).ToList());

		static void Configure_Text_Sort() => state.Configuration = state.NEWindow.neWindowUI.RunDialog_Configure_Text_Sort();

		void Execute_Text_Sort()
		{
			var result = state.Configuration as Configuration_Text_Sort;
			var regions = GetSortSource(result.SortScope, result.UseRegion);
			var ordering = GetOrdering(result.SortType, result.CaseSensitive, result.Ascending);
			if (regions.Count != ordering.Count)
				throw new Exception("Ordering misaligned");

			var newSelections = Selections.ToList();
			var orderedRegions = ordering.Select(index => regions[index]).ToList();
			var orderedRegionText = orderedRegions.Select(range => Text.GetString(range)).ToList();

			Replace(regions, orderedRegionText);

			var newRegions = regions.ToList();
			var add = 0;
			for (var ctr = 0; ctr < newSelections.Count; ++ctr)
			{
				var orderCtr = ordering[ctr];
				newSelections[orderCtr] = new NERange(newSelections[orderCtr].Anchor - regions[orderCtr].Start + regions[ctr].Start + add, newSelections[orderCtr].Cursor - regions[orderCtr].Start + regions[ctr].Start + add);
				newRegions[orderCtr] = new NERange(newRegions[orderCtr].Anchor - regions[orderCtr].Start + regions[ctr].Start + add, newRegions[orderCtr].Cursor - regions[orderCtr].Start + regions[ctr].Start + add);
				add += orderedRegionText[ctr].Length - regions[ctr].Length;
			}
			newSelections = ordering.Select(num => newSelections[num]).ToList();

			Selections = newSelections;
			if (result.SortScope == SortScope.Regions)
				SetRegions(result.UseRegion, newRegions);
		}

		void Execute_Text_Escape_Markup() => ReplaceSelections(Selections.AsTaskRunner().Select(range => HttpUtility.HtmlEncode(Text.GetString(range))).ToList());

		void Execute_Text_Escape_Regex() => ReplaceSelections(Selections.AsTaskRunner().Select(range => Regex.Escape(Text.GetString(range))).ToList());

		void Execute_Text_Escape_URL() => ReplaceSelections(Selections.AsTaskRunner().Select(range => HttpUtility.UrlEncode(Text.GetString(range))).ToList());

		void Execute_Text_Unescape_Markup() => ReplaceSelections(Selections.AsTaskRunner().Select(range => HttpUtility.HtmlDecode(Text.GetString(range))).ToList());

		void Execute_Text_Unescape_Regex() => ReplaceSelections(Selections.AsTaskRunner().Select(range => Regex.Unescape(Text.GetString(range))).ToList());

		void Execute_Text_Unescape_URL() => ReplaceSelections(Selections.AsTaskRunner().Select(range => HttpUtility.UrlDecode(Text.GetString(range))).ToList());

		static void Configure_Text_Random() => state.Configuration = state.NEWindow.neWindowUI.RunDialog_Configure_Text_Random(state.NEWindow.Focused.GetVariables());

		void Execute_Text_Random()
		{
			var result = state.Configuration as Configuration_Text_Random;
			var results = GetExpressionResults<int>(result.Expression, Selections.Count());
			ReplaceSelections(Selections.AsTaskRunner().Select((range, index) => GetRandomData(result.Chars, results[index])).ToList());
		}

		static void Configure_Text_Advanced_Unicode() => state.Configuration = state.NEWindow.neWindowUI.RunDialog_Configure_Text_Advanced_Unicode();

		void Execute_Text_Advanced_Unicode()
		{
			var result = state.Configuration as Configuration_Text_Advanced_Unicode;
			ReplaceSelections(result.Value);
		}

		static void Configure_Text_Advanced_FirstDistinct() => state.Configuration = state.NEWindow.neWindowUI.RunDialog_Configure_Text_Advanced_FirstDistinct();

		void Execute_Text_Advanced_FirstDistinct()
		{
			var result = state.Configuration as Configuration_Text_Advanced_FirstDistinct;
			TaskRunner.Run(progress =>
			{
				var valid = new HashSet<char>(result.Chars.Select(ch => result.MatchCase ? ch : char.ToLowerInvariant(ch)));
				var data = GetSelectionStrings().Select(str => result.MatchCase ? str : str.ToLowerInvariant()).Select((str, strIndex) => Tuple.Create(str, strIndex, str.Indexes(ch => valid.Contains(ch)).Distinct(index => str[index]).ToList())).OrderBy(tuple => tuple.Item3.Count).ToList();
				var chars = data.Select(tuple => tuple.Item3.Select(index => tuple.Item1[index]).ToList()).ToList();

				var onChar = new int[chars.Count];
				var current = 0;
				onChar[0] = -1;
				var best = default(int[]);
				var bestScore = int.MaxValue;
				var used = new HashSet<char>();
				var currentScore = 0;
				var score = new int[chars.Count + 1];
				var moveBack = false;

				while (true)
				{
					progress(0); // Will throw if task has been canceled
					if (moveBack)
					{
						currentScore -= score[current];
						score[current] = 0;
						--current;
						if (current < 0)
							break;
						used.Remove(chars[current][onChar[current]]);
						moveBack = false;
					}

					++onChar[current];
					if ((onChar[current] >= chars[current].Count) || (currentScore >= bestScore))
					{
						moveBack = true;
						continue;
					}

					var ch = chars[current][onChar[current]];
					++score[current];
					++currentScore;

					if (used.Contains(ch))
						continue;

					used.Add(ch);

					++current;
					if (current == chars.Count)
					{
						// Found combination!
						if (currentScore < bestScore)
						{
							bestScore = currentScore;
							best = onChar.ToArray();
						}
						moveBack = true;
						continue;
					}

					onChar[current] = -1;
				}

				if (best == null)
					throw new ArgumentException("No distinct combinations available");

				var map = new int[data.Count];
				for (var ctr = 0; ctr < data.Count; ++ctr)
					map[data[ctr].Item2] = ctr;

				Selections = Selections.Select((range, index) => NERange.FromIndex(range.Start + data[map[index]].Item3[best[map[index]]], 1)).ToList();
			});
		}

		void Execute_Text_Advanced_GUID() => ReplaceSelections(Selections.AsTaskRunner().Select(range => Guid.NewGuid().ToString()).ToList());

		static void Configure_Text_Advanced_ReverseRegex()
		{
			if (state.NEWindow.Focused.Selections.Count != 1)
				throw new Exception("Must have one selection.");

			state.Configuration = state.NEWindow.neWindowUI.RunDialog_Configure_Text_Advanced_ReverseRegex();
		}

		void Execute_Text_Advanced_ReverseRegex()
		{
			var result = state.Configuration as Configuration_Text_Advanced_ReverseRegex;
			if (Selections.Count != 1)
				throw new Exception("Must have one selection.");

			var data = RevRegExVisitor.Parse(result.RegEx, result.InfiniteCount);
			var output = data.GetPossibilities().Select(str => str + Text.DefaultEnding).ToList();
			ReplaceSelections(string.Join("", output));

			var start = Selections.Single().Start;
			var sels = new List<NERange>();
			foreach (var str in output)
			{
				sels.Add(NERange.FromIndex(start, str.Length - Text.DefaultEnding.Length));
				start += str.Length;
			}
			Selections = sels;
		}
	}
}
