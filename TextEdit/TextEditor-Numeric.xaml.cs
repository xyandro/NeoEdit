﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using NeoEdit.Common;
using NeoEdit.Common.Expressions;
using NeoEdit.Common.Transform;
using NeoEdit.TextEdit.Dialogs;

namespace NeoEdit.TextEdit
{
	partial class TextEditor
	{
		decimal Ceiling(decimal number, decimal interval)
		{
			var val = number / interval;
			var intPart = Math.Truncate(val);
			return (intPart + (val - intPart != 0m ? 1 : 0)) * interval;
		}

		string TrimNumeric(string number)
		{
			var whole = number;
			var fraction = "";
			var point = number.IndexOf('.');
			if (point != -1)
			{
				whole = number.Substring(0, point);
				fraction = number.Substring(point);
			}
			number = whole.TrimStart('0', ' ', '\r', '\n', '\t').TrimEnd(' ', '\r', '\n', '\t') + fraction.TrimEnd('0', '.', ' ', '\r', '\n', '\t');
			if (number.Length == 0)
				number = "0";
			return number;
		}

		string ConvertBase(string str, Dictionary<char, int> inputSet, Dictionary<int, char> outputSet)
		{
			BigInteger value = 0;
			for (var ctr = 0; ctr < str.Length; ++ctr)
				value = value * inputSet.Count + inputSet[str[ctr]];
			var output = new LinkedList<char>();
			while ((value != 0) || (output.Count == 0))
			{
				output.AddFirst(outputSet[(int)(value % outputSet.Count)]);
				value /= outputSet.Count;
			}
			return new string(output.ToArray());
		}

		void SelectRegEx(string pattern)
		{
			var regex = new Regex(pattern, RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.Multiline | RegexOptions.IgnoreCase);
			var results = Selections.AsParallel().AsOrdered().Select(region => Data.RegexMatches(regex, region.Start, region.Length, false, false, false)).SelectMany().Select(tuple => Range.FromIndex(tuple.Item1, tuple.Item2)).ToList();
			Selections.Replace(results);
		}

		double Limit(double minimum, double value, double maximum)
		{
			if (value < minimum)
				value = minimum;
			if (value > maximum)
				value = maximum;
			return value;
		}

		void Command_Numeric_Select_Whole()
		{
			Selections.Replace(Selections.AsParallel().AsOrdered().Select(range =>
			{
				var str = GetString(range);
				var idx = str.IndexOf('.');
				if (idx == -1)
					return range;
				return Range.FromIndex(range.Start, idx);
			}).ToList());
		}

		void Command_Numeric_Select_Fraction()
		{
			Selections.Replace(Selections.AsParallel().AsOrdered().Select(range =>
			{
				var str = GetString(range);
				var idx = str.IndexOf('.');
				if (idx == -1)
					return Range.FromIndex(range.End, 0);
				return new Range(range.Start + idx, range.End);
			}).ToList());
		}

		void Command_Numeric_Select_Integer() => SelectRegEx("[0-9]+");

		void Command_Numeric_Select_Float() => SelectRegEx(@"\d*\.?\d+(e[-+]?\d+)?");

		void Command_Numeric_Select_Hex() => SelectRegEx("[0-9a-f]+");

