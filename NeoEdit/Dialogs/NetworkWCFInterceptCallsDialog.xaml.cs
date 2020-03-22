using System;
using System.Windows;
using NeoEdit.Program.Controls;
using NeoEdit.Program.Models;

namespace NeoEdit.Program.Dialogs
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

		static public NetworkWCFInterceptCallsDialogResult Run(Window parent)
		{
			var dialog = new NetworkWCFInterceptCallsDialog() { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
