using System.Windows;
using NeoEdit.Common.Expressions;
using NeoEdit.GUI.Controls;
using NeoEdit.GUI.Dialogs;

namespace NeoEdit.TextEdit.Dialogs
{
	partial class ExpressionSolveDialog
	{
		public class Result
		{
			public string Expression { get; set; }
			public string Target { get; set; }
			public double Tolerance { get; set; }
		}

		[DepProp]
		public string Expression { get { return UIHelper<ExpressionSolveDialog>.GetPropValue<string>(this); } set { UIHelper<ExpressionSolveDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string Target { get { return UIHelper<ExpressionSolveDialog>.GetPropValue<string>(this); } set { UIHelper<ExpressionSolveDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string Tolerance { get { return UIHelper<ExpressionSolveDialog>.GetPropValue<string>(this); } set { UIHelper<ExpressionSolveDialog>.SetPropValue(this, value); } }

		public NEVariables Variables { get; }
		static ExpressionSolveDialog() { UIHelper<ExpressionSolveDialog>.Register(); }

		ExpressionSolveDialog(NEVariables variables)
		{
			Variables = variables;
			InitializeComponent();

			Expression = "";
			Target = "0";
			Tolerance = "0.000001";
		}

		void ExpressionHelp(object sender, RoutedEventArgs e) => ExpressionHelpDialog.Display(Variables);

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if (!new NEExpression(Expression).Variables.Contains("v"))
			{
				Message.Show("Must include 'v' in expression to solve.", "Error", Owner);
				return;
			}

			expression.AddCurrentSuggestion();
			target.AddCurrentSuggestion();
			tolerance.AddCurrentSuggestion();
			result = new Result { Expression = Expression, Target = Target, Tolerance = double.Parse(Tolerance) };
			DialogResult = true;
		}

		static public Result Run(Window parent, NEVariables variables)
		{
			var dialog = new ExpressionSolveDialog(variables) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
