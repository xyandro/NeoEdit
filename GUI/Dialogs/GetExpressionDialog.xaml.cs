using System;
using System.Windows;
using NeoEdit.Common.Expressions;
using NeoEdit.GUI.Controls;

namespace NeoEdit.GUI.Dialogs
{
	partial class GetExpressionDialog
	{
		public class Result
		{
			public string Expression { get; set; }
		}

		[DepProp]
		public string Expression { get { return UIHelper<GetExpressionDialog>.GetPropValue<string>(this); } set { UIHelper<GetExpressionDialog>.SetPropValue(this, value); } }
		[DepProp]
		public NEVariables Variables { get { return UIHelper<GetExpressionDialog>.GetPropValue<NEVariables>(this); } set { UIHelper<GetExpressionDialog>.SetPropValue(this, value); } }
		[DepProp]
		public int? NumRows { get { return UIHelper<GetExpressionDialog>.GetPropValue<int?>(this); } set { UIHelper<GetExpressionDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool IsValid { get { return UIHelper<GetExpressionDialog>.GetPropValue<bool>(this); } set { UIHelper<GetExpressionDialog>.SetPropValue(this, value); } }

		static GetExpressionDialog() { UIHelper<GetExpressionDialog>.Register(); }

		GetExpressionDialog(NEVariables variables, int? numRows)
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
			result = new Result { Expression = Expression };
			DialogResult = true;
		}

		static public Result Run(Window parent, NEVariables variables, int? numRows = null)
		{
			var dialog = new GetExpressionDialog(variables, numRows) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
