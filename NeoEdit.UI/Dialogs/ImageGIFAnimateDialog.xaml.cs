using System;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Expressions;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class ImageGIFAnimateDialog
	{
		[DepProp]
		public string InputFiles { get { return UIHelper<ImageGIFAnimateDialog>.GetPropValue<string>(this); } set { UIHelper<ImageGIFAnimateDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string OutputFile { get { return UIHelper<ImageGIFAnimateDialog>.GetPropValue<string>(this); } set { UIHelper<ImageGIFAnimateDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string Delay { get { return UIHelper<ImageGIFAnimateDialog>.GetPropValue<string>(this); } set { UIHelper<ImageGIFAnimateDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string Repeat { get { return UIHelper<ImageGIFAnimateDialog>.GetPropValue<string>(this); } set { UIHelper<ImageGIFAnimateDialog>.SetPropValue(this, value); } }

		public NEVariables Variables { get; }

		static ImageGIFAnimateDialog() { UIHelper<ImageGIFAnimateDialog>.Register(); }

		ImageGIFAnimateDialog(NEVariables variables)
		{
			Variables = variables;
			InitializeComponent();
			InputFiles = OutputFile = "x";
			Delay = "100 ms";
			Repeat = "0";
		}

		private void ExpressionHelp(object sender, RoutedEventArgs e) => ExpressionHelpDialog.Display(Variables);

		Configuration_Image_GIF_Animate result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Configuration_Image_GIF_Animate { InputFiles = InputFiles, OutputFile = OutputFile, Delay = Delay, Repeat = Repeat };
			inputFiles.AddCurrentSuggestion();
			outputFile.AddCurrentSuggestion();
			delay.AddCurrentSuggestion();
			repeat.AddCurrentSuggestion();
			DialogResult = true;
		}

		public static Configuration_Image_GIF_Animate Run(Window parent, NEVariables variables)
		{
			var dialog = new ImageGIFAnimateDialog(variables) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
