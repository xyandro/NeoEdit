using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using NeoEdit.Common;
using NeoEdit.Common.Parsing;
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

		Range TrimRange(Range range)
		{
			var index = range.Start;
			var length = range.Length;
			Data.Trim(ref index, ref length);
			if ((index == range.Start) && (length == range.Length))
				return range;
			return Range.FromIndex(index, length);
		}

		string TrimString(string str, TrimDialog.Result result)
		{
			switch (result.Location)
			{
				case TrimDialog.TrimLocation.Start: return str.TrimStart(result.TrimChars);
				case TrimDialog.TrimLocation.Both: return str.Trim(result.TrimChars);
				case TrimDialog.TrimLocation.End: return str.TrimEnd(result.TrimChars);
				default: throw new Exception("Invalid location");
			}
		}

		void Command_Text_Select_Trim() => Selections.Replace(Selections.AsParallel().AsOrdered().Select(range => TrimRange(range)).ToList());

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
			var strs = GetSelectionStrings().Select(str => result.MatchCase ? str : str.ToLowerInvariant()).ToList();
			var valid = new HashSet<char>(result.Chars.Select(ch => result.MatchCase ? ch : char.ToLowerInvariant(ch)));

			var onChar = new int[strs.Count];
			var current = 0;
			onChar[0] = -1;
			var best = default(int[]);
			var bestScore = int.MaxValue;
			var used = new HashSet<char>();
			var currentScore = 0;
			var score = new int[strs.Count + 1];
			var moveBack = false;

			while (true)
			{
				if (moveBack)
				{
					currentScore -= score[current];
					score[current] = 0;
					--current;
					if (current < 0)
						break;
					used.Remove(strs[current][onChar[current]]);
					moveBack = false;
				}

				++onChar[current];
				if ((onChar[current] >= strs[current].Length) || (currentScore - 1 >= bestScore))
				{
					moveBack = true;
					continue;
				}

				var ch = strs[current][onChar[current]];
				if (!valid.Contains(ch))
					continue;

				++score[current];
				++currentScore;

				if (used.Contains(ch))
					continue;

				used.Add(ch);

				++current;
				if (current == strs.Count)
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

			Selections.Replace(Selections.Select((range, index) => Range.FromIndex(range.Start + best[index], 1)).ToList());
		}
	}
}
