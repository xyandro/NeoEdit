using System.Windows;
using NeoEdit.Common.Expressions;
using NeoEdit.GUI.Controls;
using NeoEdit.GUI.Dialogs;

namespace NeoEdit.ImageEdit.Dialogs
{
	partial class ImageSizeDialog
	{
		internal class Result
		{
			public string ImageWidth { get; set; }
			public string ImageHeight { get; set; }
		}

		[DepProp]
		public string ImageWidth { get { return UIHelper<ImageSizeDialog>.GetPropValue<string>(this); } set { UIHelper<ImageSizeDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string ImageHeight { get { return UIHelper<ImageSizeDialog>.GetPropValue<string>(this); } set { UIHelper<ImageSizeDialog>.SetPropValue(this, value); } }
		public NEVariables Variables { get; }

		static ImageSizeDialog() { UIHelper<ImageSizeDialog>.Register(); }

		ImageSizeDialog(NEVariables variables)
		{
			Variables = variables;
			InitializeComponent();
			ImageWidth = "w";
			ImageHeight = "h";
		}

		private void ExpressionHelp(object sender, RoutedEventArgs e) => ExpressionHelpDialog.Display();

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result { ImageWidth = ImageWidth, ImageHeight = ImageHeight };
			imageWidth.AddCurrentSuggestion();
			imageHeight.AddCurrentSuggestion();
			DialogResult = true;
		}

		void ScaleWidth(object sender, RoutedEventArgs e) => ImageWidth = $"({ImageHeight}) * w / h";

		void ScaleHeight(object sender, RoutedEventArgs e) => ImageHeight = $"({ImageWidth}) * h / w";

		public static Result Run(Window parent, NEVariables variables)
		{
			var dialog = new ImageSizeDialog(variables) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
