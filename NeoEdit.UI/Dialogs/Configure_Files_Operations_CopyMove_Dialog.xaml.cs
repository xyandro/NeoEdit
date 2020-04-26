using System;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Expressions;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Configure_Files_Operations_CopyMove_Dialog
	{
		[DepProp]
		public string OldFileName { get { return UIHelper<Configure_Files_Operations_CopyMove_Dialog>.GetPropValue<string>(this); } set { UIHelper<Configure_Files_Operations_CopyMove_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public string NewFileName { get { return UIHelper<Configure_Files_Operations_CopyMove_Dialog>.GetPropValue<string>(this); } set { UIHelper<Configure_Files_Operations_CopyMove_Dialog>.SetPropValue(this, value); } }

		public NEVariables Variables { get; }

		static Configure_Files_Operations_CopyMove_Dialog() { UIHelper<Configure_Files_Operations_CopyMove_Dialog>.Register(); }

		Configure_Files_Operations_CopyMove_Dialog(NEVariables variables, bool move)
		{
			Variables = variables;
			InitializeComponent();

			Title = move ? "Move Files" : "Copy Files";

			OldFileName = NewFileName = "x";
		}

		private void ExpressionHelp(object sender, RoutedEventArgs e) => ExpressionHelpDialog.Display(Variables);

		Configuration_Files_Operations_CopyMove result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Configuration_Files_Operations_CopyMove { OldFileName = OldFileName, NewFileName = NewFileName };
			oldFileName.AddCurrentSuggestion();
			newFileName.AddCurrentSuggestion();
			DialogResult = true;
		}

		public static Configuration_Files_Operations_CopyMove Run(Window parent, NEVariables variables, bool move)
		{
			var dialog = new Configure_Files_Operations_CopyMove_Dialog(variables, move) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
