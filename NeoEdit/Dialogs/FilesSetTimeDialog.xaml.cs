using System;
using System.Windows;
using NeoEdit.Program.Controls;
using NeoEdit.Program.Expressions;
using NeoEdit.Program.Models;

namespace NeoEdit.Program.Dialogs
{
	partial class FilesSetTimeDialog
	{
		[DepProp]
		public NEVariables Variables { get { return UIHelper<FilesSetTimeDialog>.GetPropValue<NEVariables>(this); } set { UIHelper<FilesSetTimeDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string Expression { get { return UIHelper<FilesSetTimeDialog>.GetPropValue<string>(this); } set { UIHelper<FilesSetTimeDialog>.SetPropValue(this, value); } }

		static FilesSetTimeDialog() { UIHelper<FilesSetTimeDialog>.Register(); }

		FilesSetTimeDialog(NEVariables variables, string expression)
		{
			InitializeComponent();

			Variables = variables;
			Expression = expression;
		}

		FilesSetTimeDialogResult result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new FilesSetTimeDialogResult { Expression = Expression };
			DialogResult = true;
		}

		static public FilesSetTimeDialogResult Run(Window parent, NEVariables variables, string expression)
		{
			var dialog = new FilesSetTimeDialog(variables, expression) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
