using System;
using System.Windows;
using NeoEdit.Common.Expressions;
using NeoEdit.Common.Models;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class EditRotateDialog
	{
		[DepProp]
		public string Count { get { return UIHelper<EditRotateDialog>.GetPropValue<string>(this); } set { UIHelper<EditRotateDialog>.SetPropValue(this, value); } }

		public NEVariables Variables { get; }

		static EditRotateDialog() { UIHelper<EditRotateDialog>.Register(); }

		EditRotateDialog(NEVariables variables)
		{
			Variables = variables;
			InitializeComponent();
			Count = "1";
		}

		EditRotateDialogResult result = null;
		void OkClick(object sender, RoutedEventArgs e)
		{
			count.AddCurrentSuggestion();
			result = new EditRotateDialogResult { Count = Count };
			DialogResult = true;
		}

		public static EditRotateDialogResult Run(Window parent, NEVariables variables)
		{
			var dialog = new EditRotateDialog(variables) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();

			return dialog.result;
		}
	}
}
