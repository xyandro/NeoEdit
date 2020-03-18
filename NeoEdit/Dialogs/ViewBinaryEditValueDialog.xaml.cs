using System;
using System.Windows;
using NeoEdit.Program.Controls;

namespace NeoEdit.Program.Dialogs
{
	partial class ViewBinaryEditValueDialog
	{
		[DepProp]
		public string Value { get { return UIHelper<ViewBinaryEditValueDialog>.GetPropValue<string>(this); } set { UIHelper<ViewBinaryEditValueDialog>.SetPropValue(this, value); } }

		static ViewBinaryEditValueDialog() => UIHelper<ViewBinaryEditValueDialog>.Register();

		ViewBinaryEditValueDialog(string value)
		{
			InitializeComponent();
			Value = value;
		}

		void OkClick(object sender, RoutedEventArgs e) => DialogResult = true;

		public static string Run(Window parent, string value)
		{
			var dialog = new ViewBinaryEditValueDialog(value) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.Value;
		}
	}
}
