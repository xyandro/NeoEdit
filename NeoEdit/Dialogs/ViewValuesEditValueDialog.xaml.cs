using System;
using System.Windows;
using NeoEdit.Program.Controls;

namespace NeoEdit.Program.Dialogs
{
	partial class ViewValuesEditValueDialog
	{
		[DepProp]
		public string Value { get { return UIHelper<ViewValuesEditValueDialog>.GetPropValue<string>(this); } set { UIHelper<ViewValuesEditValueDialog>.SetPropValue(this, value); } }

		static ViewValuesEditValueDialog() => UIHelper<ViewValuesEditValueDialog>.Register();

		ViewValuesEditValueDialog(string value)
		{
			InitializeComponent();
			Value = value;
		}

		void OkClick(object sender, RoutedEventArgs e) => DialogResult = true;

		public static string Run(Window parent, string value)
		{
			var dialog = new ViewValuesEditValueDialog(value) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.Value;
		}
	}
}
