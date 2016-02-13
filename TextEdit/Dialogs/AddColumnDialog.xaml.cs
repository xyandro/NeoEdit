using System;
using System.Windows;
using NeoEdit.Common.Expressions;
using NeoEdit.GUI.Controls;

namespace NeoEdit.TextEdit.Dialogs
{
	partial class AddColumnDialog
	{
		public class Result
		{
			public string ColumnName { get; set; }
			public string Expression { get; set; }
		}

		[DepProp]
		public string ColumnName { get { return UIHelper<AddColumnDialog>.GetPropValue<string>(this); } set { UIHelper<AddColumnDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string Expression { get { return UIHelper<AddColumnDialog>.GetPropValue<string>(this); } set { UIHelper<AddColumnDialog>.SetPropValue(this, value); } }
		[DepProp]
		public NEVariables Variables { get { return UIHelper<AddColumnDialog>.GetPropValue<NEVariables>(this); } set { UIHelper<AddColumnDialog>.SetPropValue(this, value); } }
		[DepProp]
		public int NumRows { get { return UIHelper<AddColumnDialog>.GetPropValue<int>(this); } set { UIHelper<AddColumnDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool IsValid { get { return UIHelper<AddColumnDialog>.GetPropValue<bool>(this); } set { UIHelper<AddColumnDialog>.SetPropValue(this, value); } }

		static AddColumnDialog() { UIHelper<AddColumnDialog>.Register(); }

		readonly Action helpDialog;
		AddColumnDialog(NEVariables variables, int numRows, Action helpDialog)
		{
			this.helpDialog = helpDialog;

			InitializeComponent();

			Expression = "y";
			Variables = variables;
			NumRows = numRows;
		}

		void ExpressionHelp(object sender, RoutedEventArgs e)
		{
			if (helpDialog != null)
				helpDialog();
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if (!IsValid)
				throw new Exception("Invalid expression");

			columnName.AddCurrentSuggestion();
			expression.AddCurrentSuggestion();
			result = new Result { ColumnName = ColumnName, Expression = Expression };
			DialogResult = true;
		}

		static public Result Run(Window parent, NEVariables variables, int numRows, Action helpDialog = null)
		{
			var dialog = new AddColumnDialog(variables, numRows, helpDialog) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
