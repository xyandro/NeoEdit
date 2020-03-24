using System;
using System.Windows;
using NeoEdit.Common.Models;
using NeoEdit.Program.Controls;

namespace NeoEdit.Program.Dialogs
{
	partial class DiffFixWhitespaceDialog
	{
		[DepProp]
		public int LineStartTabStop { get { return UIHelper<DiffFixWhitespaceDialog>.GetPropValue<int>(this); } set { UIHelper<DiffFixWhitespaceDialog>.SetPropValue(this, value); } }

		static DiffFixWhitespaceDialog() { UIHelper<DiffFixWhitespaceDialog>.Register(); }

		DiffFixWhitespaceDialog()
		{
			InitializeComponent();
			LineStartTabStop = 4;
		}

		DiffFixWhitespaceDialogResult result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new DiffFixWhitespaceDialogResult { LineStartTabStop = LineStartTabStop };
			DialogResult = true;
		}

		public static DiffFixWhitespaceDialogResult Run(Window parent)
		{
			var dialog = new DiffFixWhitespaceDialog() { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
