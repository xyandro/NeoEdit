using System.Data;
using System.Linq;
using NeoEdit.Program.Dialogs;

namespace NeoEdit.Program
{
	partial class TextEditor
	{
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
	}
}
