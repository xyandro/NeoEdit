using System;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Configure_Network_WCF_InterceptCalls_Dialog
	{
		[DepProp]
		public string WCFURL { get { return UIHelper<Configure_Network_WCF_InterceptCalls_Dialog>.GetPropValue<string>(this); } set { UIHelper<Configure_Network_WCF_InterceptCalls_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public string InterceptURL { get { return UIHelper<Configure_Network_WCF_InterceptCalls_Dialog>.GetPropValue<string>(this); } set { UIHelper<Configure_Network_WCF_InterceptCalls_Dialog>.SetPropValue(this, value); } }

		static Configure_Network_WCF_InterceptCalls_Dialog() { UIHelper<Configure_Network_WCF_InterceptCalls_Dialog>.Register(); }

		Configure_Network_WCF_InterceptCalls_Dialog()
		{
			InitializeComponent();
			WCFURL = wcfURL.GetLastSuggestion();
			InterceptURL = interceptURL.GetLastSuggestion();
		}

		Configuration_Network_WCF_InterceptCalls result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Configuration_Network_WCF_InterceptCalls { WCFURL = WCFURL, InterceptURL = InterceptURL };
			DialogResult = true;
			wcfURL.AddCurrentSuggestion();
			interceptURL.AddCurrentSuggestion();
		}

		public static Configuration_Network_WCF_InterceptCalls Run(Window parent)
		{
			var dialog = new Configure_Network_WCF_InterceptCalls_Dialog() { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