		void Command_Numeric_Hex_ToHex() => ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => BigInteger.Parse(GetString(range)).ToString("x").TrimStart('0')).Select(str => str.Length == 0 ? "0" : str).ToList());

		void Command_Numeric_Hex_FromHex() => ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => BigInteger.Parse("0" + GetString(range), NumberStyles.HexNumber).ToString()).ToList());

		NumericConvertBaseDialog.Result Command_Numeric_ConvertBase_Dialog() => NumericConvertBaseDialog.Run(WindowParent);

		void Command_Numeric_ConvertBase(NumericConvertBaseDialog.Result result) => ReplaceSelections(GetSelectionStrings().Select(str => ConvertBase(str, result.InputSet, result.OutputSet)).ToList());

		void Command_Numeric_Series_ZeroBased() => ReplaceSelections(Selections.Select((range, index) => index.ToString()).ToList());

		void Command_Numeric_Series_OneBased() => ReplaceSelections(Selections.Select((range, index) => (index + 1).ToString()).ToList());

		NumericSeriesDialog.Result Command_Numeric_Series_LinearGeometric_Dialog(bool linear)
		{
			var nonNulls = Selections.AsParallel().AsOrdered().Select((range, index) => new { str = GetString(range), index = index }).NonNullOrWhiteSpace(obj => obj.str).Select(obj => Tuple.Create(double.Parse(obj.str), obj.index)).ToList();
			if (nonNulls.Count == 0)
				return NumericSeriesDialog.Run(WindowParent, linear, 1, 1);
			if (nonNulls.Count == 1)
				return NumericSeriesDialog.Run(WindowParent, linear, nonNulls[0].Item1, 1);

			var first = nonNulls.First();
			var last = nonNulls.Last();

			var increment = linear ? (last.Item1 - first.Item1) / (last.Item2 - first.Item2) : Math.Pow(last.Item1 / first.Item1, 1.0 / (last.Item2 - first.Item2));
			var start = linear ? first.Item1 - increment * first.Item2 : first.Item1 / Math.Pow(increment, first.Item2);

			return NumericSeriesDialog.Run(WindowParent, linear, start, increment);
		}

		void Command_Numeric_Series_Linear(NumericSeriesDialog.Result result) => ReplaceSelections(Selections.Select((range, index) => (result.Increment * index + result.Start).ToString()).ToList());

		void Command_Numeric_Series_Geometric(NumericSeriesDialog.Result result) => ReplaceSelections(Selections.Select((range, index) => (Math.Pow(result.Increment, index) * result.Start).ToString()).ToList());

		NumericScaleDialog.Result Command_Numeric_Scale_Dialog() => NumericScaleDialog.Run(WindowParent, GetVariables());

		void Command_Numeric_Scale(NumericScaleDialog.Result result)
		{
			var variables = GetVariables();
			var prevMin = new NEExpression(result.PrevMin).EvaluateRow<double>(variables);
			var prevMax = new NEExpression(result.PrevMax).EvaluateRow<double>(variables);
			var newMin = new NEExpression(result.NewMin).EvaluateRow<double>(variables);
			var newMax = new NEExpression(result.NewMax).EvaluateRow<double>(variables);

			var ratio = (newMax - newMin) / (prevMax - prevMin);
			ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => ((double.Parse(GetString(range)) - prevMin) * ratio + newMin).ToString()).ToList());
		}

		void Command_Numeric_Add_Sum()
		{
			if (!Selections.Any())
				return;

			var result = Selections.Where(range => !range.HasSelection).FirstOrDefault();
			if (result == null)
				result = Selections[Math.Max(0, Math.Min(CurrentSelection, Selections.Count - 1))];

			var sum = Selections.AsParallel().Where(range => range.HasSelection).Select(range => double.Parse(GetString(range))).Sum();
			Selections.Replace(result);
			ReplaceSelections(sum.ToString());
		}

		void Command_Numeric_Add_ForwardReverseSum(bool forward)
		{
			var numbers = Selections.AsParallel().AsOrdered().Select(range => double.Parse(GetString(range))).ToList();
			double total = 0;
			var start = forward ? 0 : numbers.Count - 1;
			var end = forward ? numbers.Count : -1;
			var step = forward ? 1 : -1;
			for (var ctr = start; ctr != end; ctr += step)
			{
				total += numbers[ctr];
				numbers[ctr] = total;
			}
			ReplaceSelections(numbers.Select(num => num.ToString()).ToList());
		}

		void Numeric_Add_IncrementDecrement(bool add) => ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => (double.Parse(GetString(range)) + (add ? 1 : -1)).ToString()).ToList());

		void Command_Numeric_Add_AddSubtractClipboard(bool add)
		{
			if (Selections.Count == 0)
				return;

			var clipboardStrings = clipboard.Strings;
			if ((clipboardStrings.Count == 1) && (Selections.Count != 1))
				clipboardStrings = Selections.Select(str => clipboardStrings[0]).ToList();

			if (Selections.Count != clipboardStrings.Count())
				throw new Exception("Must have either one or equal number of clipboards.");

			var mult = add ? 1 : -1;
			ReplaceSelections(Selections.Zip(clipboardStrings, (sel, clip) => new { sel, clip }).AsParallel().AsOrdered().Select(obj => (double.Parse(GetString(obj.sel)) + double.Parse(obj.clip) * mult).ToString()).ToList());
		}

		void Command_Numeric_Whole()
		{
			ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => GetString(range)).Select(str =>
			{
				var idx = str.IndexOf('.');
				if (idx == -1)
					return str;
				return str.Substring(0, idx);
			}).ToList());
		}

		void Command_Numeric_Fraction()
		{
			ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => GetString(range)).Select(str =>
			{
				var idx = str.IndexOf('.');
				if (idx == -1)
					return "0";
				return str.Substring(idx);
			}).ToList());
		}

		NumericFloorRoundCeilingDialog.Result Command_Numeric_Floor_Dialog() => NumericFloorRoundCeilingDialog.Run(WindowParent);

		void Command_Numeric_Floor(NumericFloorRoundCeilingDialog.Result result) => ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => Floor(decimal.Parse(GetString(range)), result.Interval).ToString()).ToList());

		NumericFloorRoundCeilingDialog.Result Command_Numeric_Ceiling_Dialog() => NumericFloorRoundCeilingDialog.Run(WindowParent);

		void Command_Numeric_Ceiling(NumericFloorRoundCeilingDialog.Result result) => ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => Ceiling(decimal.Parse(GetString(range)), result.Interval).ToString()).ToList());

		NumericFloorRoundCeilingDialog.Result Command_Numeric_Round_Dialog() => NumericFloorRoundCeilingDialog.Run(WindowParent);

		void Command_Numeric_Round(NumericFloorRoundCeilingDialog.Result result) => ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => (Math.Round(decimal.Parse(GetString(range)) / result.Interval, MidpointRounding.AwayFromZero) * result.Interval).ToString()).ToList());

		NumericLimitDialog.Result Command_Numeric_Limit_Dialog() => NumericLimitDialog.Run(WindowParent, GetVariables());

		void Command_Numeric_Limit(NumericLimitDialog.Result result)
		{
			var variables = GetVariables();
			var minimum = new NEExpression(result.Minimum).EvaluateRow<double>(variables);
			var maximum = new NEExpression(result.Maximum).EvaluateRow<double>(variables);
			if (minimum > maximum)
				throw new Exception("Minimum must be greater than maximum.");

			ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => Limit(minimum, double.Parse(GetString(range)), maximum).ToString()).ToList());
		}

		void Command_Numeric_Trim() => ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => TrimNumeric(GetString(range))).ToList());

		void Command_Numeric_Factor() => ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => Factor(BigInteger.Parse(GetString(range)))).ToList());

		NumericRandomNumberDialog.Result Command_Numeric_RandomNumber_Dialog() => NumericRandomNumberDialog.Run(WindowParent);

		void Command_Numeric_RandomNumber(NumericRandomNumberDialog.Result result) => ReplaceSelections(Selections.AsParallel().Select(range => random.Next(result.MinValue, result.MaxValue + 1).ToString()).ToList());

		NumericCombinationsPermutationsDialog.Result Command_Numeric_CombinationsPermutations_Dialog()
		{
			if (Selections.Count != 1)
				throw new Exception("Must have one selection.");

			return NumericCombinationsPermutationsDialog.Run(WindowParent);
		}

		void Command_Numeric_CombinationsPermutations(NumericCombinationsPermutationsDialog.Result result)
		{
			if (Selections.Count != 1)
				throw new Exception("Must have one selection.");

			var output = new List<List<string>>();
			var nums = new int[result.UseCount];
			var used = new bool[result.ItemCount];
			nums[0] = -1;
			var onNum = 0;
			while (true)
			{
				++nums[onNum];
				if (nums[onNum] >= result.ItemCount)
				{
					--onNum;
					if (onNum < 0)
						break;
					used[nums[onNum]] = false;
					continue;
				}
				if ((!result.Repeat) && (used[nums[onNum]]))
					continue;

				used[nums[onNum]] = true;
				++onNum;
				if (onNum < result.UseCount)
				{
					if (result.Type == NumericCombinationsPermutationsDialog.CombinationsPermutationsType.Combinations)
						nums[onNum] = nums[onNum - 1] - 1;
					else
						nums[onNum] = -1;
				}
				else
				{
					output.Add(nums.Select(num => (num + 1).ToString()).ToList());
					--onNum;
					used[nums[onNum]] = false;
				}
			}

			ReplaceSelections(string.Join("", output.Select(row => string.Join(" ", row) + Data.DefaultEnding)));

			var start = Selections.Single().Start;
			var sels = new List<Range>();
			foreach (var row in output)
			{
				foreach (var str in row)
				{
					sels.Add(Range.FromIndex(start, str.Length));
					start += str.Length + 1; // +1 is for space
				}
				start += Data.DefaultEnding.Length - 1; // -1 is for space added before
			}
			Selections.Replace(sels);
		}

		NumericMinMaxValuesDialog.Result Command_Numeric_MinMaxValues_Dialog() => NumericMinMaxValuesDialog.Run(WindowParent);

		void Command_Numeric_MinMaxValues(NumericMinMaxValuesDialog.Result result) => ReplaceSelections(string.Join(" ", new List<string> { result.Min ? result.CodePage.MinValue() : null, result.Max ? result.CodePage.MaxValue() : null }.Where(str => !string.IsNullOrEmpty(str))));
	}
}
