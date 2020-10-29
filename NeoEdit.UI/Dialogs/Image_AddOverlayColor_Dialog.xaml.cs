using System;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Expressions;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Image_AddOverlayColor_Dialog
	{
		[DepProp]
		public string Expression { get { return UIHelper<Image_AddOverlayColor_Dialog>.GetPropValue<string>(this); } set { UIHelper<Image_AddOverlayColor_Dialog>.SetPropValue(this, value); } }
		public NEVariables Variables { get; }

		static Image_AddOverlayColor_Dialog() { UIHelper<Image_AddOverlayColor_Dialog>.Register(); }

		Image_AddOverlayColor_Dialog(bool add, NEVariables variables)
		{
			Title = $"{(add ? "Add" : "Overlay")} Colors";
			Variables = variables;
			InitializeComponent();
			Expression = "c";
		}

		Configuration_Image_AddOverlayColor result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			expression.AddCurrentSuggestion();
			result = new Configuration_Image_AddOverlayColor { Expression = Expression };
			DialogResult = true;
		}

		public static Configuration_Image_AddOverlayColor Run(Window parent, bool add, NEVariables variables)
		{
			var dialog = new Image_AddOverlayColor_Dialog(add, variables) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
