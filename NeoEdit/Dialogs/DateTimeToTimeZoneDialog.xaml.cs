using System;
using System.Windows;
using NeoEdit.Program.Controls;
using NeoEdit.Program.Transform;

namespace NeoEdit.Program.Dialogs
{
	partial class DateTimeToTimeZoneDialog
	{
		public class Result
		{
			public string TimeZone { get; set; }
		}

		[DepProp]
		public string TimeZone { get { return UIHelper<DateTimeToTimeZoneDialog>.GetPropValue<string>(this); } set { UIHelper<DateTimeToTimeZoneDialog>.SetPropValue(this, value); } }

		static DateTimeToTimeZoneDialog() => UIHelper<DateTimeToTimeZoneDialog>.Register();

		DateTimeToTimeZoneDialog()
		{
			InitializeComponent();
			timeZone.AddSuggestions(Dater.GetAllTimeZones().ToArray());
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result { TimeZone = TimeZone };
			DialogResult = true;
		}

		public static Result Run(Window parent)
		{
			var dialog = new DateTimeToTimeZoneDialog() { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
