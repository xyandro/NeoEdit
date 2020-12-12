using System;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Expressions;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Edit_Repeat_Dialog
	{
		[DepProp]
		public string Expression { get { return UIHelper<Edit_Repeat_Dialog>.GetPropValue<string>(this); } set { UIHelper<Edit_Repeat_Dialog>.SetPropValue(this, value); } }
		public NEVariables Variables { get; }

		static Edit_Repeat_Dialog() { UIHelper<Edit_Repeat_Dialog>.Register(); }

		Edit_Repeat_Dialog(NEVariables variables)
		{
			Variables = variables;
			InitializeComponent();

			Expression = "1";
		}

		private void ExpressionHelp(object sender, RoutedEventArgs e) => ExpressionHelpDialog.Display(Variables);

		Configuration_Edit_Repeat result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Configuration_Edit_Repeat { Expression = Expression };
			expression.AddCurrentSuggestion();
			DialogResult = true;
		}

		public static Configuration_Edit_Repeat Run(Window parent, NEVariables variables)
		{
			var dialog = new Edit_Repeat_Dialog(variables) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
