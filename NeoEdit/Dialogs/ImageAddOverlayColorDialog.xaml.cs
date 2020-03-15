using System;
using System.Windows;
using NeoEdit.Program.Controls;
using NeoEdit.Program.Expressions;

namespace NeoEdit.Program.Dialogs
{
	partial class ImageAddOverlayColorDialog
	{
		public class Result
		{
			public string Expression { get; set; }
		}

		[DepProp]
		public string Expression { get { return UIHelper<ImageAddOverlayColorDialog>.GetPropValue<string>(this); } set { UIHelper<ImageAddOverlayColorDialog>.SetPropValue(this, value); } }
		public NEVariables Variables { get; }

		static ImageAddOverlayColorDialog() { UIHelper<ImageAddOverlayColorDialog>.Register(); }

		ImageAddOverlayColorDialog(bool add, NEVariables variables)
		{
			Title = $"{(add ? "Add" : "Overlay")} Colors";
			Variables = variables;
			InitializeComponent();
			Expression = "c";
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			expression.AddCurrentSuggestion();
			result = new Result { Expression = Expression };
			DialogResult = true;
		}

		static public Result Run(Window parent, bool add, NEVariables variables)
		{
			var dialog = new ImageAddOverlayColorDialog(add, variables) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
