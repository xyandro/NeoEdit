using System.Windows;
using NeoEdit.Common.Expressions;
using NeoEdit.GUI.Controls;

namespace NeoEdit.TextEdit.Dialogs
{
	internal partial class FilesOperationsCombineFilesDialog
	{
		internal class Result
		{
			public string InputFiles { get; set; }
			public string InputFileCount { get; set; }
			public string OutputFiles { get; set; }
		}

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

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result { InputFiles = InputFiles, InputFileCount = InputFileCount, OutputFiles = OutputFiles };
			DialogResult = true;
		}

		static public Result Run(Window parent, NEVariables variables)
		{
			var dialog = new FilesOperationsCombineFilesDialog(variables) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
