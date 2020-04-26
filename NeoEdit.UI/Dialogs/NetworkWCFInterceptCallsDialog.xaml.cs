using System;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class NetworkWCFInterceptCallsDialog
	{
		[DepProp]
		public string WCFURL { get { return UIHelper<NetworkWCFInterceptCallsDialog>.GetPropValue<string>(this); } set { UIHelper<NetworkWCFInterceptCallsDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string InterceptURL { get { return UIHelper<NetworkWCFInterceptCallsDialog>.GetPropValue<string>(this); } set { UIHelper<NetworkWCFInterceptCallsDialog>.SetPropValue(this, value); } }

		static NetworkWCFInterceptCallsDialog() { UIHelper<NetworkWCFInterceptCallsDialog>.Register(); }

		NetworkWCFInterceptCallsDialog()
		{
			InitializeComponent();
			WCFURL = wcfURL.GetLastSuggestion();
			InterceptURL = interceptURL.GetLastSuggestion();
		}

		NetworkWCFInterceptCallsDialogResult result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new NetworkWCFInterceptCallsDialogResult { WCFURL = WCFURL, InterceptURL = InterceptURL };
			DialogResult = true;
			wcfURL.AddCurrentSuggestion();
			interceptURL.AddCurrentSuggestion();
		}

		public static NetworkWCFInterceptCallsDialogResult Run(Window parent)
		{
			var dialog = new NetworkWCFInterceptCallsDialog() { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
