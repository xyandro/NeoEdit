using System;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Expressions;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Network_Fetch_File_Dialog
	{
		[DepProp]
		public string URL { get { return UIHelper<Network_Fetch_File_Dialog>.GetPropValue<string>(this); } set { UIHelper<Network_Fetch_File_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public string FileName { get { return UIHelper<Network_Fetch_File_Dialog>.GetPropValue<string>(this); } set { UIHelper<Network_Fetch_File_Dialog>.SetPropValue(this, value); } }

		public NEVariables Variables { get; }

		static Network_Fetch_File_Dialog() { UIHelper<Network_Fetch_File_Dialog>.Register(); }

		Network_Fetch_File_Dialog(NEVariables variables)
		{
			Variables = variables;
			InitializeComponent();
			URL = FileName = "x";
		}

		void ExpressionHelp(object sender, RoutedEventArgs e) => ExpressionHelpDialog.Display(Variables);

		Configuration_Network_Fetch_File result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Configuration_Network_Fetch_File { URL = URL, FileName = FileName };
			url.AddCurrentSuggestion();
			fileName.AddCurrentSuggestion();
			DialogResult = true;
		}

		public static Configuration_Network_Fetch_File Run(Window parent, NEVariables variables)
		{
			var dialog = new Network_Fetch_File_Dialog(variables) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
