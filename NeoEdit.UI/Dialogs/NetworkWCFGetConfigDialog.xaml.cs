using System;
using System.Linq;
using System.Windows;
using NeoEdit.Common;
using NeoEdit.Common.Configuration;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class NetworkWCFGetConfigDialog
	{
		[DepProp]
		public string URL { get { return UIHelper<NetworkWCFGetConfigDialog>.GetPropValue<string>(this); } set { UIHelper<NetworkWCFGetConfigDialog>.SetPropValue(this, value); } }

		static NetworkWCFGetConfigDialog()
		{
			UIHelper<NetworkWCFGetConfigDialog>.Register();
			AutoCompleteTextBox.AddTagSuggestions("NetworkWCFGetConfigURL", Settings.WCFURLs.ToArray());
		}

		NetworkWCFGetConfigDialog()
		{
			InitializeComponent();
			URL = url.GetLastSuggestion();
		}

		NetworkWCFGetConfigDialogResult result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new NetworkWCFGetConfigDialogResult { URL = URL };
			DialogResult = true;
			url.AddCurrentSuggestion();
		}

		static public NetworkWCFGetConfigDialogResult Run(Window parent)
		{
			var dialog = new NetworkWCFGetConfigDialog() { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
