using System.Windows;
using NeoEdit.Controls;
using NeoEdit.Expressions;

namespace NeoEdit.Dialogs
{
	partial class ImageGIFSplitDialog
	{
		public class Result
		{
			public string OutputTemplate { get; set; }
		}

		[DepProp]
		public string OutputTemplate { get { return UIHelper<ImageGIFSplitDialog>.GetPropValue<string>(this); } set { UIHelper<ImageGIFSplitDialog>.SetPropValue(this, value); } }
		public NEVariables Variables { get; }

		static ImageGIFSplitDialog() { UIHelper<ImageGIFSplitDialog>.Register(); }

		ImageGIFSplitDialog(NEVariables variables)
		{
			Variables = variables;
			InitializeComponent();
			OutputTemplate = @"$@""{directoryname(x)}\{filenamewithoutextension(x)}-{chunk}.png""";
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result { OutputTemplate = OutputTemplate };
			outputTemplate.AddCurrentSuggestion();
			DialogResult = true;
		}

		static public Result Run(Window parent, NEVariables variables)
		{
			var dialog = new ImageGIFSplitDialog(variables) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
