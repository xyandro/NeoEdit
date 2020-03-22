using System;
using System.Linq;
using System.Windows;
using NeoEdit.Program.Controls;
using NeoEdit.Program.Models;

namespace NeoEdit.Program.Dialogs
{
	partial class NetworkWCFGetConfig
	{
		[DepProp]
		public string URL { get { return UIHelper<NetworkWCFGetConfig>.GetPropValue<string>(this); } set { UIHelper<NetworkWCFGetConfig>.SetPropValue(this, value); } }

		static NetworkWCFGetConfig()
		{
			UIHelper<NetworkWCFGetConfig>.Register();
			AutoCompleteTextBox.AddTagSuggestions("NetworkWCFGetConfigURL", Settings.WCFURLs.ToArray());
		}

		NetworkWCFGetConfig()
		{
			InitializeComponent();
			URL = url.GetLastSuggestion();
		}

		NetworkWCFGetConfigResult result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new NetworkWCFGetConfigResult { URL = URL };
			DialogResult = true;
			url.AddCurrentSuggestion();
		}

		static public NetworkWCFGetConfigResult Run(Window parent)
		{
			var dialog = new NetworkWCFGetConfig() { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
