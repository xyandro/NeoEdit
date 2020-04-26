using System;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Expressions;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Configure_Table_AddColumn_Dialog
	{
		[DepProp]
		public string ColumnName { get { return UIHelper<Configure_Table_AddColumn_Dialog>.GetPropValue<string>(this); } set { UIHelper<Configure_Table_AddColumn_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public string Expression { get { return UIHelper<Configure_Table_AddColumn_Dialog>.GetPropValue<string>(this); } set { UIHelper<Configure_Table_AddColumn_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public NEVariables Variables { get { return UIHelper<Configure_Table_AddColumn_Dialog>.GetPropValue<NEVariables>(this); } set { UIHelper<Configure_Table_AddColumn_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public int NumRows { get { return UIHelper<Configure_Table_AddColumn_Dialog>.GetPropValue<int>(this); } set { UIHelper<Configure_Table_AddColumn_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool IsValid { get { return UIHelper<Configure_Table_AddColumn_Dialog>.GetPropValue<bool>(this); } set { UIHelper<Configure_Table_AddColumn_Dialog>.SetPropValue(this, value); } }

		static Configure_Table_AddColumn_Dialog() { UIHelper<Configure_Table_AddColumn_Dialog>.Register(); }

		Configure_Table_AddColumn_Dialog(NEVariables variables, int numRows)
		{
			InitializeComponent();

			Expression = "y";
			Variables = variables;
			NumRows = numRows;
		}

		void ExpressionHelp(object sender, RoutedEventArgs e) => ExpressionHelpDialog.Display(Variables);

		Configuration_Table_AddColumn result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if (!IsValid)
				throw new Exception("Invalid expression");

			columnName.AddCurrentSuggestion();
			expression.AddCurrentSuggestion();
			result = new Configuration_Table_AddColumn { ColumnName = ColumnName, Expression = Expression };
			DialogResult = true;
		}

		public static Configuration_Table_AddColumn Run(Window parent, NEVariables variables, int numRows)
		{
			var dialog = new Configure_Table_AddColumn_Dialog(variables, numRows) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
