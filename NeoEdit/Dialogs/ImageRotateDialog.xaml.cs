using System;
using System.Windows;
using NeoEdit.Program.Controls;
using NeoEdit.Program.Expressions;

namespace NeoEdit.Program.Dialogs
{
	partial class ImageRotateDialog
	{
		public class Result
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
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
