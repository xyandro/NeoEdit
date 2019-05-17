using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using NeoEdit;
using NeoEdit.Dialogs;
using NeoEdit.Expressions;
using NeoEdit.Transform;

namespace NeoEdit
{
	public static class NumericFunctions
	{
		static ThreadSafeRandom random = new ThreadSafeRandom();

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

		static void SelectRegEx(ITextEditor te, string pattern)
		{
			var regex = new Regex(pattern, RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.Multiline | RegexOptions.IgnoreCase);
			var results = te.Selections.AsParallel().AsOrdered().Select(region => te.Data.RegexMatches(regex, region.Start, region.Length, false, false, false)).SelectMany().Select(tuple => Range.FromIndex(tuple.Item1, tuple.Item2)).ToList();
			te.SetSelections(results);
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

		static public void Command_Numeric_Select_MinMax(ITextEditor te, bool max)
		{
			if (!te.Selections.Any())
				throw new Exception("No selections");

			var values = te.Selections.AsParallel().AsOrdered().Select(range => double.Parse(te.GetString(range))).ToList();
			var find = max ? values.OrderByDescending().First() : values.OrderBy().First();
			te.SetSelections(values.Indexes(value => value == find).Select(index => te.Selections[index]).ToList());
		}

		static public void Command_Numeric_Select_Fraction_Whole(ITextEditor te)
		{
			te.SetSelections(te.Selections.AsParallel().AsOrdered().Select(range =>
			{
				var str = te.GetString(range);
				var idx = str.IndexOf('.');
				if (idx == -1)
					return range;
				return Range.FromIndex(range.Start, idx);
			}).ToList());
		}

		static public void Command_Numeric_Select_Fraction_Fraction(ITextEditor te)
		{
			te.SetSelections(te.Selections.AsParallel().AsOrdered().Select(range =>
			{
				var str = te.GetString(range);
				var idx = str.IndexOf('.');
				if (idx == -1)
					return Range.FromIndex(range.End, 0);
				return new Range(range.Start + idx, range.End);
			}).ToList());
		}

		static public void Command_Numeric_Hex_ToHex(ITextEditor te) => te.ReplaceSelections(te.Selections.AsParallel().AsOrdered().Select(range => BigInteger.Parse(te.GetString(range)).ToString("x").TrimStart('0')).Select(str => str.Length == 0 ? "0" : str).ToList());

		static public void Command_Numeric_Hex_FromHex(ITextEditor te) => te.ReplaceSelections(te.Selections.AsParallel().AsOrdered().Select(range => BigInteger.Parse("0" + te.GetString(range), NumberStyles.HexNumber).ToString()).ToList());

		static public NumericConvertBaseDialog.Result Command_Numeric_ConvertBase_Dialog(ITextEditor te) => NumericConvertBaseDialog.Run(te.WindowParent);

		static public void Command_Numeric_ConvertBase(ITextEditor te, NumericConvertBaseDialog.Result result) => te.ReplaceSelections(te.GetSelectionStrings().Select(str => ConvertBase(str, result.InputSet, result.OutputSet)).ToList());

		static public void Command_Numeric_Series_ZeroBased(ITextEditor te) => te.ReplaceSelections(te.Selections.Select((range, index) => index.ToString()).ToList());

		static public void Command_Numeric_Series_OneBased(ITextEditor te) => te.ReplaceSelections(te.Selections.Select((range, index) => (index + 1).ToString()).ToList());

		static public NumericSeriesDialog.Result Command_Numeric_Series_LinearGeometric_Dialog(ITextEditor te, bool linear) => NumericSeriesDialog.Run(te.WindowParent, linear, te.GetVariables());

		static public void Command_Numeric_Series_LinearGeometric(ITextEditor te, NumericSeriesDialog.Result result, bool linear)
		{
			var variables = te.GetVariables();
			var start = new NEExpression(result.StartExpression).Evaluate<double>(variables);
			var increment = new NEExpression(result.IncrementExpression).Evaluate<double>(variables);
			te.ReplaceSelections(te.Selections.Select((range, index) => (linear ? start + increment * index : start * Math.Pow(increment, index)).ToString()).ToList());
		}

		static public NumericScaleDialog.Result Command_Numeric_Scale_Dialog(ITextEditor te) => NumericScaleDialog.Run(te.WindowParent, te.GetVariables());

		static public void Command_Numeric_Scale(ITextEditor te, NumericScaleDialog.Result result)
		{
			var variables = te.GetVariables();
			var prevMins = new NEExpression(result.PrevMin).EvaluateList<double>(variables, te.Selections.Count());
			var prevMaxs = new NEExpression(result.PrevMax).EvaluateList<double>(variables, te.Selections.Count());
			var newMins = new NEExpression(result.NewMin).EvaluateList<double>(variables, te.Selections.Count());
			var newMaxs = new NEExpression(result.NewMax).EvaluateList<double>(variables, te.Selections.Count());

			te.ReplaceSelections(te.Selections.AsParallel().AsOrdered().Select((range, index) => ((double.Parse(te.GetString(range)) - prevMins[index]) * (newMaxs[index] - newMins[index]) / (prevMaxs[index] - prevMins[index]) + newMins[index]).ToString()).ToList());
		}

		static public void Command_Numeric_Add_Sum(ITextEditor te)
		{
			if (!te.Selections.Any())
				return;

			var result = te.Selections.Where(range => !range.HasSelection).FirstOrDefault();
			if (result == null)
				result = te.Selections[Math.Max(0, Math.Min(te.CurrentSelection, te.Selections.Count - 1))];

			var sum = te.Selections.AsParallel().Where(range => range.HasSelection).Select(range => double.Parse(te.GetString(range))).Sum();
			te.SetSelections(new List<Range> { result });
			te.ReplaceSelections(sum.ToString());
		}

		static public void Command_Numeric_Add_ForwardReverseSum(ITextEditor te, bool forward, bool undo)
		{
			var numbers = te.Selections.AsParallel().AsOrdered().Select(range => double.Parse(te.GetString(range))).ToList();
			double total = 0;
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
			te.ReplaceSelections(numbers.Select(num => num.ToString()).ToList());
		}

		static public void Command_Numeric_Add_IncrementDecrement(ITextEditor te, bool add) => te.ReplaceSelections(te.Selections.AsParallel().AsOrdered().Select(range => (double.Parse(te.GetString(range)) + (add ? 1 : -1)).ToString()).ToList());

		static public void Command_Numeric_Add_AddSubtractClipboard(ITextEditor te, bool add)
		{
			if (te.Selections.Count == 0)
				return;

			var clipboardStrings = te.Clipboard;
			if ((clipboardStrings.Count == 1) && (te.Selections.Count != 1))
				clipboardStrings = te.Selections.Select(str => clipboardStrings[0]).ToList();

			if (te.Selections.Count != clipboardStrings.Count())
				throw new Exception("Must have either one or equal number of clipboards.");

			var mult = add ? 1 : -1;
			te.ReplaceSelections(te.Selections.Zip(clipboardStrings, (sel, clip) => new { sel, clip }).AsParallel().AsOrdered().Select(obj => (double.Parse(te.GetString(obj.sel)) + double.Parse(obj.clip) * mult).ToString()).ToList());
		}

		static public void Command_Numeric_Fraction_Whole(ITextEditor te)
		{
			te.ReplaceSelections(te.Selections.AsParallel().AsOrdered().Select(range => te.GetString(range)).Select(str =>
			{
				var idx = str.IndexOf('.');
				if (idx == -1)
					return str;
				return str.Substring(0, idx);
			}).ToList());
		}

		static public void Command_Numeric_Fraction_Fraction(ITextEditor te)
		{
			te.ReplaceSelections(te.Selections.AsParallel().AsOrdered().Select(range => te.GetString(range)).Select(str =>
			{
				var idx = str.IndexOf('.');
				if (idx == -1)
					return "0";
				return str.Substring(idx);
			}).ToList());
		}

		static public void Command_Numeric_Fraction_Simplify(ITextEditor te) => te.ReplaceSelections(te.Selections.AsParallel().AsOrdered().Select(range => te.GetString(range)).Select(SimplifyFraction).ToList());

		static public void Command_Numeric_Absolute(ITextEditor te) => te.ReplaceSelections(te.Selections.AsParallel().AsOrdered().Select(range => te.GetString(range).TrimStart('-')).ToList());

		static public NumericFloorRoundCeilingDialog.Result Command_Numeric_Floor_Dialog(ITextEditor te) => NumericFloorRoundCeilingDialog.Run(te.WindowParent, te.GetVariables());

		static public void Command_Numeric_Floor(ITextEditor te, NumericFloorRoundCeilingDialog.Result result)
		{
			var baseValue = new NEExpression(result.BaseValue).EvaluateList<double>(te.GetVariables(), te.Selections.Count());
			var interval = new NEExpression(result.Interval).EvaluateList<double>(te.GetVariables(), te.Selections.Count());
			te.ReplaceSelections(te.Selections.AsParallel().AsOrdered().Select((range, index) => (Math.Floor((double.Parse(te.GetString(range), NumberStyles.Float) - baseValue[index]) / interval[index]) * interval[index] + baseValue[index]).ToString()).ToList());
		}

		static public NumericFloorRoundCeilingDialog.Result Command_Numeric_Ceiling_Dialog(ITextEditor te) => NumericFloorRoundCeilingDialog.Run(te.WindowParent, te.GetVariables());

		static public void Command_Numeric_Ceiling(ITextEditor te, NumericFloorRoundCeilingDialog.Result result)
		{
			var baseValue = new NEExpression(result.BaseValue).EvaluateList<double>(te.GetVariables(), te.Selections.Count());
			var interval = new NEExpression(result.Interval).EvaluateList<double>(te.GetVariables(), te.Selections.Count());
			te.ReplaceSelections(te.Selections.AsParallel().AsOrdered().Select((range, index) => (Math.Ceiling((double.Parse(te.GetString(range), NumberStyles.Float) - baseValue[index]) / interval[index]) * interval[index] + baseValue[index]).ToString()).ToList());
		}

		static public NumericFloorRoundCeilingDialog.Result Command_Numeric_Round_Dialog(ITextEditor te) => NumericFloorRoundCeilingDialog.Run(te.WindowParent, te.GetVariables());

		static public void Command_Numeric_Round(ITextEditor te, NumericFloorRoundCeilingDialog.Result result)
		{
			var baseValue = new NEExpression(result.BaseValue).EvaluateList<double>(te.GetVariables(), te.Selections.Count());
			var interval = new NEExpression(result.Interval).EvaluateList<double>(te.GetVariables(), te.Selections.Count());
			te.ReplaceSelections(te.Selections.AsParallel().AsOrdered().Select((range, index) => (Math.Round((double.Parse(te.GetString(range), NumberStyles.Float) - baseValue[index]) / interval[index], MidpointRounding.AwayFromZero) * interval[index] + baseValue[index]).ToString()).ToList());
		}

		static public NumericLimitDialog.Result Command_Numeric_Limit_Dialog(ITextEditor te) => NumericLimitDialog.Run(te.WindowParent, te.GetVariables());

		static public void Command_Numeric_Limit(ITextEditor te, NumericLimitDialog.Result result)
		{
			var variables = te.GetVariables();
			var minimums = new NEExpression(result.Minimum).EvaluateList<double>(variables, te.Selections.Count());
			var maximums = new NEExpression(result.Maximum).EvaluateList<double>(variables, te.Selections.Count());

			te.ReplaceSelections(te.Selections.AsParallel().AsOrdered().Select((range, index) => Limit(minimums[index], double.Parse(te.GetString(range)), maximums[index]).ToString()).ToList());
		}

		static public NumericCycleDialog.Result Command_Numeric_Cycle_Dialog(ITextEditor te) => NumericCycleDialog.Run(te.WindowParent, te.GetVariables());

		static public void Command_Numeric_Cycle(ITextEditor te, NumericCycleDialog.Result result)
		{
			var variables = te.GetVariables();
			var minimums = new NEExpression(result.Minimum).EvaluateList<double>(variables, te.Selections.Count());
			var maximums = new NEExpression(result.Maximum).EvaluateList<double>(variables, te.Selections.Count());
			te.ReplaceSelections(te.Selections.AsParallel().AsOrdered().Select((range, index) => Cycle(double.Parse(te.GetString(range)), minimums[index], maximums[index], result.IncludeBeginning).ToString()).ToList());
		}

		static public void Command_Numeric_Trim(ITextEditor te) => te.ReplaceSelections(te.Selections.AsParallel().AsOrdered().Select(range => TrimNumeric(te.GetString(range))).ToList());

		static public void Command_Numeric_Factor(ITextEditor te) => te.ReplaceSelections(te.Selections.AsParallel().AsOrdered().Select(range => Factor(BigInteger.Parse(te.GetString(range)))).ToList());

		static public NumericRandomNumberDialog.Result Command_Numeric_RandomNumber_Dialog(ITextEditor te) => NumericRandomNumberDialog.Run(te.WindowParent, te.GetVariables());

		static public void Command_Numeric_RandomNumber(ITextEditor te, NumericRandomNumberDialog.Result result)
		{
			var variables = te.GetVariables();
			var minValues = new NEExpression(result.MinValue).EvaluateList<int>(variables, te.Selections.Count());
			var maxValues = new NEExpression(result.MaxValue).EvaluateList<int>(variables, te.Selections.Count());
			te.ReplaceSelections(te.Selections.AsParallel().Select((range, index) => random.Next(minValues[index], maxValues[index] + 1).ToString()).ToList());
		}

		static public NumericCombinationsPermutationsDialog.Result Command_Numeric_CombinationsPermutations_Dialog(ITextEditor te)
		{
			if (te.Selections.Count != 1)
				throw new Exception("Must have one selection.");

			return NumericCombinationsPermutationsDialog.Run(te.WindowParent);
		}

		static public void Command_Numeric_CombinationsPermutations(ITextEditor te, NumericCombinationsPermutationsDialog.Result result)
		{
			if (te.Selections.Count != 1)
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

			te.ReplaceSelections(string.Join("", output.Select(row => string.Join(" ", row) + te.Data.DefaultEnding)));

			var start = te.Selections.Single().Start;
			var sels = new List<Range>();
			foreach (var row in output)
			{
				foreach (var str in row)
				{
					sels.Add(Range.FromIndex(start, str.Length));
					start += str.Length + 1; // +1 is for space
				}
				start += te.Data.DefaultEnding.Length - 1; // -1 is for space added before
			}
			te.SetSelections(sels);
		}

		static public NumericMinMaxValuesDialog.Result Command_Numeric_MinMaxValues_Dialog(ITextEditor te) => NumericMinMaxValuesDialog.Run(te.WindowParent);

		static public void Command_Numeric_MinMaxValues(ITextEditor te, NumericMinMaxValuesDialog.Result result) => te.ReplaceSelections(string.Join(" ", new List<string> { result.Min ? result.CodePage.MinValue() : null, result.Max ? result.CodePage.MaxValue() : null }.Where(str => !string.IsNullOrEmpty(str))));
	}
}
