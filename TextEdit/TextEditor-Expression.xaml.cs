using System;
using System.Data;
using System.Linq;
using NeoEdit.Common.Expressions;
using NeoEdit.GUI.Controls;
using NeoEdit.GUI.Dialogs;
using NeoEdit.TextEdit.Dialogs;

namespace NeoEdit.TextEdit
{
	partial class TextEditor
	{
		GetExpressionDialog.Result Command_Expression_Expression_Dialog() => GetExpressionDialog.Run(WindowParent, GetVariables(), Selections.Count);

		void Command_Expression_Expression(GetExpressionDialog.Result result) => ReplaceSelections(GetFixedExpressionResults<string>(result.Expression));

		GetExpressionDialog.Result Command_Expression_Copy_Dialog() => GetExpressionDialog.Run(WindowParent, GetVariables());

		void Command_Expression_Copy(GetExpressionDialog.Result result) => SetClipboardStrings(GetVariableExpressionResults<string>(result.Expression));

		void Command_Expression_EvaluateSelected() => ReplaceSelections(GetFixedExpressionResults<string>("Eval(x)"));

		GetExpressionDialog.Result Command_Expression_SelectByExpression_Dialog() => GetExpressionDialog.Run(WindowParent, GetVariables(), Selections.Count);

		void Command_Expression_SelectByExpression(GetExpressionDialog.Result result)
		{
			var results = GetFixedExpressionResults<bool>(result.Expression);
			SetSelections(Selections.Where((str, num) => results[num]).ToList());
		}

		ExpressionSolveDialog.Result Command_Expression_Solve_Dialog() => ExpressionSolveDialog.Run(WindowParent, GetVariables());

		void Command_Expression_Solve(ExpressionSolveDialog.Result result, AnswerResult answer)
		{
			var variables = GetVariables();
			var expression = new NEExpression(result.Expression);
			var targets = new NEExpression(result.Target).EvaluateList<double>(variables, Selections.Count);
			var values = Selections.AsParallel().AsOrdered().Select(range => range.HasSelection ? double.Parse(GetString(range)) : 0).ToList();

			var maxLoops = 10000;
			var level = Enumerable.Repeat(1e14, Selections.Count).ToList();
			var done = Enumerable.Repeat(false, Selections.Count).ToList();
			var doneCount = Selections.Count;
			while ((maxLoops-- >= 0) && (doneCount != 0))
			{
				variables.Add(NEVariable.List("v", "", () => values));
				var current = expression.EvaluateList<double>(variables, Selections.Count).Select((value, index) => Math.Abs(value - targets[index])).ToList();

				for (var ctr = 0; ctr < done.Count; ++ctr)
					if ((!done[ctr]) && (current[ctr] <= result.Tolerance))
					{
						--doneCount;
						done[ctr] = true;
					}

				variables.Add(NEVariable.List("v", "", () => values.Select((value, index) => value - level[index])));
				var prev = expression.EvaluateList<double>(variables, Selections.Count).Select((value, index) => Math.Abs(value - targets[index])).ToList();

				variables.Add(NEVariable.List("v", "", () => values.Select((value, index) => value + level[index])));
				var next = expression.EvaluateList<double>(variables, Selections.Count).Select((value, index) => Math.Abs(value - targets[index])).ToList();

				for (var ctr = 0; ctr < done.Count; ++ctr)
					if (!done[ctr])
					{
						if ((current[ctr] <= prev[ctr]) && (current[ctr] <= next[ctr]))
							level[ctr] /= 10;
						else if (prev[ctr] <= next[ctr])
							values[ctr] -= level[ctr];
						else
							values[ctr] += level[ctr];
					}
			}

			if (doneCount != 0)
			{
				if (new Message(WindowParent)
				{
					Title = "Confirm",
					Text = "Unable to find value. Use best match?",
					Options = Message.OptionsEnum.YesNo,
					DefaultCancel = Message.OptionsEnum.No,
				}.Show() != Message.OptionsEnum.Yes)
					throw new Exception("Unable to find value");
			}

			ReplaceSelections(values.Select(value => value.ToString()).ToList());
		}

		void Command_Expression_ClearVariables() => variables.Clear();

		ExpressionSetVariablesDialog.Result Command_Expression_SetVariables_Dialog() => ExpressionSetVariablesDialog.Run(WindowParent, Selections.Select(range => GetString(range)).FirstOrDefault() ?? "");

		void Command_Expression_SetVariables(ExpressionSetVariablesDialog.Result result) => variables[result.VarName] = GetSelectionStrings();
	}
}
