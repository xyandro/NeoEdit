using System;
using System.Windows;
using NeoEdit.Common.Expressions;
using NeoEdit.Common.Models;
using NeoEdit.Program.Controls;

namespace NeoEdit.Program.Dialogs
{
	partial class ImageAddOverlayColorDialog
	{
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

		ImageAddOverlayColorDialogResult result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			expression.AddCurrentSuggestion();
			result = new ImageAddOverlayColorDialogResult { Expression = Expression };
			DialogResult = true;
		}

		public static ImageAddOverlayColorDialogResult Run(Window parent, bool add, NEVariables variables)
		{
			var dialog = new ImageAddOverlayColorDialog(add, variables) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
