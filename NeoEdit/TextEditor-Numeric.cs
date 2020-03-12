using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Numerics;
using NeoEdit.Program.Dialogs;
using NeoEdit.Program.Expressions;
using NeoEdit.Program.Transform;

namespace NeoEdit.Program
{
	partial class TextEditor
	{
		static string ConvertBase(string str, Dictionary<char, int> inputSet, Dictionary<int, char> outputSet)
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

		static double Cycle(double value, double minimum, double maximum, bool includeBeginning)
		{
			var range = maximum - minimum;
			if (range <= 0)
				throw new Exception("Minimum must be less than maximum.");
			value -= minimum;
			var mult = (int)(value / range);
			if (value < 0)
				--mult;
			value -= mult * range;
			if ((!includeBeginning) && (value == 0))
				value += range;
			value += minimum;
			return value;
		}

		static string Factor(BigInteger value)
		{
			var factors = new List<BigInteger>();
			if (value < 0)
			{
				factors.Add(-1);
				value = -value;
			}

			BigInteger factor = 2;
			while (value > 1)
			{
				if (value % factor == 0)
				{
					factors.Add(factor);
					value /= factor;
					continue;
				}

				++factor;
			}

			if (!factors.Any())
				factors.Add(value);

			factors.Reverse();

			return string.Join("*", factors);
		}

		static void GetFraction(string str, out BigInteger numerator, out BigInteger denominator)
		{
			if (str.IndexOf('.') == -1)
			{
				numerator = BigInteger.Parse(str);
				denominator = 1;
				return;
			}

			var value = double.Parse(str);
			for (var mult = 1; mult < 1000000; ++mult)
			{
				var val = value * mult;
				var rounded = Math.Round(val);
				if (Math.Abs(val - rounded) <= 1E-8)
				{
					numerator = (BigInteger)rounded;
					denominator = mult;
					return;
				}
			}

			throw new Exception($"Failed to convert {str} to fraction");
		}

		static double Limit(double minimum, double value, double maximum)
		{
			if (minimum > maximum)
				throw new Exception("Minimum must be less than maximum.");
			if (value < minimum)
				value = minimum;
			if (value > maximum)
				value = maximum;
			return value;
		}

		static string SimplifyFraction(string str)
		{
			var idx = str.IndexOf('/');
			if (idx == -1)
			{
				idx = str.Length;
				str += "/1";
			}

			GetFraction(str.Remove(idx), out var num, out var numMult);
			GetFraction(str.Substring(idx + 1), out var den, out var denMult);

			num *= denMult;
			den *= numMult;

			var gcf = Helpers.GCF(num, den);
			num /= gcf;
			den /= gcf;

			if (den == 1)
				return num.ToString();
			else
				return $"{num}/{den}";
		}

		static string TrimNumeric(string number)
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

		void Command_Numeric_Select_MinMax(bool max)
		{
			if (!Selections.Any())
				throw new Exception("No selections");

			var values = Selections.AsParallel().AsOrdered().Select(range => new NumericValue(Text.GetString(range))).ToList();
			var find = max ? values.OrderByDescending().First() : values.OrderBy().First();
			Selections = values.Indexes(value => value == find).Select(index => Selections[index]).ToList();
		}

		void Command_Numeric_Select_Fraction_Whole()
		{
			Selections = Selections.AsParallel().AsOrdered().Select(range =>
			{
				var str = Text.GetString(range);
				var idx = str.IndexOf('.');
				if (idx == -1)
					return range;
				return Range.FromIndex(range.Start, idx);
			}).ToList();
		}

		void Command_Numeric_Select_Fraction_Fraction()
		{
			Selections = Selections.AsParallel().AsOrdered().Select(range =>
			{
				var str = Text.GetString(range);
				var idx = str.IndexOf('.');
				if (idx == -1)
					return Range.FromIndex(range.End, 0);
				return new Range(range.End, range.Start + idx);
			}).ToList();
		}

