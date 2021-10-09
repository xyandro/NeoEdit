using System;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Expressions;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class FileTable_Various_Various_Dialog
	{
		[DepProp]
		public string Expression { get { return UIHelper<FileTable_Various_Various_Dialog>.GetPropValue<string>(this); } set { UIHelper<FileTable_Various_Various_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public NEVariables Variables { get { return UIHelper<FileTable_Various_Various_Dialog>.GetPropValue<NEVariables>(this); } set { UIHelper<FileTable_Various_Various_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public int? NumRows { get { return UIHelper<FileTable_Various_Various_Dialog>.GetPropValue<int?>(this); } set { UIHelper<FileTable_Various_Various_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool IsValid { get { return UIHelper<FileTable_Various_Various_Dialog>.GetPropValue<bool>(this); } set { UIHelper<FileTable_Various_Various_Dialog>.SetPropValue(this, value); } }

		static FileTable_Various_Various_Dialog() { UIHelper<FileTable_Various_Various_Dialog>.Register(); }

		FileTable_Various_Various_Dialog(NEVariables variables, int? numRows)
		{
			InitializeComponent();

			Expression = "x";
			Variables = variables;
			NumRows = numRows;

			PreviewKeyDown += (s, e) => (Expression, _) = ExpressionShortcutsDialog.HandleKey(e, Expression, true);
		}

		void ExpressionHelp(object sender, RoutedEventArgs e) => ExpressionHelpDialog.Display(Variables);

		Configuration_FileTable_Various_Various result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if (!IsValid)
				throw new Exception("Invalid expression");

			expression.AddCurrentSuggestion();
			result = new Configuration_FileTable_Various_Various { Expression = Expression };
			DialogResult = true;
		}

		public static Configuration_FileTable_Various_Various Run(Window parent, NEVariables variables, int? numRows = null)
		{
			var dialog = new FileTable_Various_Various_Dialog(variables, numRows) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
