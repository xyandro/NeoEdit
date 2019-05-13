using System.Windows;
using NeoEdit.TextEdit.Expressions;
using NeoEdit.TextEdit.Controls;
using NeoEdit.TextEdit.Dialogs;

namespace NeoEdit.TextEdit.Dialogs
{
	internal partial class ImageRotateDialog
	{
		internal class Result
		{
			public string AngleExpression { get; set; }
		}

		[DepProp]
		public string AngleExpression { get { return UIHelper<ImageRotateDialog>.GetPropValue<string>(this); } set { UIHelper<ImageRotateDialog>.SetPropValue(this, value); } }
		public NEVariables Variables { get; }

		static ImageRotateDialog() { UIHelper<ImageRotateDialog>.Register(); }

		ImageRotateDialog(NEVariables variables)
		{
			Variables = variables;

			InitializeComponent();

			AngleExpression = "0 deg";
		}

		void ExpressionHelp(object sender, RoutedEventArgs e) => ExpressionHelpDialog.Display(Variables);

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result { AngleExpression = AngleExpression };
			angleExpression.AddCurrentSuggestion();
			DialogResult = true;
		}

		public static Result Run(Window parent, NEVariables variables)
		{
			var dialog = new ImageRotateDialog(variables) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
