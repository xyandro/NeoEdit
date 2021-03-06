﻿using System.Linq;
using System.Windows;
using NeoEdit.Program.Controls;

namespace NeoEdit.Program.Dialogs
{
	partial class NetworkWCFGetConfig
	{
		public class Result
		{
			public string URL { get; set; }
		}

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

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result { URL = URL };
			DialogResult = true;
			url.AddCurrentSuggestion();
		}

		static public Result Run(Window parent)
		{
			var dialog = new NetworkWCFGetConfig() { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
