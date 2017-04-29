using System;
using System.Windows;
using NeoEdit.GUI.Controls;

namespace NeoEdit.TextEdit.Dialogs
{
	internal partial class FilesSetTimeDialog
	{
		internal class Result
		{
			public DateTime Value { get; set; }
		}

		[DepProp]
		public DateTime Value { get { return UIHelper<FilesSetTimeDialog>.GetPropValue<DateTime>(this); } set { UIHelper<FilesSetTimeDialog>.SetPropValue(this, value); } }

		static FilesSetTimeDialog() { UIHelper<FilesSetTimeDialog>.Register(); }

		FilesSetTimeDialog(DateTime value)
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
			var dialog = new FilesSetTimeDialog(datetime) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
