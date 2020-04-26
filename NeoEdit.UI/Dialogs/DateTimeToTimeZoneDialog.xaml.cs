using System;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Transform;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
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

		Configuration_DateTime_ToTimeZone result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Configuration_DateTime_ToTimeZone { TimeZone = TimeZone };
			DialogResult = true;
		}

		public static Configuration_DateTime_ToTimeZone Run(Window parent)
		{
			var dialog = new DateTimeToTimeZoneDialog() { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
