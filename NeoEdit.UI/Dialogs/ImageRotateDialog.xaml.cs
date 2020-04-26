using System;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Expressions;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class ImageRotateDialog
	{
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

		ImageRotateDialogResult result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new ImageRotateDialogResult { AngleExpression = AngleExpression };
			angleExpression.AddCurrentSuggestion();
			DialogResult = true;
		}

		public static ImageRotateDialogResult Run(Window parent, NEVariables variables)
		{
			var dialog = new ImageRotateDialog(variables) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
