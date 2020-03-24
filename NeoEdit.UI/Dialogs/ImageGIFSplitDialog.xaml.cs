using System;
using System.Windows;
using NeoEdit.Common.Expressions;
using NeoEdit.Common.Models;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class ImageGIFSplitDialog
	{
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

		ImageGIFSplitDialogResult result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new ImageGIFSplitDialogResult { OutputTemplate = OutputTemplate };
			outputTemplate.AddCurrentSuggestion();
			DialogResult = true;
		}

		public static ImageGIFSplitDialogResult Run(Window parent, NEVariables variables)
		{
			var dialog = new ImageGIFSplitDialog(variables) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
