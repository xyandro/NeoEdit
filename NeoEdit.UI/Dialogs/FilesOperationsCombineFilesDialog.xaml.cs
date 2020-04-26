using System;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Expressions;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class FilesOperationsCombineFilesDialog
	{
		[DepProp]
		public string InputFiles { get { return UIHelper<FilesOperationsCombineFilesDialog>.GetPropValue<string>(this); } set { UIHelper<FilesOperationsCombineFilesDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string InputFileCount { get { return UIHelper<FilesOperationsCombineFilesDialog>.GetPropValue<string>(this); } set { UIHelper<FilesOperationsCombineFilesDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string OutputFiles { get { return UIHelper<FilesOperationsCombineFilesDialog>.GetPropValue<string>(this); } set { UIHelper<FilesOperationsCombineFilesDialog>.SetPropValue(this, value); } }
		public NEVariables Variables { get; }

		static FilesOperationsCombineFilesDialog() { UIHelper<FilesOperationsCombineFilesDialog>.Register(); }

		FilesOperationsCombineFilesDialog(NEVariables variables)
		{
			Variables = variables;
			InitializeComponent();
			InputFiles = "x";
			InputFileCount = "xn";
			OutputFiles = @"$@""{directoryname(xtmin)}\{filenamewithoutextension(xtmin)}-Combine{extension(xtmin)}""";
		}

		Configuration_Files_Operations_CombineFiles result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Configuration_Files_Operations_CombineFiles { InputFiles = InputFiles, InputFileCount = InputFileCount, OutputFiles = OutputFiles };
			DialogResult = true;
		}

		public static Configuration_Files_Operations_CombineFiles Run(Window parent, NEVariables variables)
		{
			var dialog = new FilesOperationsCombineFilesDialog(variables) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
