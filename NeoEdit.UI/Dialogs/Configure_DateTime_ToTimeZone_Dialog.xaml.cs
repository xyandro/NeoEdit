using System;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Transform;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Configure_DateTime_ToTimeZone_Dialog
	{
		[DepProp]
		public string TimeZone { get { return UIHelper<Configure_DateTime_ToTimeZone_Dialog>.GetPropValue<string>(this); } set { UIHelper<Configure_DateTime_ToTimeZone_Dialog>.SetPropValue(this, value); } }

		static Configure_DateTime_ToTimeZone_Dialog() => UIHelper<Configure_DateTime_ToTimeZone_Dialog>.Register();

		Configure_DateTime_ToTimeZone_Dialog()
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
			var dialog = new Configure_DateTime_ToTimeZone_Dialog() { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
