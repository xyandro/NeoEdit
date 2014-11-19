using System;
using System.Windows;
using NeoEdit.GUI.Common;
using NeoEdit.GUI.Dialogs;

namespace NeoEdit.TextEditor.Dialogs
{
	internal partial class ChooseDateTimeDialog
	{
		internal class Result : IDialogResult
		{
			public DateTime Value { get; set; }
		}

		[DepProp]
		public DateTime Value { get { return UIHelper<ChooseDateTimeDialog>.GetPropValue<DateTime>(this); } set { UIHelper<ChooseDateTimeDialog>.SetPropValue(this, value); } }

		static ChooseDateTimeDialog() { UIHelper<ChooseDateTimeDialog>.Register(); }

		ChooseDateTimeDialog(DateTime value)
		{
			InitializeComponent();

			Value = value;
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result { Value = Value };
			DialogResult = true;
		}

		static public Result Run(DateTime datetime)
		{
			var dialog = new ChooseDateTimeDialog(datetime);
			return dialog.ShowDialog() == true ? dialog.result : null;
		}
	}
}
