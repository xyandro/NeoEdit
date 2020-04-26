using System;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Expressions;
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

		Configuration_Image_GIF_Split result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Configuration_Image_GIF_Split { OutputTemplate = OutputTemplate };
			outputTemplate.AddCurrentSuggestion();
			DialogResult = true;
		}

		public static Configuration_Image_GIF_Split Run(Window parent, NEVariables variables)
		{
			var dialog = new ImageGIFSplitDialog(variables) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
