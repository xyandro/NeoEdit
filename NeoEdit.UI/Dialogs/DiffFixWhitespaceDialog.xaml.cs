using System;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
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

		Configuration_Diff_Fix_Whitespace_Dialog result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Configuration_Diff_Fix_Whitespace_Dialog { LineStartTabStop = LineStartTabStop };
			DialogResult = true;
		}

		public static Configuration_Diff_Fix_Whitespace_Dialog Run(Window parent)
		{
			var dialog = new DiffFixWhitespaceDialog() { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
