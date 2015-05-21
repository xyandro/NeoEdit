using System;
using System.Windows;
using NeoEdit.GUI.Controls;

namespace NeoEdit.TextEdit.Dialogs
{
	internal partial class ChooseDateTimeDialog
	{
		internal class Result
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

		static public Result Run(Window parent, DateTime datetime)
		{
			var dialog = new ChooseDateTimeDialog(datetime) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
