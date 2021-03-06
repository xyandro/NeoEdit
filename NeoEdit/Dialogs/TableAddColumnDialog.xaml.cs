﻿using System;
using System.Windows;
using NeoEdit.Program.Controls;
using NeoEdit.Program.Expressions;

namespace NeoEdit.Program.Dialogs
{
	partial class TableAddColumnDialog
	{
		public class Result
		{
			public string ColumnName { get; set; }
			public string Expression { get; set; }
		}

		[DepProp]
		public string ColumnName { get { return UIHelper<TableAddColumnDialog>.GetPropValue<string>(this); } set { UIHelper<TableAddColumnDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string Expression { get { return UIHelper<TableAddColumnDialog>.GetPropValue<string>(this); } set { UIHelper<TableAddColumnDialog>.SetPropValue(this, value); } }
		[DepProp]
		public NEVariables Variables { get { return UIHelper<TableAddColumnDialog>.GetPropValue<NEVariables>(this); } set { UIHelper<TableAddColumnDialog>.SetPropValue(this, value); } }
		[DepProp]
		public int NumRows { get { return UIHelper<TableAddColumnDialog>.GetPropValue<int>(this); } set { UIHelper<TableAddColumnDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool IsValid { get { return UIHelper<TableAddColumnDialog>.GetPropValue<bool>(this); } set { UIHelper<TableAddColumnDialog>.SetPropValue(this, value); } }

		static TableAddColumnDialog() { UIHelper<TableAddColumnDialog>.Register(); }

		TableAddColumnDialog(NEVariables variables, int numRows)
		{
			InitializeComponent();

			Expression = "y";
			Variables = variables;
			NumRows = numRows;
		}

		void ExpressionHelp(object sender, RoutedEventArgs e) => ExpressionHelpDialog.Display(Variables);

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

		static public Result Run(Window parent, NEVariables variables, int numRows)
		{
			var dialog = new TableAddColumnDialog(variables, numRows) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
