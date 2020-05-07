using System;
using System.Collections.Generic;
using System.Linq;
using NeoEdit.Common;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Parsing;
using NeoEdit.Common.RevRegEx;
using NeoEdit.TaskRunning;

namespace NeoEdit.Editor
{
	partial class Tab
	{
		static string GetRandomData(string chars, int length) => new string(Enumerable.Range(0, length).Select(num => chars[random.Next(chars.Length)]).ToArray());

		static string SetWidth(string str, Configuration_Text_Select_ByWidth result, int value)
		{
			if (str.Length == value)
				return str;

			if (str.Length > value)
			{
				switch (result.Location)
				{
					case Configuration_Text_Select_ByWidth.TextLocation.Start: return str.Substring(0, value);
					case Configuration_Text_Select_ByWidth.TextLocation.Middle: return str.Substring((str.Length - value) / 2, value);
					case Configuration_Text_Select_ByWidth.TextLocation.End: return str.Substring(str.Length - value);
					default: throw new ArgumentException("Invalid");
				}
			}
			else
			{
				var len = value - str.Length;
				switch (result.Location)
				{
					case Configuration_Text_Select_ByWidth.TextLocation.Start: return str + new string(result.PadChar, len);
					case Configuration_Text_Select_ByWidth.TextLocation.Middle: return new string(result.PadChar, len / 2) + str + new string(result.PadChar, (len + 1) / 2);
					case Configuration_Text_Select_ByWidth.TextLocation.End: return new string(result.PadChar, len) + str;
					default: throw new ArgumentException("Invalid");
				}
			}
		}

		Range TrimRange(Range range, Configuration_Text_Select_Chars result)
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
			return Range.FromIndex(position, length);
		}

		static string TrimString(string str, Configuration_Text_Select_Chars result)
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

		Configuration_Text_Select_Chars Configure_Text_Select_Trim() => Tabs.TabsWindow.Configure_Text_Select_Chars(0);

		void Execute_Text_Select_Trim()
		{
			var result = state.Configuration as Configuration_Text_Select_Chars;
			Selections = Selections.AsParallel().AsOrdered().Select(range => TrimRange(range, result)).ToList();
		}

		Configuration_Text_Select_ByWidth Configure_Text_Select_ByWidth() => Tabs.TabsWindow.Configure_Text_Select_ByWidth(false, true, GetVariables());

		void Execute_Text_Select_ByWidth()
		{
			var result = state.Configuration as Configuration_Text_Select_ByWidth;
			var results = GetExpressionResults<int>(result.Expression, Selections.Count());
			Selections = Selections.AsParallel().AsOrdered().Where((range, index) => range.Length == results[index]).ToList();
		}

		Configuration_Text_Select_Chars Configure_Text_Select_WholeBoundedWord(bool wholeWord) => Tabs.TabsWindow.Configure_Text_Select_Chars(wholeWord ? 1 : 2);

		void Execute_Text_Select_WholeBoundedWord(bool wholeWord)
		{
			var result = state.Configuration as Configuration_Text_Select_Chars;
			var minPosition = 0;
			var maxPosition = TextView.MaxPosition;

			var sels = new List<Range>();
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

				sels.Add(new Range(endPosition, startPosition));
			}
			Selections = sels;
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

