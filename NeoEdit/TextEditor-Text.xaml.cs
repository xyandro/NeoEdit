using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using NeoEdit.Common;
using NeoEdit.Common.Dialogs;
using NeoEdit.Common.Parsing;
using NeoEdit.Dialogs;
using NeoEdit.RevRegEx;

namespace NeoEdit
{
	partial class TextEditor
	{
		static string GetRandomData(string chars, int length) => new string(Enumerable.Range(0, length).Select(num => chars[random.Next(chars.Length)]).ToArray());

		static string SetWidth(string str, TextWidthDialog.Result result, int value)
		{
			if (str.Length == value)
				return str;

			if (str.Length > value)
			{
				switch (result.Location)
				{
					case TextWidthDialog.TextLocation.Start: return str.Substring(0, value);
					case TextWidthDialog.TextLocation.Middle: return str.Substring((str.Length - value) / 2, value);
					case TextWidthDialog.TextLocation.End: return str.Substring(str.Length - value);
					default: throw new ArgumentException("Invalid");
				}
			}
			else
			{
				var len = value - str.Length;
				switch (result.Location)
				{
					case TextWidthDialog.TextLocation.Start: return str + new string(result.PadChar, len);
					case TextWidthDialog.TextLocation.Middle: return new string(result.PadChar, len / 2) + str + new string(result.PadChar, (len + 1) / 2);
					case TextWidthDialog.TextLocation.End: return new string(result.PadChar, len) + str;
					default: throw new ArgumentException("Invalid");
				}
			}
		}

		static Range TrimRange(ITextEditor te, Range range, TextTrimDialog.Result result)
		{
			var index = range.Start;
			var length = range.Length;
			te.Data.Trim(ref index, ref length, result.TrimChars, result.Start, result.End);
			if ((index == range.Start) && (length == range.Length))
				return range;
			return Range.FromIndex(index, length);
		}

		static string TrimString(string str, TextTrimDialog.Result result)
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

		static public TextTrimDialog.Result Command_Text_Select_Trim_Dialog(ITextEditor te) => TextTrimDialog.Run(te.WindowParent);

		static public void Command_Text_Select_Trim(ITextEditor te, TextTrimDialog.Result result) => te.SetSelections(te.Selections.AsParallel().AsOrdered().Select(range => TrimRange(te, range, result)).ToList());

		static public TextWidthDialog.Result Command_Text_Select_ByWidth_Dialog(ITextEditor te) => TextWidthDialog.Run(te.WindowParent, false, true, te.GetVariables());

		static public void Command_Text_Select_ByWidth(ITextEditor te, TextWidthDialog.Result result)
		{
			var results = te.GetFixedExpressionResults<int>(result.Expression);
			te.SetSelections(te.Selections.AsParallel().AsOrdered().Where((range, index) => range.Length == results[index]).ToList());
		}

		static public TextSelectWholeBoundedWordDialog.Result Command_Text_Select_WholeBoundedWord_Dialog(ITextEditor te, bool wholeWord) => TextSelectWholeBoundedWordDialog.Run(te.WindowParent, wholeWord);

		static public void Command_Text_Select_WholeBoundedWord(ITextEditor te, TextSelectWholeBoundedWordDialog.Result result, bool wholeWord)
		{
			var minOffset = te.BeginOffset;
			var maxOffset = te.EndOffset;

			var sels = new List<Range>();
			foreach (var range in te.Selections)
			{
				var startOffset = range.Start;
				var endOffset = range.End;

				if (result.Start)
					while ((startOffset > minOffset) && (result.Chars.Contains(te.Data.Data[startOffset - 1]) == wholeWord))
						--startOffset;

				if (result.End)
					while ((endOffset < maxOffset) && (result.Chars.Contains(te.Data.Data[endOffset]) == wholeWord))
						++endOffset;

				sels.Add(new Range(startOffset, endOffset));
			}
			te.SetSelections(sels);
		}

		static public void Command_Text_Select_MinMax_Text(ITextEditor te, bool max)
		{
			if (!te.Selections.Any())
				throw new Exception("No selections");

			var strings = te.GetSelectionStrings();
			var find = max ? strings.OrderByDescending().First() : strings.OrderBy().First();
			te.SetSelections(strings.Indexes(str => str == find).Select(index => te.Selections[index]).ToList());
		}

		static public void Command_Text_Select_MinMax_Length(ITextEditor te, bool max)
		{
			if (!te.Selections.Any())
				throw new Exception("No selections");

			var lengths = te.Selections.Select(range => range.Length).ToList();
			var find = max ? lengths.OrderByDescending().First() : lengths.OrderBy().First();
			te.SetSelections(lengths.Indexes(length => length == find).Select(index => te.Selections[index]).ToList());
		}

		static public void Command_Text_Case_Upper(ITextEditor te) => te.ReplaceSelections(te.Selections.AsParallel().AsOrdered().Select(range => te.GetString(range).ToUpperInvariant()).ToList());

		static public void Command_Text_Case_Lower(ITextEditor te) => te.ReplaceSelections(te.Selections.AsParallel().AsOrdered().Select(range => te.GetString(range).ToLowerInvariant()).ToList());

		static public void Command_Text_Case_Proper(ITextEditor te) => te.ReplaceSelections(te.Selections.AsParallel().AsOrdered().Select(range => te.GetString(range).ToProper()).ToList());

		static public void Command_Text_Case_Toggle(ITextEditor te) => te.ReplaceSelections(te.Selections.AsParallel().AsOrdered().Select(range => te.GetString(range).ToToggled()).ToList());

