using System;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Expressions;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Configure_Edit_Repeat_Dialog
	{
		[DepProp]
		public string Expression { get { return UIHelper<Configure_Edit_Repeat_Dialog>.GetPropValue<string>(this); } set { UIHelper<Configure_Edit_Repeat_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool SelectRepetitions { get { return UIHelper<Configure_Edit_Repeat_Dialog>.GetPropValue<bool>(this); } set { UIHelper<Configure_Edit_Repeat_Dialog>.SetPropValue(this, value); } }
		public NEVariables Variables { get; }

		static Configure_Edit_Repeat_Dialog() { UIHelper<Configure_Edit_Repeat_Dialog>.Register(); }

		Configure_Edit_Repeat_Dialog(bool selectRepetitions, NEVariables variables)
		{
			Variables = variables;
			InitializeComponent();

			Expression = "1";
			SelectRepetitions = selectRepetitions;
		}

		private void ExpressionHelp(object sender, RoutedEventArgs e) => ExpressionHelpDialog.Display(Variables);

		Configuration_Edit_Repeat result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Configuration_Edit_Repeat { Expression = Expression, SelectRepetitions = SelectRepetitions };
			expression.AddCurrentSuggestion();
			DialogResult = true;
		}

		public static Configuration_Edit_Repeat Run(Window parent, bool selectRepetitions, NEVariables variables)
		{
			var dialog = new Configure_Edit_Repeat_Dialog(selectRepetitions, variables) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
