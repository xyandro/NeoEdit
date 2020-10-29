using System;
using System.Linq;
using System.Windows;
using NeoEdit.Common;
using NeoEdit.Common.Configuration;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Network_WCF_GetConfig_Dialog
	{
		[DepProp]
		public string URL { get { return UIHelper<Network_WCF_GetConfig_Dialog>.GetPropValue<string>(this); } set { UIHelper<Network_WCF_GetConfig_Dialog>.SetPropValue(this, value); } }

		static Network_WCF_GetConfig_Dialog()
		{
			UIHelper<Network_WCF_GetConfig_Dialog>.Register();
			AutoCompleteTextBox.AddTagSuggestions("NetworkWCFGetConfigURL", Settings.WCFURLs.ToArray());
		}

		Network_WCF_GetConfig_Dialog()
		{
			InitializeComponent();
			URL = url.GetLastSuggestion();
		}

		Configuration_Network_WCF_GetConfig result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Configuration_Network_WCF_GetConfig { URL = URL };
			DialogResult = true;
			url.AddCurrentSuggestion();
		}

		public static Configuration_Network_WCF_GetConfig Run(Window parent)
		{
			var dialog = new Network_WCF_GetConfig_Dialog() { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
