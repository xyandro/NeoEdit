using System;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Expressions;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Edit_Expression_Expression_Dialog
	{
		[DepProp]
		public string Expression { get { return UIHelper<Edit_Expression_Expression_Dialog>.GetPropValue<string>(this); } set { UIHelper<Edit_Expression_Expression_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public NEVariables Variables { get { return UIHelper<Edit_Expression_Expression_Dialog>.GetPropValue<NEVariables>(this); } set { UIHelper<Edit_Expression_Expression_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public int? RowCount { get { return UIHelper<Edit_Expression_Expression_Dialog>.GetPropValue<int?>(this); } set { UIHelper<Edit_Expression_Expression_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool IsValid { get { return UIHelper<Edit_Expression_Expression_Dialog>.GetPropValue<bool>(this); } set { UIHelper<Edit_Expression_Expression_Dialog>.SetPropValue(this, value); } }

		static Edit_Expression_Expression_Dialog() { UIHelper<Edit_Expression_Expression_Dialog>.Register(); }

		Edit_Expression_Expression_Dialog(NEVariables variables, int? rowCount)
		{
			InitializeComponent();

			Expression = "x";
			Variables = variables;
			RowCount = rowCount;

			PreviewKeyDown += (s, e) => (Expression, _) = ExpressionShortcutsDialog.HandleKey(e, Expression, true);
		}

		void ExpressionHelp(object sender, RoutedEventArgs e) => ExpressionHelpDialog.Display(Variables);

		Configuration_Edit_Expression_Expression result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if (!IsValid)
				throw new Exception("Invalid expression");

			expression.AddCurrentSuggestion();
			result = new Configuration_Edit_Expression_Expression { Expression = Expression, Action = (Configuration_Edit_Expression_Expression.Actions)(e.Source as FrameworkElement).Tag };
			DialogResult = true;
		}

		public static Configuration_Edit_Expression_Expression Run(Window parent, NEVariables variables, int? rowCount = null)
		{
			var dialog = new Edit_Expression_Expression_Dialog(variables, rowCount) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
