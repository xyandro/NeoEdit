using System;
using System.Windows;
using NeoEdit.Program.Controls;
using NeoEdit.Program.Expressions;

namespace NeoEdit.Program.Dialogs
{
	partial class EditExpressionExpressionDialog
	{
		public enum Action
		{
			Evaluate,
			Copy,
		}

		public class Result
		{
			public string Expression { get; set; }
			public Action Action { get; set; }
		}

		[DepProp]
		public string Expression { get { return UIHelper<EditExpressionExpressionDialog>.GetPropValue<string>(this); } set { UIHelper<EditExpressionExpressionDialog>.SetPropValue(this, value); } }
		[DepProp]
		public NEVariables Variables { get { return UIHelper<EditExpressionExpressionDialog>.GetPropValue<NEVariables>(this); } set { UIHelper<EditExpressionExpressionDialog>.SetPropValue(this, value); } }
		[DepProp]
		public int? NumRows { get { return UIHelper<EditExpressionExpressionDialog>.GetPropValue<int?>(this); } set { UIHelper<EditExpressionExpressionDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool IsValid { get { return UIHelper<EditExpressionExpressionDialog>.GetPropValue<bool>(this); } set { UIHelper<EditExpressionExpressionDialog>.SetPropValue(this, value); } }

		static EditExpressionExpressionDialog() { UIHelper<EditExpressionExpressionDialog>.Register(); }

		EditExpressionExpressionDialog(NEVariables variables, int? numRows)
		{
			InitializeComponent();

			Expression = "x";
			Variables = variables;
			NumRows = numRows;
		}

		void ExpressionHelp(object sender, RoutedEventArgs e) => ExpressionHelpDialog.Display(Variables);

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if (!IsValid)
				throw new Exception("Invalid expression");

			expression.AddCurrentSuggestion();
			result = new Result { Expression = Expression, Action = (Action)(e.Source as FrameworkElement).Tag };
			DialogResult = true;
		}

		static public Result Run(Window parent, NEVariables variables, int? numRows = null)
		{
			var dialog = new EditExpressionExpressionDialog(variables, numRows) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
