using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using NeoEdit;
using NeoEdit.Expressions;
using NeoEdit.Controls;
using NeoEdit.Dialogs;

namespace NeoEdit
{
	partial class TextEditor
	{
		[DepProp]
		public bool IncludeInlineVariables { get { return UIHelper<TextEditor>.GetPropValue<bool>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }

		class InlineVariable
		{
			public string Name { get; set; }
			public string Expression { get => NEExpression.ToString(); set => NEExpression = new NEExpression(value); }
			public Range ExpressionRange { get; set; }
			public NEExpression NEExpression { get; set; }
			double value;
			public double Value
			{
				get => value; set
				{
					if (value == this.value)
						return;
					this.value = value;
				}
			}
			public Range ValueRange { get; set; }
			public Exception Exception { get; set; }

			public InlineVariable(string name, string expression, Range expressionRange, string value, Range valueRange)
			{
				Name = name;
				Expression = expression;
				ExpressionRange = expressionRange;
				double.TryParse(value, out this.value);
				ValueRange = valueRange;
			}
		}

		void CalculateInlineVariables(List<InlineVariable> inlineVars)
		{
			var variables = GetVariables();
			var processed = new HashSet<InlineVariable>();
			inlineVars.NonNullOrEmpty(inlineVar => inlineVar.Name).ForEach(inlineVar => variables.Remove(inlineVar.Name));

			while (true)
			{
				var done = true;
				foreach (var inlineVar in inlineVars)
				{
					if (processed.Contains(inlineVar))
						continue;
					if (!inlineVar.NEExpression.Variables.All(name => variables.Contains(name)))
						continue;
					inlineVar.Value = inlineVar.NEExpression.Evaluate<double>(variables, "");
					if (!string.IsNullOrEmpty(inlineVar.Name))
						variables.Add(NEVariable.Constant(inlineVar.Name, "User-defined", inlineVar.Value));
					processed.Add(inlineVar);
					done = false;
				}
				if (done)
					break;
			}

			foreach (var inlineVar in inlineVars)
			{
				if (processed.Contains(inlineVar))
					continue;
				inlineVar.Exception = new Exception($"{inlineVar.Name}: {string.Join(", ", inlineVar.NEExpression.Variables.Where(name => !variables.Contains(name)))} undefined");
			}
		}

		List<InlineVariable> GetInlineVariables(ITextEditor te)
		{
			var inlineVars = new List<InlineVariable>();
			var regex = new Regex(@"\[(\w*):'(.*?)'=(.*?)\]", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.Multiline | RegexOptions.IgnoreCase);
			var found = new HashSet<string>();
			foreach (var tuple in te.Data.RegexMatches(regex, BeginOffset, EndOffset - BeginOffset, false, false, false))
			{
				var match = regex.Match(te.Data.GetString(tuple.Item1, tuple.Item2));
				var valueRange = Range.FromIndex(tuple.Item1 + match.Groups[3].Index, match.Groups[3].Length);
				var inlineVar = new InlineVariable(match.Groups[1].Value, match.Groups[2].Value, Range.FromIndex(tuple.Item1 + match.Groups[2].Index, match.Groups[2].Length), GetString(valueRange), valueRange);
				if (!string.IsNullOrEmpty(inlineVar.Name))
				{
					if (found.Contains(inlineVar.Name))
						throw new Exception($"Duplicate inline variable: {inlineVar.Name}");
					found.Add(inlineVar.Name);
				}
				inlineVars.Add(inlineVar);
			}
			return inlineVars;
		}

		GetExpressionDialog.Result Command_Expression_Expression_Dialog() => GetExpressionDialog.Run(TabsParent, GetVariables(), Selections.Count);

		void Command_Expression_Expression(GetExpressionDialog.Result result) => ReplaceSelections(GetFixedExpressionResults<string>(result.Expression));

		GetExpressionDialog.Result Command_Expression_Copy_Dialog() => GetExpressionDialog.Run(TabsParent, GetVariables());

		void Command_Expression_Copy(GetExpressionDialog.Result result) => SetClipboardStrings(GetVariableExpressionResults<string>(result.Expression));

		void Command_Expression_EvaluateSelected() => ReplaceSelections(GetFixedExpressionResults<string>("Eval(x)"));

		GetExpressionDialog.Result Command_Expression_SelectByExpression_Dialog() => GetExpressionDialog.Run(TabsParent, GetVariables(), Selections.Count);

		void Command_Expression_SelectByExpression(GetExpressionDialog.Result result)
		{
			var results = GetFixedExpressionResults<bool>(result.Expression);
			SetSelections(Selections.Where((str, num) => results[num]).ToList());
		}

		void Command_Expression_InlineVariables_Add() => ReplaceSelections(GetSelectionStrings().Select(str => $"[:'{(string.IsNullOrEmpty(str) ? "0" : str)}'=0]").ToList());

		void Command_Expression_InlineVariables_Calculate(ITextEditor te)
		{
			var inlineVars = GetInlineVariables(te);
			CalculateInlineVariables(inlineVars);
			inlineVars.Select(inlineVar => inlineVar.Exception).NonNull().ForEach(ex => throw ex);
			Replace(inlineVars.Select(inlineVar => inlineVar.ValueRange).ToList(), inlineVars.Select(inlineVar => inlineVar.Value.ToString()).ToList());
		}

		ExpressionSolveDialog.Result Command_Expression_InlineVariables_Solve_Dialog() => ExpressionSolveDialog.Run(TabsParent, GetVariables());

		void Command_Expression_InlineVariables_Solve(ITextEditor te, ExpressionSolveDialog.Result result, AnswerResult answer)
		{
			var inlineVars = GetInlineVariables(te);
			var setIndex = inlineVars.FindIndex(inlineVar => inlineVar.Name.Equals(result.SetVariable));
			if (setIndex == -1)
				throw new Exception($"Unknown variable: {result.SetVariable}");
			var changeIndex = inlineVars.FindIndex(inlineVar => inlineVar.Name.Equals(result.ChangeVariable));
			if (changeIndex == -1)
				throw new Exception($"Unknown variable: {result.ChangeVariable}");

			var variables = GetVariables();
			var target = new NEExpression(result.TargetExpression).Evaluate<double>(variables, "");
			var tolerance = new NEExpression(result.ToleranceExpression).Evaluate<double>(variables, "");
			var value = new NEExpression(result.StartValueExpression).Evaluate<double>(variables, "");

			Func<double, double> GetValue = input =>
			{
				inlineVars[changeIndex].Expression = input.ToString();
				CalculateInlineVariables(inlineVars);
				var exception = inlineVars.Select(inlineVar => inlineVar.Value).OfType<Exception>().FirstOrDefault();
				if (exception != null)
					throw exception;
				var diff = Math.Abs(inlineVars[setIndex].Value - target);
				if ((double.IsNaN(diff)) || (double.IsInfinity(diff)))
					diff = double.MaxValue;
				return diff;
			};

			var current = GetValue(value);
			var level = Math.Pow(10, Math.Floor(Math.Log10(value)));
			var increasing = true;

			double? prev = null, next = null;
			var maxLoops = 100;
			while (maxLoops-- > 0)
			{
				if (increasing)
					level *= 10;
				else if (current <= tolerance)
					break;

				prev = prev ?? GetValue(value - level);
				next = next ?? GetValue(value + level);

				if ((current <= prev) && (current <= next))
				{
					increasing = false;
					level /= 10;
					prev = next = null;
				}
				else if (increasing)
					prev = next = null;
				else if (prev <= next)
				{
					next = current;
					current = prev.Value;
					prev = null;
					value -= level;
				}
				else
				{
					prev = current;
					current = next.Value;
					next = null;
					value += level;
				}
			}

			if (maxLoops == 0)
			{
				if ((answer.Answer != Message.OptionsEnum.YesToAll) && (answer.Answer != Message.OptionsEnum.NoToAll))
					answer.Answer = new Message(TabsParent)
					{
						Title = "Confirm",
						Text = "Unable to find value. Use best match?",
						Options = Message.OptionsEnum.YesNoYesAll,
						DefaultCancel = Message.OptionsEnum.No,
					}.Show();
				if ((answer.Answer != Message.OptionsEnum.Yes) && (answer.Answer != Message.OptionsEnum.YesToAll))
					throw new Exception("Unable to find value");
			}

			GetValue(value);
			var sels = new List<Range>();
			var values = new List<string>();
			for (var ctr = 0; ctr < inlineVars.Count; ++ctr)
			{
				if (ctr == changeIndex)
				{
					sels.Add(inlineVars[ctr].ExpressionRange);
					values.Add(value.ToString());
				}
				sels.Add(inlineVars[ctr].ValueRange);
				values.Add(inlineVars[ctr].Value.ToString());
			}
			SetSelections(sels);
			ReplaceSelections(values);
		}

		void Command_Expression_InlineVariables_IncludeInExpressions(bool? multiStatus) => IncludeInlineVariables = multiStatus != true;
	}
}
