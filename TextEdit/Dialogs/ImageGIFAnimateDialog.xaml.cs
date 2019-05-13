using System.Windows;
using NeoEdit.TextEdit.Expressions;
using NeoEdit.TextEdit.Controls;
using NeoEdit.TextEdit.Dialogs;

namespace NeoEdit.TextEdit.Dialogs
{
	internal partial class ImageGIFAnimateDialog
	{
		internal class Result
		{
			public string InputFiles { get; set; }
			public string OutputFile { get; set; }
			public string Delay { get; set; }
			public string Repeat { get; set; }
		}

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

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result { InputFiles = InputFiles, OutputFile = OutputFile, Delay = Delay, Repeat = Repeat };
			inputFiles.AddCurrentSuggestion();
			outputFile.AddCurrentSuggestion();
			delay.AddCurrentSuggestion();
			repeat.AddCurrentSuggestion();
			DialogResult = true;
		}

		public static Result Run(Window parent, NEVariables variables)
		{
			var dialog = new ImageGIFAnimateDialog(variables) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
