using System.Windows;
using NeoEdit.Common.Expressions;
using NeoEdit.TextEdit.Controls;
using NeoEdit.TextEdit.Dialogs;

namespace NeoEdit.TextEdit.Dialogs
{
	partial class ExpressionSolveDialog
	{
		public class Result
		{
			public string SetVariable { get; set; }
			public string TargetExpression { get; set; }
			public string ToleranceExpression { get; set; }
			public string ChangeVariable { get; set; }
			public string StartValueExpression { get; set; }
		}

		[DepProp]
		public string SetVariable { get { return UIHelper<ExpressionSolveDialog>.GetPropValue<string>(this); } set { UIHelper<ExpressionSolveDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string TargetExpression { get { return UIHelper<ExpressionSolveDialog>.GetPropValue<string>(this); } set { UIHelper<ExpressionSolveDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string ToleranceExpression { get { return UIHelper<ExpressionSolveDialog>.GetPropValue<string>(this); } set { UIHelper<ExpressionSolveDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string ChangeVariable { get { return UIHelper<ExpressionSolveDialog>.GetPropValue<string>(this); } set { UIHelper<ExpressionSolveDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string StartValueExpression { get { return UIHelper<ExpressionSolveDialog>.GetPropValue<string>(this); } set { UIHelper<ExpressionSolveDialog>.SetPropValue(this, value); } }

		public NEVariables Variables { get; }
		static ExpressionSolveDialog() { UIHelper<ExpressionSolveDialog>.Register(); }

		ExpressionSolveDialog(NEVariables variables)
		{
			Variables = variables;
			InitializeComponent();

			SetVariable = ChangeVariable = "";
			TargetExpression = StartValueExpression = "0";
			ToleranceExpression = "0.000001";
		}

		void ExpressionHelp(object sender, RoutedEventArgs e) => ExpressionHelpDialog.Display(Variables);

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if ((string.IsNullOrWhiteSpace(SetVariable)) || (string.IsNullOrWhiteSpace(ChangeVariable)))
				return;

			setVariable.AddCurrentSuggestion();
			targetExpression.AddCurrentSuggestion();
			toleranceExpression.AddCurrentSuggestion();
			changeVariable.AddCurrentSuggestion();
			startValueExpression.AddCurrentSuggestion();
			result = new Result { SetVariable = SetVariable, TargetExpression = TargetExpression, ToleranceExpression = ToleranceExpression, ChangeVariable = ChangeVariable, StartValueExpression = StartValueExpression };
			DialogResult = true;
		}

		static public Result Run(Window parent, NEVariables variables)
		{
			var dialog = new ExpressionSolveDialog(variables) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
