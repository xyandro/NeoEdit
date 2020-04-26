using System;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Expressions;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Configure_Files_Operations_SplitFile_Dialog
	{
		[DepProp]
		public string OutputTemplate { get { return UIHelper<Configure_Files_Operations_SplitFile_Dialog>.GetPropValue<string>(this); } set { UIHelper<Configure_Files_Operations_SplitFile_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public string ChunkSize { get { return UIHelper<Configure_Files_Operations_SplitFile_Dialog>.GetPropValue<string>(this); } set { UIHelper<Configure_Files_Operations_SplitFile_Dialog>.SetPropValue(this, value); } }
		public NEVariables Variables { get; }

		static Configure_Files_Operations_SplitFile_Dialog() { UIHelper<Configure_Files_Operations_SplitFile_Dialog>.Register(); }

		Configure_Files_Operations_SplitFile_Dialog(NEVariables variables)
		{
			Variables = variables;
			InitializeComponent();
			OutputTemplate = @"$@""{directoryname(x)}\{filenamewithoutextension(x)}-{chunk}{extension(x)}""";
			ChunkSize = "20 mb";
		}

		Configuration_Files_Operations_SplitFile result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Configuration_Files_Operations_SplitFile { OutputTemplate = OutputTemplate, ChunkSize = ChunkSize };
			DialogResult = true;
		}

		public static Configuration_Files_Operations_SplitFile Run(Window parent, NEVariables variables)
		{
			var dialog = new Configure_Files_Operations_SplitFile_Dialog(variables) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
