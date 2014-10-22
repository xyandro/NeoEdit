using System;
using System.Windows;
using NeoEdit.GUI.Common;

namespace NeoEdit.TextEditor.Dialogs
{
	internal partial class ChooseDateTimeDialog
	{
		[DepProp]
		public DateTime Value { get { return UIHelper<ChooseDateTimeDialog>.GetPropValue<DateTime>(this); } set { UIHelper<ChooseDateTimeDialog>.SetPropValue(this, value); } }

		static ChooseDateTimeDialog() { UIHelper<ChooseDateTimeDialog>.Register(); }

		ChooseDateTimeDialog(DateTime value)
		{
			InitializeComponent();

			Value = value;
		}

		void OkClick(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
		}

		static public DateTime? Run(DateTime datetime)
		{
			var dialog = new ChooseDateTimeDialog(datetime);
			return dialog.ShowDialog() == true ? (DateTime?)dialog.Value : null;
		}
	}
}
