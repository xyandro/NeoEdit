using System;
using System.Windows;
using NeoEdit.Program.Controls;
using NeoEdit.Program.Expressions;

namespace NeoEdit.Program.Dialogs
{
	partial class ExpressionExpressionDialog
	{
		public enum Action
		{
			Evaluate,
			Copy,
			Select,
		}

		public class Result
		{
			public string Expression { get; set; }
			public Action Action { get; set; }
		}

		[DepProp]
		public string Expression { get { return UIHelper<ExpressionExpressionDialog>.GetPropValue<string>(this); } set { UIHelper<ExpressionExpressionDialog>.SetPropValue(this, value); } }
		[DepProp]
		public NEVariables Variables { get { return UIHelper<ExpressionExpressionDialog>.GetPropValue<NEVariables>(this); } set { UIHelper<ExpressionExpressionDialog>.SetPropValue(this, value); } }
		[DepProp]
		public int? NumRows { get { return UIHelper<ExpressionExpressionDialog>.GetPropValue<int?>(this); } set { UIHelper<ExpressionExpressionDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool IsValid { get { return UIHelper<ExpressionExpressionDialog>.GetPropValue<bool>(this); } set { UIHelper<ExpressionExpressionDialog>.SetPropValue(this, value); } }

		static ExpressionExpressionDialog() { UIHelper<ExpressionExpressionDialog>.Register(); }

		ExpressionExpressionDialog(NEVariables variables, int? numRows)
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
			var dialog = new ExpressionExpressionDialog(variables, numRows) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
