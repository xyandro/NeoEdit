using System;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Expressions;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class NetworkFetchFileDialog
	{
		[DepProp]
		public string URL { get { return UIHelper<NetworkFetchFileDialog>.GetPropValue<string>(this); } set { UIHelper<NetworkFetchFileDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string FileName { get { return UIHelper<NetworkFetchFileDialog>.GetPropValue<string>(this); } set { UIHelper<NetworkFetchFileDialog>.SetPropValue(this, value); } }

		public NEVariables Variables { get; }

		static NetworkFetchFileDialog() { UIHelper<NetworkFetchFileDialog>.Register(); }

		NetworkFetchFileDialog(NEVariables variables)
		{
			Variables = variables;
			InitializeComponent();
			URL = FileName = "x";
		}

		void ExpressionHelp(object sender, RoutedEventArgs e) => ExpressionHelpDialog.Display(Variables);

		Configuration_Network_FetchFile result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Configuration_Network_FetchFile { URL = URL, FileName = FileName };
			url.AddCurrentSuggestion();
			fileName.AddCurrentSuggestion();
			DialogResult = true;
		}

		public static Configuration_Network_FetchFile Run(Window parent, NEVariables variables)
		{
			var dialog = new NetworkFetchFileDialog(variables) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