		void Command_Numeric_Hex_ToHex() => ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => BigInteger.Parse(Text.GetString(range)).ToString("x").TrimStart('0')).Select(str => str.Length == 0 ? "0" : str).ToList());

		void Command_Numeric_Hex_FromHex() => ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => BigInteger.Parse("0" + Text.GetString(range), NumberStyles.HexNumber).ToString()).ToList());

		NumericConvertBaseDialog.Result Command_Numeric_ConvertBase_Dialog() => NumericConvertBaseDialog.Run(state.TabsWindow);

		void Command_Numeric_ConvertBase(NumericConvertBaseDialog.Result result) => ReplaceSelections(GetSelectionStrings().Select(str => ConvertBase(str, result.InputSet, result.OutputSet)).ToList());

		void Command_Numeric_Series_ZeroBased() => ReplaceSelections(Selections.Select((range, index) => index.ToString()).ToList());

		void Command_Numeric_Series_OneBased() => ReplaceSelections(Selections.Select((range, index) => (index + 1).ToString()).ToList());

		NumericSeriesDialog.Result Command_Numeric_Series_LinearGeometric_Dialog(bool linear) => NumericSeriesDialog.Run(state.TabsWindow, linear, GetVariables());

		void Command_Numeric_Series_LinearGeometric(NumericSeriesDialog.Result result, bool linear)
		{
			var variables = GetVariables();
			var start = new NEExpression(result.StartExpression).Evaluate<double>(variables);
			var increment = new NEExpression(result.IncrementExpression).Evaluate<double>(variables);
			ReplaceSelections(Selections.Select((range, index) => (linear ? start + increment * index : start * Math.Pow(increment, index)).ToString()).ToList());
		}

		NumericScaleDialog.Result Command_Numeric_Scale_Dialog() => NumericScaleDialog.Run(state.TabsWindow, GetVariables());

		void Command_Numeric_Scale(NumericScaleDialog.Result result)
		{
			var variables = GetVariables();
			var prevMins = new NEExpression(result.PrevMin).EvaluateList<double>(variables, Selections.Count());
			var prevMaxs = new NEExpression(result.PrevMax).EvaluateList<double>(variables, Selections.Count());
			var newMins = new NEExpression(result.NewMin).EvaluateList<double>(variables, Selections.Count());
			var newMaxs = new NEExpression(result.NewMax).EvaluateList<double>(variables, Selections.Count());

			ReplaceSelections(Selections.AsParallel().AsOrdered().Select((range, index) => ((double.Parse(Text.GetString(range)) - prevMins[index]) * (newMaxs[index] - newMins[index]) / (prevMaxs[index] - prevMins[index]) + newMins[index]).ToString()).ToList());
		}

		void Command_Numeric_Add_Sum()
		{
			if (!Selections.Any())
				return;

			var result = Selections.Where(range => !range.HasSelection).FirstOrDefault();
			if (result == null)
				result = Selections[Math.Max(0, Math.Min(CurrentSelection, Selections.Count - 1))];

			var total = new NumericValue(0);
			Selections.Where(range => range.HasSelection).ForEach(range => total += new NumericValue(Text.GetString(range)));
			Selections = new List<Range> { result };
			ReplaceSelections(total.ToString());
		}

		void Command_Numeric_Add_ForwardReverseSum(bool forward, bool undo)
		{
			var numbers = Selections.AsParallel().AsOrdered().Select(range => new NumericValue(Text.GetString(range))).ToList();
			var total = new NumericValue(0);
			var start = forward ? 0 : numbers.Count - 1;
			var end = forward ? numbers.Count : -1;
			var step = forward ? 1 : -1;
			for (var ctr = start; ctr != end; ctr += step)
			{
				if (undo)
					numbers[ctr] -= total;
				total += numbers[ctr];
				if (!undo)
					numbers[ctr] = total;
			}
			ReplaceSelections(numbers.Select(num => num.ToString()).ToList());
		}

		void Command_Numeric_Add_IncrementDecrement(bool add) => ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => (new NumericValue(Text.GetString(range)) + new NumericValue(add ? 1 : -1)).ToString()).ToList());

		void Command_Numeric_Add_AddSubtractClipboard(bool add)
		{
			if (Selections.Count == 0)
				return;

			var clipboardStrings = Clipboard;
			if ((clipboardStrings.Count == 1) && (Selections.Count != 1))
				clipboardStrings = Selections.Select(str => clipboardStrings[0]).ToList();

			if (Selections.Count != clipboardStrings.Count())
				throw new Exception("Must have either one or equal number of clipboards.");

			var mult = new NumericValue(add ? 1 : -1);
			ReplaceSelections(Selections.Zip(clipboardStrings, (sel, clip) => new { sel, clip }).AsParallel().AsOrdered().Select(obj => (new NumericValue(Text.GetString(obj.sel)) + new NumericValue(obj.clip) * mult).ToString()).ToList());
		}

		void Command_Numeric_Fraction_Whole()
		{
			ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => Text.GetString(range)).Select(str =>
			{
				var idx = str.IndexOf('.');
				if (idx == -1)
					return str;
				return str.Substring(0, idx);
			}).ToList());
		}

		void Command_Numeric_Fraction_Fraction()
		{
			ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => Text.GetString(range)).Select(str =>
			{
				var idx = str.IndexOf('.');
				if (idx == -1)
					return "0";
				return str.Substring(idx);
			}).ToList());
		}

		void Command_Numeric_Fraction_Simplify() => ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => Text.GetString(range)).Select(SimplifyFraction).ToList());

		void Command_Numeric_Absolute() => ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => Text.GetString(range).TrimStart('-')).ToList());

		NumericFloorRoundCeilingDialog.Result Command_Numeric_Floor_Dialog() => NumericFloorRoundCeilingDialog.Run(state.TabsWindow, "Floor", GetVariables());

		void Command_Numeric_Floor(NumericFloorRoundCeilingDialog.Result result)
		{
			var baseValue = new NEExpression(result.BaseValue).EvaluateList<double>(GetVariables(), Selections.Count());
			var interval = new NEExpression(result.Interval).EvaluateList<double>(GetVariables(), Selections.Count());
			ReplaceSelections(Selections.AsParallel().AsOrdered().Select((range, index) => (Math.Floor((double.Parse(Text.GetString(range), NumberStyles.Float) - baseValue[index]) / interval[index]) * interval[index] + baseValue[index]).ToString()).ToList());
		}

		NumericFloorRoundCeilingDialog.Result Command_Numeric_Ceiling_Dialog() => NumericFloorRoundCeilingDialog.Run(state.TabsWindow, "Ceiling", GetVariables());

		void Command_Numeric_Ceiling(NumericFloorRoundCeilingDialog.Result result)
		{
			var baseValue = new NEExpression(result.BaseValue).EvaluateList<double>(GetVariables(), Selections.Count());
			var interval = new NEExpression(result.Interval).EvaluateList<double>(GetVariables(), Selections.Count());
			ReplaceSelections(Selections.AsParallel().AsOrdered().Select((range, index) => (Math.Ceiling((double.Parse(Text.GetString(range), NumberStyles.Float) - baseValue[index]) / interval[index]) * interval[index] + baseValue[index]).ToString()).ToList());
		}

		NumericFloorRoundCeilingDialog.Result Command_Numeric_Round_Dialog() => NumericFloorRoundCeilingDialog.Run(state.TabsWindow, "Round", GetVariables());

		void Command_Numeric_Round(NumericFloorRoundCeilingDialog.Result result)
		{
			var baseValue = new NEExpression(result.BaseValue).EvaluateList<double>(GetVariables(), Selections.Count());
			var interval = new NEExpression(result.Interval).EvaluateList<double>(GetVariables(), Selections.Count());
			ReplaceSelections(Selections.AsParallel().AsOrdered().Select((range, index) => (Math.Round((double.Parse(Text.GetString(range), NumberStyles.Float) - baseValue[index]) / interval[index], MidpointRounding.AwayFromZero) * interval[index] + baseValue[index]).ToString()).ToList());
		}

		NumericLimitDialog.Result Command_Numeric_Limit_Dialog() => NumericLimitDialog.Run(state.TabsWindow, GetVariables());

		void Command_Numeric_Limit(NumericLimitDialog.Result result)
		{
			var variables = GetVariables();
			var minimums = new NEExpression(result.Minimum).EvaluateList<double>(variables, Selections.Count());
			var maximums = new NEExpression(result.Maximum).EvaluateList<double>(variables, Selections.Count());

			ReplaceSelections(Selections.AsParallel().AsOrdered().Select((range, index) => Limit(minimums[index], double.Parse(Text.GetString(range)), maximums[index]).ToString()).ToList());
		}

		NumericCycleDialog.Result Command_Numeric_Cycle_Dialog() => NumericCycleDialog.Run(state.TabsWindow, GetVariables());

		void Command_Numeric_Cycle(NumericCycleDialog.Result result)
		{
			var variables = GetVariables();
			var minimums = new NEExpression(result.Minimum).EvaluateList<double>(variables, Selections.Count());
			var maximums = new NEExpression(result.Maximum).EvaluateList<double>(variables, Selections.Count());
			ReplaceSelections(Selections.AsParallel().AsOrdered().Select((range, index) => Cycle(double.Parse(Text.GetString(range)), minimums[index], maximums[index], result.IncludeBeginning).ToString()).ToList());
		}

		void Command_Numeric_Trim() => ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => TrimNumeric(Text.GetString(range))).ToList());

		void Command_Numeric_Factor() => ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => Factor(BigInteger.Parse(Text.GetString(range)))).ToList());

		NumericRandomNumberDialog.Result Command_Numeric_RandomNumber_Dialog() => NumericRandomNumberDialog.Run(state.TabsWindow, GetVariables());

		void Command_Numeric_RandomNumber(NumericRandomNumberDialog.Result result)
		{
			var variables = GetVariables();
			var minValues = new NEExpression(result.MinValue).EvaluateList<int>(variables, Selections.Count());
			var maxValues = new NEExpression(result.MaxValue).EvaluateList<int>(variables, Selections.Count());
			ReplaceSelections(Selections.AsParallel().Select((range, index) => random.Next(minValues[index], maxValues[index] + 1).ToString()).ToList());
		}

		NumericCombinationsPermutationsDialog.Result Command_Numeric_CombinationsPermutations_Dialog()
		{
			if (Selections.Count != 1)
				throw new Exception("Must have one selection.");

			return NumericCombinationsPermutationsDialog.Run(state.TabsWindow);
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

			ReplaceSelections(string.Join("", output.Select(row => string.Join(" ", row) + TextView.DefaultEnding)));

			var start = Selections.Single().Start;
			var sels = new List<Range>();
			foreach (var row in output)
			{
				foreach (var str in row)
				{
					sels.Add(Range.FromIndex(start, str.Length));
					start += str.Length + 1; // +1 is for space
				}
				start += TextView.DefaultEnding.Length - 1; // -1 is for space added before
			}
			Selections = sels;
		}

		NumericMinMaxValuesDialog.Result Command_Numeric_MinMaxValues_Dialog() => NumericMinMaxValuesDialog.Run(state.TabsWindow);

		void Command_Numeric_MinMaxValues(NumericMinMaxValuesDialog.Result result) => ReplaceSelections(string.Join(" ", new List<string> { result.Min ? result.CodePage.MinValue() : null, result.Max ? result.CodePage.MaxValue() : null }.Where(str => !string.IsNullOrEmpty(str))));
	}
}
