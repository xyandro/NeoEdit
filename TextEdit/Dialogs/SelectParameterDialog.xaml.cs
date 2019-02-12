using System.Windows;
using NeoEdit.Common.Expressions;
using NeoEdit.GUI.Controls;
using NeoEdit.GUI.Dialogs;

namespace NeoEdit.TextEdit.Dialogs
{
	partial class SelectParameterDialog
	{
		internal class Result
		{
			public string Expression { get; set; }
		}

		[DepProp]
		public string Expression { get { return UIHelper<SelectParameterDialog>.GetPropValue<string>(this); } set { UIHelper<SelectParameterDialog>.SetPropValue(this, value); } }
		public NEVariables Variables { get; }

		static SelectParameterDialog() { UIHelper<SelectParameterDialog>.Register(); }

		SelectParameterDialog(NEVariables variables)
		{
			Variables = variables;
			InitializeComponent();

			Expression = "1";
		}

		private void ExpressionHelp(object sender, RoutedEventArgs e) => ExpressionHelpDialog.Display(Variables);

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result { Expression = Expression };
			expression.AddCurrentSuggestion();
			DialogResult = true;
		}

		public static Result Run(Window parent, NEVariables variables)
		{
			var dialog = new SelectParameterDialog(variables) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
