﻿using System;
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
	partial class NEFileHandler
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

			var values = Selections.AsTaskRunner().Select(range => new NumericValue(Text.GetString(range))).ToList();
			var find = max ? values.OrderByDescending().First() : values.OrderBy().First();
			Selections = values.Indexes(value => value == find).Select(index => Selections[index]).ToList();
		}

		static Configuration_Numeric_Select_Limit Configure_Numeric_Select_Limit() => EditorExecuteState.CurrentState.NEFiles.FilesWindow.RunDialog_Configure_Numeric_Select_Limit(EditorExecuteState.CurrentState.NEFiles.Focused.GetVariables());

		void Execute_Numeric_Select_Limit()
		{
			var result = EditorExecuteState.CurrentState.Configuration as Configuration_Numeric_Select_Limit;
			var variables = GetVariables();
			var minimums = EditorExecuteState.CurrentState.GetExpression(result.Minimum).EvaluateList<double>(variables, Selections.Count());
			var maximums = EditorExecuteState.CurrentState.GetExpression(result.Maximum).EvaluateList<double>(variables, Selections.Count());

			ReplaceSelections(Selections.AsTaskRunner().Select((range, index) => Limit(minimums[index], double.Parse(Text.GetString(range)), maximums[index]).ToString()).ToList());
		}

		static Configuration_Numeric_Various Configure_Numeric_Round() => EditorExecuteState.CurrentState.NEFiles.FilesWindow.RunDialog_Configure_Numeric_Various("Round", EditorExecuteState.CurrentState.NEFiles.Focused.GetVariables());

		void Execute_Numeric_Round()
		{
			var result = EditorExecuteState.CurrentState.Configuration as Configuration_Numeric_Various;
			var baseValue = EditorExecuteState.CurrentState.GetExpression(result.BaseValue).EvaluateList<double>(GetVariables(), Selections.Count());
			var interval = EditorExecuteState.CurrentState.GetExpression(result.Interval).EvaluateList<double>(GetVariables(), Selections.Count());
			ReplaceSelections(Selections.AsTaskRunner().Select((range, index) => (Math.Round((double.Parse(Text.GetString(range), NumberStyles.Float) - baseValue[index]) / interval[index], MidpointRounding.AwayFromZero) * interval[index] + baseValue[index]).ToString()).ToList());
		}

		static Configuration_Numeric_Various Configure_Numeric_Floor() => EditorExecuteState.CurrentState.NEFiles.FilesWindow.RunDialog_Configure_Numeric_Various("Floor", EditorExecuteState.CurrentState.NEFiles.Focused.GetVariables());

		void Execute_Numeric_Floor()
		{
			var result = EditorExecuteState.CurrentState.Configuration as Configuration_Numeric_Various;
			var baseValue = EditorExecuteState.CurrentState.GetExpression(result.BaseValue).EvaluateList<double>(GetVariables(), Selections.Count());
			var interval = EditorExecuteState.CurrentState.GetExpression(result.Interval).EvaluateList<double>(GetVariables(), Selections.Count());
			ReplaceSelections(Selections.AsTaskRunner().Select((range, index) => (Math.Floor((double.Parse(Text.GetString(range), NumberStyles.Float) - baseValue[index]) / interval[index]) * interval[index] + baseValue[index]).ToString()).ToList());
		}

		static Configuration_Numeric_Various Configure_Numeric_Ceiling() => EditorExecuteState.CurrentState.NEFiles.FilesWindow.RunDialog_Configure_Numeric_Various("Ceiling", EditorExecuteState.CurrentState.NEFiles.Focused.GetVariables());

		void Execute_Numeric_Ceiling()
		{
			var result = EditorExecuteState.CurrentState.Configuration as Configuration_Numeric_Various;
			var baseValue = EditorExecuteState.CurrentState.GetExpression(result.BaseValue).EvaluateList<double>(GetVariables(), Selections.Count());
			var interval = EditorExecuteState.CurrentState.GetExpression(result.Interval).EvaluateList<double>(GetVariables(), Selections.Count());
			ReplaceSelections(Selections.AsTaskRunner().Select((range, index) => (Math.Ceiling((double.Parse(Text.GetString(range), NumberStyles.Float) - baseValue[index]) / interval[index]) * interval[index] + baseValue[index]).ToString()).ToList());
		}

		void Execute_Numeric_Sum_Sum()
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

		void Execute_Numeric_Sum_IncrementDecrement(bool add)
		{
			var toAdd = new NumericValue(add ? 1 : -1);
			ReplaceSelections(Selections.AsTaskRunner().Select(range => (new NumericValue(Text.GetString(range)) + toAdd).ToString()).ToList());
		}

		void Execute_Numeric_Sum_AddSubtractClipboard(bool add)
		{
			if (Selections.Count == 0)
				return;

			var clipboardStrings = Clipboard;
			if ((clipboardStrings.Count == 1) && (Selections.Count != 1))
				clipboardStrings = Selections.Select(str => clipboardStrings[0]).ToList();

			if (Selections.Count != clipboardStrings.Count())
				throw new Exception("Must have either one or equal number of clipboards.");

			var mult = new NumericValue(add ? 1 : -1);
			ReplaceSelections(Selections.Zip(clipboardStrings, (sel, clip) => new { sel, clip }).AsTaskRunner().Select(obj => (new NumericValue(Text.GetString(obj.sel)) + new NumericValue(obj.clip) * mult).ToString()).ToList());
		}

		void Execute_Numeric_Sum_ForwardReverseSumWithUndo(bool forward, bool undo)
		{
			var numbers = Selections.AsTaskRunner().Select(range => new NumericValue(Text.GetString(range))).ToList().ToList();
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

		void Execute_Numeric_AbsoluteValue() => ReplaceSelections(Selections.AsTaskRunner().Select(range => Text.GetString(range).TrimStart('-')).ToList());

		static Configuration_Numeric_Scale Configure_Numeric_Scale() => EditorExecuteState.CurrentState.NEFiles.FilesWindow.RunDialog_Configure_Numeric_Scale(EditorExecuteState.CurrentState.NEFiles.Focused.GetVariables());

		void Execute_Numeric_Scale()
		{
			var result = EditorExecuteState.CurrentState.Configuration as Configuration_Numeric_Scale;
			var variables = GetVariables();
			var prevMins = EditorExecuteState.CurrentState.GetExpression(result.PrevMin).EvaluateList<double>(variables, Selections.Count());
			var prevMaxs = EditorExecuteState.CurrentState.GetExpression(result.PrevMax).EvaluateList<double>(variables, Selections.Count());
			var newMins = EditorExecuteState.CurrentState.GetExpression(result.NewMin).EvaluateList<double>(variables, Selections.Count());
			var newMaxs = EditorExecuteState.CurrentState.GetExpression(result.NewMax).EvaluateList<double>(variables, Selections.Count());

			ReplaceSelections(Selections.AsTaskRunner().Select((range, index) => ((double.Parse(Text.GetString(range)) - prevMins[index]) * (newMaxs[index] - newMins[index]) / (prevMaxs[index] - prevMins[index]) + newMins[index]).ToString()).ToList());
		}

		static Configuration_Numeric_Cycle Configure_Numeric_Cycle() => EditorExecuteState.CurrentState.NEFiles.FilesWindow.RunDialog_Configure_Numeric_Cycle(EditorExecuteState.CurrentState.NEFiles.Focused.GetVariables());

		void Execute_Numeric_Cycle()
		{
			var result = EditorExecuteState.CurrentState.Configuration as Configuration_Numeric_Cycle;
			var variables = GetVariables();
			var minimums = EditorExecuteState.CurrentState.GetExpression(result.Minimum).EvaluateList<double>(variables, Selections.Count());
			var maximums = EditorExecuteState.CurrentState.GetExpression(result.Maximum).EvaluateList<double>(variables, Selections.Count());
			ReplaceSelections(Selections.AsTaskRunner().Select((range, index) => Cycle(double.Parse(Text.GetString(range)), minimums[index], maximums[index], result.IncludeBeginning).ToString()).ToList());
		}

		void Execute_Numeric_Trim() => ReplaceSelections(Selections.AsTaskRunner().Select(range => TrimNumeric(Text.GetString(range))).ToList());

		void Execute_Numeric_Fraction() => ReplaceSelections(Selections.AsTaskRunner().Select(range => Text.GetString(range)).Select(SimplifyFraction).ToList());

		void Execute_Numeric_Factor() => ReplaceSelections(Selections.AsTaskRunner().Select(range => Factor(BigInteger.Parse(Text.GetString(range)))).ToList());

		void Execute_Numeric_Series_ZeroBased() => ReplaceSelections(Selections.Select((range, index) => index.ToString()).ToList());

		void Execute_Numeric_Series_OneBased() => ReplaceSelections(Selections.Select((range, index) => (index + 1).ToString()).ToList());

		static Configuration_Numeric_Series_LinearGeometric Configure_Numeric_Series_LinearGeometric(bool linear) => EditorExecuteState.CurrentState.NEFiles.FilesWindow.RunDialog_Configure_Numeric_Series_LinearGeometric(linear, EditorExecuteState.CurrentState.NEFiles.Focused.GetVariables());

		void Execute_Numeric_Series_LinearGeometric(bool linear)
		{
			var result = EditorExecuteState.CurrentState.Configuration as Configuration_Numeric_Series_LinearGeometric;
			var variables = GetVariables();
			var start = EditorExecuteState.CurrentState.GetExpression(result.StartExpression).Evaluate<double>(variables);
			var increment = EditorExecuteState.CurrentState.GetExpression(result.IncrementExpression).Evaluate<double>(variables);
			ReplaceSelections(Selections.Select((range, index) => (linear ? start + increment * index : start * Math.Pow(increment, index)).ToString()).ToList());
		}

		void Execute_Numeric_ConvertBase_ToHex() => ReplaceSelections(Selections.AsTaskRunner().Select(range => BigInteger.Parse(Text.GetString(range)).ToString("x").TrimStart('0')).Select(str => str.Length == 0 ? "0" : str).ToList());

		void Execute_Numeric_ConvertBase_FromHex() => ReplaceSelections(Selections.AsTaskRunner().Select(range => BigInteger.Parse("0" + Text.GetString(range), NumberStyles.HexNumber).ToString()).ToList());

		static Configuration_Numeric_ConvertBase_ConvertBase Configure_Numeric_ConvertBase_ConvertBase() => EditorExecuteState.CurrentState.NEFiles.FilesWindow.RunDialog_Configure_Numeric_ConvertBase_ConvertBase();

		void Execute_Numeric_ConvertBase_ConvertBase()
		{
			var result = EditorExecuteState.CurrentState.Configuration as Configuration_Numeric_ConvertBase_ConvertBase;
			ReplaceSelections(GetSelectionStrings().Select(str => ConvertBase(str, result.InputSet, result.OutputSet)).ToList());
		}

		static Configuration_Numeric_RandomNumber Configure_Numeric_RandomNumber() => EditorExecuteState.CurrentState.NEFiles.FilesWindow.RunDialog_Configure_Numeric_RandomNumber(EditorExecuteState.CurrentState.NEFiles.Focused.GetVariables());

		void Execute_Numeric_RandomNumber()
		{
			var result = EditorExecuteState.CurrentState.Configuration as Configuration_Numeric_RandomNumber;
			var variables = GetVariables();
			var minValues = EditorExecuteState.CurrentState.GetExpression(result.MinValue).EvaluateList<int>(variables, Selections.Count());
			var maxValues = EditorExecuteState.CurrentState.GetExpression(result.MaxValue).EvaluateList<int>(variables, Selections.Count());
			ReplaceSelections(Selections.AsTaskRunner().Select((range, index) => random.Next(minValues[index], maxValues[index] + 1).ToString()).ToList());
		}

		static Configuration_Numeric_CombinationsPermutations Configure_Numeric_CombinationsPermutations()
		{
			if (EditorExecuteState.CurrentState.NEFiles.Focused.Selections.Count != 1)
				throw new Exception("Must have one selection.");

			return EditorExecuteState.CurrentState.NEFiles.FilesWindow.RunDialog_Configure_Numeric_CombinationsPermutations();
		}

		void Execute_Numeric_CombinationsPermutations()
		{
			var result = EditorExecuteState.CurrentState.Configuration as Configuration_Numeric_CombinationsPermutations;
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

			ReplaceSelections(string.Join("", output.Select(row => string.Join(" ", row) + Text.DefaultEnding)));

			var start = Selections.Single().Start;
			var sels = new List<Range>();
			foreach (var row in output)
			{
				foreach (var str in row)
				{
					sels.Add(Range.FromIndex(start, str.Length));
					start += str.Length + 1; // +1 is for space
				}
				start += Text.DefaultEnding.Length - 1; // -1 is for space added before
			}
			Selections = sels;
		}

		static Configuration_Numeric_MinMaxValues Configure_Numeric_MinMaxValues() => EditorExecuteState.CurrentState.NEFiles.FilesWindow.RunDialog_Configure_Numeric_MinMaxValues();

		void Execute_Numeric_MinMaxValues()
		{
			var result = EditorExecuteState.CurrentState.Configuration as Configuration_Numeric_MinMaxValues;
			ReplaceSelections(string.Join(" ", new List<string> { result.Min ? result.CodePage.MinValue() : null, result.Max ? result.CodePage.MaxValue() : null }.Where(str => !string.IsNullOrEmpty(str))));
		}
	}
}
