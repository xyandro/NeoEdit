using System;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Expressions;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Configure_File_SaveCopy_ByExpression_Dialog
	{
		[DepProp]
		public string Expression { get { return UIHelper<Configure_File_SaveCopy_ByExpression_Dialog>.GetPropValue<string>(this); } set { UIHelper<Configure_File_SaveCopy_ByExpression_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public NEVariables Variables { get { return UIHelper<Configure_File_SaveCopy_ByExpression_Dialog>.GetPropValue<NEVariables>(this); } set { UIHelper<Configure_File_SaveCopy_ByExpression_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public int? NumRows { get { return UIHelper<Configure_File_SaveCopy_ByExpression_Dialog>.GetPropValue<int?>(this); } set { UIHelper<Configure_File_SaveCopy_ByExpression_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool IsValid { get { return UIHelper<Configure_File_SaveCopy_ByExpression_Dialog>.GetPropValue<bool>(this); } set { UIHelper<Configure_File_SaveCopy_ByExpression_Dialog>.SetPropValue(this, value); } }

		static Configure_File_SaveCopy_ByExpression_Dialog() { UIHelper<Configure_File_SaveCopy_ByExpression_Dialog>.Register(); }

		Configure_File_SaveCopy_ByExpression_Dialog(NEVariables variables, int? numRows)
		{
			InitializeComponent();

			Expression = "x";
			Variables = variables;
			NumRows = numRows;
		}

		void ExpressionHelp(object sender, RoutedEventArgs e) => ExpressionHelpDialog.Display(Variables);

		Configuration_File_SaveCopy_ByExpression result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if (!IsValid)
				throw new Exception("Invalid expression");

			expression.AddCurrentSuggestion();
			result = new Configuration_File_SaveCopy_ByExpression { Expression = Expression };
			DialogResult = true;
		}

		public static Configuration_File_SaveCopy_ByExpression Run(Window parent, NEVariables variables, int? numRows = null)
		{
			var dialog = new Configure_File_SaveCopy_ByExpression_Dialog(variables, numRows) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
