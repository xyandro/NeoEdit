using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using NeoEdit.Common;
using NeoEdit.Common.Controls;
using NeoEdit.Common.Dialogs;
using NeoEdit.Common.Expressions;
using NeoEdit.MenuExpression.Dialogs;

namespace NeoEdit.MenuExpression
{
	public static class ExpressionFunctions
	{
		static void CalculateInlineVariables(ITextEditor te, List<InlineVariable> inlineVars)
		{
			var variables = te.GetVariables();
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

		static public GetExpressionDialog.Result Command_Expression_Expression_Dialog(ITextEditor te) => GetExpressionDialog.Run(te.WindowParent, te.GetVariables(), te.Selections.Count);

		static public void Command_Expression_Expression(ITextEditor te, GetExpressionDialog.Result result) => te.ReplaceSelections(te.GetFixedExpressionResults<string>(result.Expression));

		static public GetExpressionDialog.Result Command_Expression_Copy_Dialog(ITextEditor te) => GetExpressionDialog.Run(te.WindowParent, te.GetVariables());

		static public void Command_Expression_Copy(ITextEditor te, GetExpressionDialog.Result result) => te.SetClipboardStrings(te.GetVariableExpressionResults<string>(result.Expression));

		static public void Command_Expression_EvaluateSelected(ITextEditor te) => te.ReplaceSelections(te.GetFixedExpressionResults<string>("Eval(x)"));

		static public GetExpressionDialog.Result Command_Expression_SelectByExpression_Dialog(ITextEditor te) => GetExpressionDialog.Run(te.WindowParent, te.GetVariables(), te.Selections.Count);

		static public void Command_Expression_SelectByExpression(ITextEditor te, GetExpressionDialog.Result result)
		{
			var results = te.GetFixedExpressionResults<bool>(result.Expression);
			te.SetSelections(te.Selections.Where((str, num) => results[num]).ToList());
		}

		static public void Command_Expression_InlineVariables_Add(ITextEditor te) => te.ReplaceSelections(te.GetSelectionStrings().Select(str => $"[:'{(string.IsNullOrEmpty(str) ? "0" : str)}'=0]").ToList());

		static public void Command_Expression_InlineVariables_Calculate(ITextEditor te)
		{
			var inlineVars = te.GetInlineVariables();
			CalculateInlineVariables(te, inlineVars);
			inlineVars.Select(inlineVar => inlineVar.Exception).NonNull().ForEach(ex => throw ex);
			te.Replace(inlineVars.Select(inlineVar => inlineVar.ValueRange).ToList(), inlineVars.Select(inlineVar => inlineVar.Value.ToString()).ToList());
		}

		static public ExpressionSolveDialog.Result Command_Expression_InlineVariables_Solve_Dialog(ITextEditor te) => ExpressionSolveDialog.Run(te.WindowParent, te.GetVariables());

		static public void Command_Expression_InlineVariables_Solve(ITextEditor te, ExpressionSolveDialog.Result result, AnswerResult answer)
		{
			var inlineVars = te.GetInlineVariables();
			var setIndex = inlineVars.FindIndex(inlineVar => inlineVar.Name.Equals(result.SetVariable));
			if (setIndex == -1)
				throw new Exception($"Unknown variable: {result.SetVariable}");
			var changeIndex = inlineVars.FindIndex(inlineVar => inlineVar.Name.Equals(result.ChangeVariable));
			if (changeIndex == -1)
				throw new Exception($"Unknown variable: {result.ChangeVariable}");

			var variables = te.GetVariables();
			var target = new NEExpression(result.TargetExpression).Evaluate<double>(variables, "");
			var tolerance = new NEExpression(result.ToleranceExpression).Evaluate<double>(variables, "");
			var value = new NEExpression(result.StartValueExpression).Evaluate<double>(variables, "");

			Func<double, double> GetValue = input =>
			{
				inlineVars[changeIndex].Expression = input.ToString();
				CalculateInlineVariables(te, inlineVars);
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
				if ((answer.Answer != MessageOptions.YesToAll) && (answer.Answer != MessageOptions.NoToAll))
					answer.Answer = new Message(te.WindowParent)
					{
						Title = "Confirm",
						Text = "Unable to find value. Use best match?",
						Options = MessageOptions.YesNoYesAll,
						DefaultCancel = MessageOptions.No,
					}.Show();
				if ((answer.Answer != MessageOptions.Yes) && (answer.Answer != MessageOptions.YesToAll))
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
			te.SetSelections(sels);
			te.ReplaceSelections(values);
		}

		static public void Command_Expression_InlineVariables_IncludeInExpressions(ITextEditor te, bool? multiStatus) => te.IncludeInlineVariables = multiStatus != true;
	}
}
