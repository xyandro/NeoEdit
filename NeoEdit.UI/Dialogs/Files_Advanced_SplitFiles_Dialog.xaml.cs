using System;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Expressions;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Files_Advanced_SplitFiles_Dialog
	{
		[DepProp]
		public string OutputTemplate { get { return UIHelper<Files_Advanced_SplitFiles_Dialog>.GetPropValue<string>(this); } set { UIHelper<Files_Advanced_SplitFiles_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public string ChunkSize { get { return UIHelper<Files_Advanced_SplitFiles_Dialog>.GetPropValue<string>(this); } set { UIHelper<Files_Advanced_SplitFiles_Dialog>.SetPropValue(this, value); } }
		public NEVariables Variables { get; }

		static Files_Advanced_SplitFiles_Dialog() { UIHelper<Files_Advanced_SplitFiles_Dialog>.Register(); }

		Files_Advanced_SplitFiles_Dialog(NEVariables variables)
		{
			Variables = variables;
			InitializeComponent();
			OutputTemplate = @"$@""{directoryname(x)}\{filenamewithoutextension(x)}-{chunk}{extension(x)}""";
			ChunkSize = "20 mb";
		}

		Configuration_Files_Advanced_SplitFiles result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Configuration_Files_Advanced_SplitFiles { OutputTemplate = OutputTemplate, ChunkSize = ChunkSize };
			DialogResult = true;
		}

		public static Configuration_Files_Advanced_SplitFiles Run(Window parent, NEVariables variables)
		{
			var dialog = new Files_Advanced_SplitFiles_Dialog(variables) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