		void Execute_Text_Case_Upper() => ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => Text.GetString(range).ToUpperInvariant()).ToList());

		void Execute_Text_Case_Lower() => ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => Text.GetString(range).ToLowerInvariant()).ToList());

		void Execute_Text_Case_Proper() => ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => Text.GetString(range).ToProper()).ToList());

		void Execute_Text_Case_Toggle() => ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => Text.GetString(range).ToToggled()).ToList());

		void Execute_Text_Length() => ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => range.Length.ToString()).ToList());

		Configuration_Text_Select_ByWidth Configure_Text_Width()
		{
			var numeric = Selections.Any() ? Selections.AsParallel().All(range => Text.GetString(range).IsNumeric()) : false;
			return Tabs.TabsWindow.Configure_Text_Select_ByWidth(numeric, false, GetVariables());
		}

		void Execute_Text_Width()
		{
			var result = state.Configuration as Configuration_Text_Select_ByWidth;
			var results = GetExpressionResults<int>(result.Expression, Selections.Count());
			ReplaceSelections(Selections.AsParallel().AsOrdered().Select((range, index) => SetWidth(Text.GetString(range), result, results[index])).ToList());
		}

		Configuration_Text_Select_Chars Configure_Text_Trim() => Tabs.TabsWindow.Configure_Text_Select_Chars(0);

		void Execute_Text_Trim()
		{
			var result = state.Configuration as Configuration_Text_Select_Chars;
			ReplaceSelections(Selections.AsParallel().AsOrdered().Select(str => TrimString(Text.GetString(str), result)).ToList());
		}

		void Execute_Text_SingleLine() => ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => Text.GetString(range).Replace("\r", "").Replace("\n", "")).ToList());

		Configuration_Text_Unicode Configure_Text_Unicode() => Tabs.TabsWindow.Configure_Text_Unicode();

		void Execute_Text_Unicode()
		{
			var result = state.Configuration as Configuration_Text_Unicode;
			ReplaceSelections(result.Value);
		}

		void Execute_Text_GUID() => ReplaceSelections(Selections.AsParallel().Select(range => Guid.NewGuid().ToString()).ToList());

		Configuration_Text_RandomText Configure_Text_RandomText() => Tabs.TabsWindow.Configure_Text_RandomText(GetVariables());

		void Execute_Text_RandomText()
		{
			var result = state.Configuration as Configuration_Text_RandomText;
			var results = GetExpressionResults<int>(result.Expression, Selections.Count());
			ReplaceSelections(Selections.AsParallel().AsOrdered().Select((range, index) => GetRandomData(result.Chars, results[index])).ToList());
		}

		void Execute_Text_LoremIpsum() => ReplaceSelections(new LoremGenerator().GetSentences().Take(Selections.Count).ToList());

		Configuration_Text_ReverseRegEx Configure_Text_ReverseRegEx()
		{
			if (Selections.Count != 1)
				throw new Exception("Must have one selection.");

			return Tabs.TabsWindow.Configure_Text_ReverseRegEx();
		}

		void Execute_Text_ReverseRegEx()
		{
			var result = state.Configuration as Configuration_Text_ReverseRegEx;
			if (Selections.Count != 1)
				throw new Exception("Must have one selection.");

			var data = RevRegExVisitor.Parse(result.RegEx, result.InfiniteCount);
			var output = data.GetPossibilities().Select(str => str + TextView.DefaultEnding).ToList();
			ReplaceSelections(string.Join("", output));

			var start = Selections.Single().Start;
			var sels = new List<Range>();
			foreach (var str in output)
			{
				sels.Add(Range.FromIndex(start, str.Length - TextView.DefaultEnding.Length));
				start += str.Length;
			}
			Selections = sels;
		}

		Configuration_Text_FirstDistinct Configure_Text_FirstDistinct() => Tabs.TabsWindow.Configure_Text_FirstDistinct();

		void Execute_Text_FirstDistinct()
		{
			var result = state.Configuration as Configuration_Text_FirstDistinct;
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
					progress.Current = 0; // Will throw if task has been canceled
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

				Selections = Selections.Select((range, index) => Range.FromIndex(range.Start + data[map[index]].Item3[best[map[index]]], 1)).ToList();
			});
		}

		void Execute_Text_RepeatCount()
		{
			var strs = GetSelectionStrings();
			var counts = strs.GroupBy(str => str).ToDictionary(group => group.Key, group => group.Count());
			ReplaceSelections(strs.Select(str => counts[str].ToString()).ToList());
		}

		void Execute_Text_RepeatIndex()
		{
			var counts = new Dictionary<string, int>();
			var strs = GetSelectionStrings();
			var newStrs = new List<string>();
			foreach (var str in strs)
			{
				if (!counts.ContainsKey(str))
					counts[str] = 0;
				++counts[str];
				newStrs.Add(counts[str].ToString());
			}
			ReplaceSelections(newStrs);
		}
	}
}
