using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Numerics;
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

		private string ConvertBase(string str, Dictionary<char, int> inputSet, Dictionary<int, char> outputSet)
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

		void Command_Numeric_Hex_ToHex() => ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => BigInteger.Parse(GetString(range)).ToString("x").TrimStart('0')).ToList());

		void Command_Numeric_Hex_FromHex() => ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => BigInteger.Parse("0" + GetString(range), NumberStyles.HexNumber).ToString()).ToList());

		void Command_Numeric_Sum()
		{
			var result = Selections.Where(range => !range.HasSelection).ToList();
			if (result.Count != 1)
				throw new Exception("Must have one empty selection to get the result");

			var sum = Selections.AsParallel().Where(range => range.HasSelection).Select(range => double.Parse(GetString(range))).Sum();
			Selections.Replace(result);
			ReplaceSelections(sum.ToString());
		}

		ConvertBaseDialog.Result Command_Numeric_ConvertBase_Dialog() => ConvertBaseDialog.Run(WindowParent);

		void Command_Numeric_ConvertBase(ConvertBaseDialog.Result result) => ReplaceSelections(GetSelectionStrings().Select(str => ConvertBase(str, result.InputSet, result.OutputSet)).ToList());

		void Command_Numeric_Series_ZeroBased() => ReplaceSelections(Selections.Select((range, index) => index.ToString()).ToList());

		void Command_Numeric_Series_OneBased() => ReplaceSelections(Selections.Select((range, index) => (index + 1).ToString()).ToList());

		NumericSeriesDialog.Result Command_Numeric_Series_LinearGeometric_Dialog(bool linear)
		{
			var nonNulls = Selections.AsParallel().AsOrdered().Select((range, index) => new { str = GetString(range), index = index }).Where(obj => !string.IsNullOrWhiteSpace(obj.str)).Select(obj => Tuple.Create(double.Parse(obj.str), obj.index)).ToList();
			if (nonNulls.Count == 0)
				return NumericSeriesDialog.Run(WindowParent, 1, 1);

			if (nonNulls.Count == 1)
				return NumericSeriesDialog.Run(WindowParent, 1, (nonNulls[0].Item1 - 1) / nonNulls[0].Item2);

			var first = nonNulls.First();
			var last = nonNulls.Last();

			var multiplier = linear ? (last.Item1 - first.Item1) / (last.Item2 - first.Item2) : Math.Pow(last.Item1 / first.Item1, 1.0 / (last.Item2 - first.Item2));
			var start = linear ? first.Item1 - multiplier * first.Item2 : first.Item1 / Math.Pow(multiplier, first.Item2);

			return NumericSeriesDialog.Run(WindowParent, start, multiplier);
		}

		void Command_Numeric_Series_Linear(NumericSeriesDialog.Result result) => ReplaceSelections(Selections.Select((range, index) => (result.Multiplier * index + result.Start).ToString()).ToList());

		void Command_Numeric_Series_Geometric(NumericSeriesDialog.Result result) => ReplaceSelections(Selections.Select((range, index) => (Math.Pow(result.Multiplier, index) * result.Start).ToString()).ToList());

		ScaleDialog.Result Command_Numeric_Scale_Dialog() => ScaleDialog.Run(WindowParent);

		void Command_Numeric_Scale(ScaleDialog.Result result)
		{
			var ratio = (result.NewMax - result.NewMin) / (result.PrevMax - result.PrevMin);
			ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => ((double.Parse(GetString(range)) - result.PrevMin) * ratio + result.NewMin).ToString()).ToList());
		}

		void Command_Numeric_ForwardReverseSum(bool forward)
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

		FloorRoundCeilingDialog.Result Command_Numeric_Floor_Dialog() => FloorRoundCeilingDialog.Run(WindowParent);

		void Command_Numeric_Floor(FloorRoundCeilingDialog.Result result) => ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => Floor(decimal.Parse(GetString(range)), result.Interval).ToString()).ToList());

		FloorRoundCeilingDialog.Result Command_Numeric_Round_Dialog() => FloorRoundCeilingDialog.Run(WindowParent);

		void Command_Numeric_Round(FloorRoundCeilingDialog.Result result) => ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => (Math.Round(decimal.Parse(GetString(range)) / result.Interval, MidpointRounding.AwayFromZero) * result.Interval).ToString()).ToList());

		FloorRoundCeilingDialog.Result Command_Numeric_Ceiling_Dialog() => FloorRoundCeilingDialog.Run(WindowParent);

		void Command_Numeric_Ceiling(FloorRoundCeilingDialog.Result result) => ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => Ceiling(decimal.Parse(GetString(range)), result.Interval).ToString()).ToList());

		void Command_Numeric_Factor() => ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => Factor(BigInteger.Parse(GetString(range)))).ToList());

		RandomNumberDialog.Result Command_Numeric_RandomNumber_Dialog() => RandomNumberDialog.Run(WindowParent);

		void Command_Numeric_RandomNumber(RandomNumberDialog.Result result) => ReplaceSelections(Selections.AsParallel().Select(range => random.Next(result.MinValue, result.MaxValue + 1).ToString()).ToList());

		CombinationsPermutationsDialog.Result Command_Numeric_CombinationsPermutations_Dialog()
		{
			if (Selections.Count != 1)
				throw new Exception("Must have one selection.");

			return CombinationsPermutationsDialog.Run(WindowParent);
		}

		void Command_Numeric_CombinationsPermutations(CombinationsPermutationsDialog.Result result)
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
					if (result.Type == CombinationsPermutationsDialog.CombinationsPermutationsType.Combinations)
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

		MinMaxValuesDialog.Result Command_Numeric_MinMaxValues_Dialog() => MinMaxValuesDialog.Run(WindowParent);

		void Command_Numeric_MinMaxValues(MinMaxValuesDialog.Result result) => ReplaceSelections(string.Join(" ", new List<string> { result.Min ? result.CodePage.MinValue() : null, result.Max ? result.CodePage.MaxValue() : null }.Where(str => !string.IsNullOrEmpty(str))));
	}
}
