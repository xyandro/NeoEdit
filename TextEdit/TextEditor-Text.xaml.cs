﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using NeoEdit.Common;
using NeoEdit.Common.Parsing;
using NeoEdit.GUI.Dialogs;
using NeoEdit.TextEdit.Dialogs;

namespace NeoEdit.TextEdit
{
	partial class TextEditor
	{
		string SetWidth(string str, WidthDialog.Result result, int value)
		{
			if (str.Length == value)
				return str;

			if (str.Length > value)
			{
				switch (result.Location)
				{
					case WidthDialog.TextLocation.Start: return str.Substring(0, value);
					case WidthDialog.TextLocation.Middle: return str.Substring((str.Length - value + 1) / 2, value);
					case WidthDialog.TextLocation.End: return str.Substring(str.Length - value);
					default: throw new ArgumentException("Invalid");
				}
			}
			else
			{
				var len = value - str.Length;
				switch (result.Location)
				{
					case WidthDialog.TextLocation.Start: return str + new string(result.PadChar, len);
					case WidthDialog.TextLocation.Middle: return new string(result.PadChar, (len + 1) / 2) + str + new string(result.PadChar, len / 2);
					case WidthDialog.TextLocation.End: return new string(result.PadChar, len) + str;
					default: throw new ArgumentException("Invalid");
				}
			}
		}

		Range TrimRange(Range range, TrimDialog.Result result)
		{
			var index = range.Start;
			var length = range.Length;
			Data.Trim(ref index, ref length, result.TrimChars, result.Start, result.End);
			if ((index == range.Start) && (length == range.Length))
				return range;
			return Range.FromIndex(index, length);
		}

		string TrimString(string str, TrimDialog.Result result)
		{
			var start = 0;
			var end = str.Length;
			if (result.Start)
			{
				while ((start < end) && (result.TrimChars.Contains(str[start])))
					++start;
			}
			if (result.End)
			{
				while ((start < end) && (result.TrimChars.Contains(str[end - 1])))
					--end;
			}
			return str.Substring(start, end - start);
		}

		TrimDialog.Result Command_Text_Select_Trim_Dialog() => TrimDialog.Run(WindowParent);

		void Command_Text_Select_Trim(TrimDialog.Result result) => Selections.Replace(Selections.AsParallel().AsOrdered().Select(range => TrimRange(range, result)).ToList());

		WidthDialog.Result Command_Text_Select_ByWidth_Dialog() => WidthDialog.Run(WindowParent, false, true, GetVariables());

		void Command_Text_Select_ByWidth(WidthDialog.Result result)
		{
			var results = GetFixedExpressionResults<int>(result.Expression);
			Selections.Replace(Selections.AsParallel().AsOrdered().Where((range, index) => range.Length == results[index]).ToList());
		}

		void Command_Text_Case_Upper() => ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => GetString(range).ToUpperInvariant()).ToList());

		void Command_Text_Case_Lower() => ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => GetString(range).ToLowerInvariant()).ToList());

		void Command_Text_Case_Proper() => ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => GetString(range).ToProper()).ToList());

		void Command_Text_Case_Toggle() => ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => GetString(range).ToToggled()).ToList());

		void Command_Text_Length() => ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => range.Length.ToString()).ToList());

		WidthDialog.Result Command_Text_Width_Dialog()
		{
			var numeric = Selections.Any() ? Selections.AsParallel().All(range => GetString(range).IsNumeric()) : false;
			return WidthDialog.Run(WindowParent, numeric, false, GetVariables());
		}

		void Command_Text_Width(WidthDialog.Result result)
		{
			var results = GetFixedExpressionResults<int>(result.Expression);
			ReplaceSelections(Selections.AsParallel().AsOrdered().Select((range, index) => SetWidth(GetString(range), result, results[index])).ToList());
		}

		TrimDialog.Result Command_Text_Trim_Dialog() => TrimDialog.Run(WindowParent);

		void Command_Text_Trim(TrimDialog.Result result) => ReplaceSelections(Selections.AsParallel().AsOrdered().Select(str => TrimString(GetString(str), result)).ToList());

		void Command_Text_SingleLine() => ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => GetString(range).Replace("\r", "").Replace("\n", "")).ToList());

		void Command_Text_GUID() => ReplaceSelections(Selections.AsParallel().Select(range => Guid.NewGuid().ToString()).ToList());

		RandomDataDialog.Result Command_Text_RandomText_Dialog() => RandomDataDialog.Run(GetVariables(), WindowParent);

		void Command_Text_RandomText(RandomDataDialog.Result result)
		{
			var results = GetFixedExpressionResults<int>(result.Expression);
			ReplaceSelections(Selections.AsParallel().AsOrdered().Select((range, index) => GetRandomData(result.Chars, results[index])).ToList());
		}

		void Command_Text_LoremIpsum() => ReplaceSelections(new LoremGenerator().GetSentences().Take(Selections.Count).ToList());

		RevRegExDialog.Result Command_Text_ReverseRegEx_Dialog()
		{
			if (Selections.Count != 1)
				throw new Exception("Must have one selection.");

			return RevRegExDialog.Run(WindowParent);
		}

		void Command_Text_ReverseRegEx(RevRegExDialog.Result result)
		{
			if (Selections.Count != 1)
				throw new Exception("Must have one selection.");

			var data = RevRegEx.RevRegExVisitor.Parse(result.RegEx);
			var output = data.GetPossibilities().Select(str => str + Data.DefaultEnding).ToList();
			ReplaceSelections(string.Join("", output));

			var start = Selections.Single().Start;
			var sels = new List<Range>();
			foreach (var str in output)
			{
				sels.Add(Range.FromIndex(start, str.Length - Data.DefaultEnding.Length));
				start += str.Length;
			}
			Selections.Replace(sels);
		}

		FirstDistinctDialog.Result Command_Text_FirstDistinct_Dialog() => FirstDistinctDialog.Run(WindowParent);

		void Command_Text_FirstDistinct(FirstDistinctDialog.Result result)
		{
			var opResult = ProgressDialog.Run(WindowParent, "Finding characters...", (cancelled, progress) =>
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
					if (cancelled())
						break;

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

				return Selections.Select((range, index) => Range.FromIndex(range.Start + data[map[index]].Item3[best[map[index]]], 1)).ToList();
			}) as List<Range>;

			if (opResult != null)
				Selections.Replace(opResult);
		}

		void Command_Text_RepeatCount()
		{
			var strs = GetSelectionStrings();
			var counts = strs.GroupBy(str => str).ToDictionary(group => group.Key, group => group.Count());
			ReplaceSelections(strs.Select(str => counts[str].ToString()).ToList());
		}

		void Command_Text_RepeatIndex()
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
