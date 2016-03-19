using System.Windows;
using NeoEdit.Common.Expressions;
using NeoEdit.GUI.Controls;
using NeoEdit.GUI.Dialogs;

namespace NeoEdit.TextEdit.Dialogs
{
	internal partial class RepeatDialog
	{
		internal class Result
		{
			public string Expression { get; set; }
			public bool SelectRepetitions { get; set; }
		}

		[DepProp]
		public string Expression { get { return UIHelper<RepeatDialog>.GetPropValue<string>(this); } set { UIHelper<RepeatDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool SelectRepetitions { get { return UIHelper<RepeatDialog>.GetPropValue<bool>(this); } set { UIHelper<RepeatDialog>.SetPropValue(this, value); } }
		public NEVariables Variables { get; }

		static RepeatDialog() { UIHelper<RepeatDialog>.Register(); }

		RepeatDialog(bool selectRepetitions, NEVariables variables)
		{
			Variables = variables;
			InitializeComponent();

			Expression = "1";
			SelectRepetitions = selectRepetitions;
		}

		private void ExpressionHelp(object sender, RoutedEventArgs e) => ExpressionHelpDialog.Display(Variables);

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result { Expression = Expression, SelectRepetitions = SelectRepetitions };
			expression.AddCurrentSuggestion();
			DialogResult = true;
		}

		static public Result Run(Window parent, bool selectRepetitions, NEVariables variables)
		{
			var dialog = new RepeatDialog(selectRepetitions, variables) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
