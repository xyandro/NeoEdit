using System.Data;
using System.Linq;
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
			Selections.Replace(Selections.Where((str, num) => results[num]).ToList());
		}

		void Command_Expression_ClearVariables() => variables.Clear();

		SetVariablesDialog.Result Command_Expression_SetVariables_Dialog() => SetVariablesDialog.Run(WindowParent, Selections.Select(range => GetString(range)).FirstOrDefault() ?? "");

		void Command_Expression_SetVariables(SetVariablesDialog.Result result) => variables[result.VarName] = GetSelectionStrings();
	}
}
