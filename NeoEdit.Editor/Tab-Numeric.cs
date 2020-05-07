using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using NeoEdit.Common;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Expressions;
using NeoEdit.Common.Transform;
using NeoEdit.TaskRunning;

namespace NeoEdit.Editor
{
	partial class Tab
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

		void Execute_Numeric_Select_MinMax(bool max)
		{
			if (!Selections.Any())
				throw new Exception("No selections");

			var values = Selections.AsParallel().AsOrdered().Select(range => new NumericValue(Text.GetString(range))).ToList();
			var find = max ? values.OrderByDescending().First() : values.OrderBy().First();
			Selections = values.Indexes(value => value == find).Select(index => Selections[index]).ToList();
		}

		void Execute_Numeric_Select_Fraction_Whole()
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

		void Execute_Numeric_Select_Fraction_Fraction()
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

		void Execute_Numeric_Hex_ToHex() => ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => BigInteger.Parse(Text.GetString(range)).ToString("x").TrimStart('0')).Select(str => str.Length == 0 ? "0" : str).ToList());

		void Execute_Numeric_Hex_FromHex() => ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => BigInteger.Parse("0" + Text.GetString(range), NumberStyles.HexNumber).ToString()).ToList());

		Configuration_Numeric_ConvertBase Configure_Numeric_ConvertBase() => Tabs.TabsWindow.Configure_Numeric_ConvertBase();

		void Execute_Numeric_ConvertBase()
		{
			var result = state.Configuration as Configuration_Numeric_ConvertBase;
			ReplaceSelections(GetSelectionStrings().Select(str => ConvertBase(str, result.InputSet, result.OutputSet)).ToList());
		}

		void Execute_Numeric_Series_ZeroBased() => ReplaceSelections(Selections.Select((range, index) => index.ToString()).ToList());

		void Execute_Numeric_Series_OneBased() => ReplaceSelections(Selections.Select((range, index) => (index + 1).ToString()).ToList());

		Configuration_Numeric_Series_LinearGeometric Configure_Numeric_Series_LinearGeometric(bool linear) => Tabs.TabsWindow.Configure_Numeric_Series_LinearGeometric(linear, GetVariables());

		void Execute_Numeric_Series_LinearGeometric(bool linear)
		{
			var result = state.Configuration as Configuration_Numeric_Series_LinearGeometric;
			var variables = GetVariables();
			var start = state.GetExpression(result.StartExpression).Evaluate<double>(variables);
			var increment = state.GetExpression(result.IncrementExpression).Evaluate<double>(variables);
			ReplaceSelections(Selections.Select((range, index) => (linear ? start + increment * index : start * Math.Pow(increment, index)).ToString()).ToList());
		}

		Configuration_Numeric_Scale Configure_Numeric_Scale() => Tabs.TabsWindow.Configure_Numeric_Scale(GetVariables());

		void Execute_Numeric_Scale()
		{
			var result = state.Configuration as Configuration_Numeric_Scale;
			var variables = GetVariables();
			var prevMins = state.GetExpression(result.PrevMin).EvaluateList<double>(variables, Selections.Count());
			var prevMaxs = state.GetExpression(result.PrevMax).EvaluateList<double>(variables, Selections.Count());
			var newMins = state.GetExpression(result.NewMin).EvaluateList<double>(variables, Selections.Count());
			var newMaxs = state.GetExpression(result.NewMax).EvaluateList<double>(variables, Selections.Count());

			ReplaceSelections(Selections.AsParallel().AsOrdered().Select((range, index) => ((double.Parse(Text.GetString(range)) - prevMins[index]) * (newMaxs[index] - newMins[index]) / (prevMaxs[index] - prevMins[index]) + newMins[index]).ToString()).ToList());
		}

		void Execute_Numeric_Add_Sum()
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

		void Execute_Numeric_Add_ForwardReverseSum(bool forward, bool undo)
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

		void Execute_Numeric_Add_IncrementDecrement(bool add)
		{
			var toAdd = new NumericValue(add ? 1 : -1);
			ReplaceSelections(Selections.AsTaskRunner().Select(range => (new NumericValue(Text.GetString(range)) + toAdd).ToString()).ToList());
		}

		void Execute_Numeric_Add_AddSubtractClipboard(bool add)
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

		void Execute_Numeric_Fraction_Whole()
		{
			ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => Text.GetString(range)).Select(str =>
			{
				var idx = str.IndexOf('.');
				if (idx == -1)
					return str;
				return str.Substring(0, idx);
			}).ToList());
		}

		void Execute_Numeric_Fraction_Fraction()
		{
			ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => Text.GetString(range)).Select(str =>
			{
				var idx = str.IndexOf('.');
				if (idx == -1)
					return "0";
				return str.Substring(idx);
			}).ToList());
		}

		void Execute_Numeric_Fraction_Simplify() => ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => Text.GetString(range)).Select(SimplifyFraction).ToList());

		void Execute_Numeric_Absolute() => ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => Text.GetString(range).TrimStart('-')).ToList());

		Configuration_Numeric_Floor Configure_Numeric_Floor() => Tabs.TabsWindow.Configure_Numeric_Floor("Floor", GetVariables());

		void Execute_Numeric_Floor()
		{
			var result = state.Configuration as Configuration_Numeric_Floor;
			var baseValue = state.GetExpression(result.BaseValue).EvaluateList<double>(GetVariables(), Selections.Count());
			var interval = state.GetExpression(result.Interval).EvaluateList<double>(GetVariables(), Selections.Count());
			ReplaceSelections(Selections.AsParallel().AsOrdered().Select((range, index) => (Math.Floor((double.Parse(Text.GetString(range), NumberStyles.Float) - baseValue[index]) / interval[index]) * interval[index] + baseValue[index]).ToString()).ToList());
		}

		Configuration_Numeric_Floor Configure_Numeric_Ceiling() => Tabs.TabsWindow.Configure_Numeric_Floor("Ceiling", GetVariables());

		void Execute_Numeric_Ceiling()
		{
			var result = state.Configuration as Configuration_Numeric_Floor;
			var baseValue = state.GetExpression(result.BaseValue).EvaluateList<double>(GetVariables(), Selections.Count());
			var interval = state.GetExpression(result.Interval).EvaluateList<double>(GetVariables(), Selections.Count());
			ReplaceSelections(Selections.AsParallel().AsOrdered().Select((range, index) => (Math.Ceiling((double.Parse(Text.GetString(range), NumberStyles.Float) - baseValue[index]) / interval[index]) * interval[index] + baseValue[index]).ToString()).ToList());
		}

		Configuration_Numeric_Floor Configure_Numeric_Round() => Tabs.TabsWindow.Configure_Numeric_Floor("Round", GetVariables());

		void Execute_Numeric_Round()
		{
			var result = state.Configuration as Configuration_Numeric_Floor;
			var baseValue = state.GetExpression(result.BaseValue).EvaluateList<double>(GetVariables(), Selections.Count());
			var interval = state.GetExpression(result.Interval).EvaluateList<double>(GetVariables(), Selections.Count());
			ReplaceSelections(Selections.AsParallel().AsOrdered().Select((range, index) => (Math.Round((double.Parse(Text.GetString(range), NumberStyles.Float) - baseValue[index]) / interval[index], MidpointRounding.AwayFromZero) * interval[index] + baseValue[index]).ToString()).ToList());
		}

		Configuration_Numeric_Limit Configure_Numeric_Limit() => Tabs.TabsWindow.Configure_Numeric_Limit(GetVariables());

		void Execute_Numeric_Limit()
		{
			var result = state.Configuration as Configuration_Numeric_Limit;
			var variables = GetVariables();
			var minimums = state.GetExpression(result.Minimum).EvaluateList<double>(variables, Selections.Count());
			var maximums = state.GetExpression(result.Maximum).EvaluateList<double>(variables, Selections.Count());

			ReplaceSelections(Selections.AsParallel().AsOrdered().Select((range, index) => Limit(minimums[index], double.Parse(Text.GetString(range)), maximums[index]).ToString()).ToList());
		}

		Configuration_Numeric_Cycle Configure_Numeric_Cycle() => Tabs.TabsWindow.Configure_Numeric_Cycle(GetVariables());

		void Execute_Numeric_Cycle()
		{
			var result = state.Configuration as Configuration_Numeric_Cycle;
			var variables = GetVariables();
			var minimums = state.GetExpression(result.Minimum).EvaluateList<double>(variables, Selections.Count());
			var maximums = state.GetExpression(result.Maximum).EvaluateList<double>(variables, Selections.Count());
			ReplaceSelections(Selections.AsParallel().AsOrdered().Select((range, index) => Cycle(double.Parse(Text.GetString(range)), minimums[index], maximums[index], result.IncludeBeginning).ToString()).ToList());
		}

		void Execute_Numeric_Trim() => ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => TrimNumeric(Text.GetString(range))).ToList());

		void Execute_Numeric_Factor() => ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => Factor(BigInteger.Parse(Text.GetString(range)))).ToList());

		Configuration_Numeric_RandomNumber Configure_Numeric_RandomNumber() => Tabs.TabsWindow.Configure_Numeric_RandomNumber(GetVariables());

		void Execute_Numeric_RandomNumber()
		{
			var result = state.Configuration as Configuration_Numeric_RandomNumber;
			var variables = GetVariables();
			var minValues = state.GetExpression(result.MinValue).EvaluateList<int>(variables, Selections.Count());
			var maxValues = state.GetExpression(result.MaxValue).EvaluateList<int>(variables, Selections.Count());
			ReplaceSelections(Selections.AsParallel().Select((range, index) => random.Next(minValues[index], maxValues[index] + 1).ToString()).ToList());
		}

		Configuration_Numeric_CombinationsPermutations Configure_Numeric_CombinationsPermutations()
		{
			if (Selections.Count != 1)
				throw new Exception("Must have one selection.");

			return Tabs.TabsWindow.Configure_Numeric_CombinationsPermutations();
		}

		void Execute_Numeric_CombinationsPermutations()
		{
			var result = state.Configuration as Configuration_Numeric_CombinationsPermutations;
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
					if (result.Type == Configuration_Numeric_CombinationsPermutations.CombinationsPermutationsType.Combinations)
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

		Configuration_Numeric_MinMaxValues Configure_Numeric_MinMaxValues() => Tabs.TabsWindow.Configure_Numeric_MinMaxValues();

		void Execute_Numeric_MinMaxValues()
		{
			var result = state.Configuration as Configuration_Numeric_MinMaxValues;
			ReplaceSelections(string.Join(" ", new List<string> { result.Min ? result.CodePage.MinValue() : null, result.Max ? result.CodePage.MaxValue() : null }.Where(str => !string.IsNullOrEmpty(str))));
		}
	}
}
