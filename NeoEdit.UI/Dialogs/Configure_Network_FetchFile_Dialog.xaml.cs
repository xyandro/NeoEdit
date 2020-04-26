using System;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Expressions;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Configure_Network_FetchFile_Dialog
	{
		[DepProp]
		public string URL { get { return UIHelper<Configure_Network_FetchFile_Dialog>.GetPropValue<string>(this); } set { UIHelper<Configure_Network_FetchFile_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public string FileName { get { return UIHelper<Configure_Network_FetchFile_Dialog>.GetPropValue<string>(this); } set { UIHelper<Configure_Network_FetchFile_Dialog>.SetPropValue(this, value); } }

		public NEVariables Variables { get; }

		static Configure_Network_FetchFile_Dialog() { UIHelper<Configure_Network_FetchFile_Dialog>.Register(); }

		Configure_Network_FetchFile_Dialog(NEVariables variables)
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
			var dialog = new Configure_Network_FetchFile_Dialog(variables) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
