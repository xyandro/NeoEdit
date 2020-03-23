using System;
using System.Windows;
using NeoEdit.Program.Controls;
using NeoEdit.Program.Models;
using NeoEdit.Common.Transform;

namespace NeoEdit.Program.Dialogs
{
	partial class DateTimeToTimeZoneDialog
	{
		[DepProp]
		public string TimeZone { get { return UIHelper<DateTimeToTimeZoneDialog>.GetPropValue<string>(this); } set { UIHelper<DateTimeToTimeZoneDialog>.SetPropValue(this, value); } }

		static DateTimeToTimeZoneDialog() => UIHelper<DateTimeToTimeZoneDialog>.Register();

		DateTimeToTimeZoneDialog()
		{
			InitializeComponent();
			timeZone.AddSuggestions(Dater.GetAllTimeZones().ToArray());
		}

		DateTimeToTimeZoneDialogResult result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new DateTimeToTimeZoneDialogResult { TimeZone = TimeZone };
			DialogResult = true;
		}

		public static DateTimeToTimeZoneDialogResult Run(Window parent)
		{
			var dialog = new DateTimeToTimeZoneDialog() { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
