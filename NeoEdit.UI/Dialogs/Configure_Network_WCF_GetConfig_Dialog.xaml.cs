using System;
using System.Linq;
using System.Windows;
using NeoEdit.Common;
using NeoEdit.Common.Configuration;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Configure_Network_WCF_GetConfig_Dialog
	{
		[DepProp]
		public string URL { get { return UIHelper<Configure_Network_WCF_GetConfig_Dialog>.GetPropValue<string>(this); } set { UIHelper<Configure_Network_WCF_GetConfig_Dialog>.SetPropValue(this, value); } }

		static Configure_Network_WCF_GetConfig_Dialog()
		{
			UIHelper<Configure_Network_WCF_GetConfig_Dialog>.Register();
			AutoCompleteTextBox.AddTagSuggestions("NetworkWCFGetConfigURL", Settings.WCFURLs.ToArray());
		}

		Configure_Network_WCF_GetConfig_Dialog()
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

		static public Configuration_Network_WCF_GetConfig Run(Window parent)
		{
			var dialog = new Configure_Network_WCF_GetConfig_Dialog() { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
