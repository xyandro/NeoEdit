using System.Windows;
using NeoEdit.Expressions;
using NeoEdit.Controls;

namespace NeoEdit.Dialogs
{
	internal partial class FilesOperationsSplitFileDialog
	{
		internal class Result
		{
			public string OutputTemplate { get; set; }
			public string ChunkSize { get; set; }
		}

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

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result { OutputTemplate = OutputTemplate, ChunkSize = ChunkSize };
			DialogResult = true;
		}

		static public Result Run(Window parent, NEVariables variables)
		{
			var dialog = new FilesOperationsSplitFileDialog(variables) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