		static public void Command_Text_Length(ITextEditor te) => te.ReplaceSelections(te.Selections.AsParallel().AsOrdered().Select(range => range.Length.ToString()).ToList());

		static public TextWidthDialog.Result Command_Text_Width_Dialog(ITextEditor te)
		{
			var numeric = te.Selections.Any() ? te.Selections.AsParallel().All(range => te.GetString(range).IsNumeric()) : false;
			return TextWidthDialog.Run(te.WindowParent, numeric, false, te.GetVariables());
		}

		static public void Command_Text_Width(ITextEditor te, TextWidthDialog.Result result)
		{
			var results = te.GetFixedExpressionResults<int>(result.Expression);
			te.ReplaceSelections(te.Selections.AsParallel().AsOrdered().Select((range, index) => SetWidth(te.GetString(range), result, results[index])).ToList());
		}

		static public TextTrimDialog.Result Command_Text_Trim_Dialog(ITextEditor te) => TextTrimDialog.Run(te.WindowParent);

		static public void Command_Text_Trim(ITextEditor te, TextTrimDialog.Result result) => te.ReplaceSelections(te.Selections.AsParallel().AsOrdered().Select(str => TrimString(te.GetString(str), result)).ToList());

		static public void Command_Text_SingleLine(ITextEditor te) => te.ReplaceSelections(te.Selections.AsParallel().AsOrdered().Select(range => te.GetString(range).Replace("\r", "").Replace("\n", "")).ToList());

		static public TextUnicodeDialog.Result Command_Text_Unicode_Dialog(ITextEditor te) => TextUnicodeDialog.Run(te.WindowParent);

		static public void Command_Text_Unicode(ITextEditor te, TextUnicodeDialog.Result result) => te.ReplaceSelections(result.Value);

		static public void Command_Text_GUID(ITextEditor te) => te.ReplaceSelections(te.Selections.AsParallel().Select(range => Guid.NewGuid().ToString()).ToList());

		static public TextRandomTextDialog.Result Command_Text_RandomText_Dialog(ITextEditor te) => TextRandomTextDialog.Run(te.GetVariables(), te.WindowParent);

		static public void Command_Text_RandomText(ITextEditor te, TextRandomTextDialog.Result result)
		{
			var results = te.GetFixedExpressionResults<int>(result.Expression);
			te.ReplaceSelections(te.Selections.AsParallel().AsOrdered().Select((range, index) => GetRandomData(result.Chars, results[index])).ToList());
		}

		static public void Command_Text_LoremIpsum(ITextEditor te) => te.ReplaceSelections(new LoremGenerator().GetSentences().Take(te.Selections.Count).ToList());

		static public TextReverseRegExDialog.Result Command_Text_ReverseRegEx_Dialog(ITextEditor te)
		{
			if (te.Selections.Count != 1)
				throw new Exception("Must have one selection.");

			return TextReverseRegExDialog.Run(te.WindowParent);
		}

		static public void Command_Text_ReverseRegEx(ITextEditor te, TextReverseRegExDialog.Result result)
		{
			if (te.Selections.Count != 1)
				throw new Exception("Must have one selection.");

			var data = RevRegExVisitor.Parse(result.RegEx, result.InfiniteCount);
			var output = data.GetPossibilities().Select(str => str + te.Data.DefaultEnding).ToList();
			te.ReplaceSelections(string.Join("", output));

			var start = te.Selections.Single().Start;
			var sels = new List<Range>();
			foreach (var str in output)
			{
				sels.Add(Range.FromIndex(start, str.Length - te.Data.DefaultEnding.Length));
				start += str.Length;
			}
			te.SetSelections(sels);
		}

		static public TextFirstDistinctDialog.Result Command_Text_FirstDistinct_Dialog(ITextEditor te) => TextFirstDistinctDialog.Run(te.WindowParent);

		static public void Command_Text_FirstDistinct(ITextEditor te, TextFirstDistinctDialog.Result result)
		{
			var opResult = ProgressDialog.Run(te.WindowParent, "Finding characters...", (cancelled, progress) =>
			{
				var valid = new HashSet<char>(result.Chars.Select(ch => result.MatchCase ? ch : char.ToLowerInvariant(ch)));
				var data = te.GetSelectionStrings().Select(str => result.MatchCase ? str : str.ToLowerInvariant()).Select((str, strIndex) => Tuple.Create(str, strIndex, str.Indexes(ch => valid.Contains(ch)).Distinct(index => str[index]).ToList())).OrderBy(tuple => tuple.Item3.Count).ToList();
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

				return te.Selections.Select((range, index) => Range.FromIndex(range.Start + data[map[index]].Item3[best[map[index]]], 1)).ToList();
			}) as List<Range>;

			if (opResult != null)
				te.SetSelections(opResult);
		}

		static public void Command_Text_RepeatCount(ITextEditor te)
		{
			var strs = te.GetSelectionStrings();
			var counts = strs.GroupBy(str => str).ToDictionary(group => group.Key, group => group.Count());
			te.ReplaceSelections(strs.Select(str => counts[str].ToString()).ToList());
		}

		static public void Command_Text_RepeatIndex(ITextEditor te)
		{
			var counts = new Dictionary<string, int>();
			var strs = te.GetSelectionStrings();
			var newStrs = new List<string>();
			foreach (var str in strs)
			{
				if (!counts.ContainsKey(str))
					counts[str] = 0;
				++counts[str];
				newStrs.Add(counts[str].ToString());
			}
			te.ReplaceSelections(newStrs);
		}
	}
}
