using System;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Expressions;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class FilesOperationsSplitFileDialog
	{
		[DepProp]
		public string OutputTemplate { get { return UIHelper<FilesOperationsSplitFileDialog>.GetPropValue<string>(this); } set { UIHelper<FilesOperationsSplitFileDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string ChunkSize { get { return UIHelper<FilesOperationsSplitFileDialog>.GetPropValue<string>(this); } set { UIHelper<FilesOperationsSplitFileDialog>.SetPropValue(this, value); } }
		public NEVariables Variables { get; }

		static FilesOperationsSplitFileDialog() { UIHelper<FilesOperationsSplitFileDialog>.Register(); }

		FilesOperationsSplitFileDialog(NEVariables variables)
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
			var dialog = new FilesOperationsSplitFileDialog(variables) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
