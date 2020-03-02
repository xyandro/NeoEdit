using System.Data;
using System.Linq;
using NeoEdit.Program.Dialogs;

namespace NeoEdit.Program
{
	partial class TextEditor
	{
		ExpressionExpressionDialog.Result Command_Expression_Expression_Dialog() => ExpressionExpressionDialog.Run(TabsParent, GetVariables());

		void Command_Expression_Expression(ExpressionExpressionDialog.Result result)
		{
			switch (result.Action)
			{
				case ExpressionExpressionDialog.Action.Evaluate: ReplaceSelections(GetFixedExpressionResults<string>(result.Expression)); break;
				case ExpressionExpressionDialog.Action.Copy: SetClipboardStrings(GetVariableExpressionResults<string>(result.Expression)); break;
				case ExpressionExpressionDialog.Action.Select:
					var results = GetFixedExpressionResults<bool>(result.Expression);
					SetSelections(Selections.Where((str, num) => results[num]).ToList());
					break;
			}
		}

		void Command_Expression_EvaluateSelected() => ReplaceSelections(GetFixedExpressionResults<string>("Eval(x)"));
	}
}
