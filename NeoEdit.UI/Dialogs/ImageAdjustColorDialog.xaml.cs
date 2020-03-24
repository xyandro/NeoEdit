using System;
using System.Windows;
using NeoEdit.Common.Expressions;
using NeoEdit.Common.Models;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class ImageAdjustColorDialog
	{
		[DepProp]
		public string Expression { get { return UIHelper<ImageAdjustColorDialog>.GetPropValue<string>(this); } set { UIHelper<ImageAdjustColorDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool Alpha { get { return UIHelper<ImageAdjustColorDialog>.GetPropValue<bool>(this); } set { UIHelper<ImageAdjustColorDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool Red { get { return UIHelper<ImageAdjustColorDialog>.GetPropValue<bool>(this); } set { UIHelper<ImageAdjustColorDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool Green { get { return UIHelper<ImageAdjustColorDialog>.GetPropValue<bool>(this); } set { UIHelper<ImageAdjustColorDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool Blue { get { return UIHelper<ImageAdjustColorDialog>.GetPropValue<bool>(this); } set { UIHelper<ImageAdjustColorDialog>.SetPropValue(this, value); } }
		public NEVariables Variables { get; }

		static ImageAdjustColorDialog() { UIHelper<ImageAdjustColorDialog>.Register(); }

		ImageAdjustColorDialog(NEVariables variables)
		{
			Variables = variables;
			InitializeComponent();
			Expression = "1";
			Red = Green = Blue = true;
		}

		ImageAdjustColorDialogResult result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			expression.AddCurrentSuggestion();
			result = new ImageAdjustColorDialogResult { Expression = Expression, Alpha = Alpha, Red = Red, Green = Green, Blue = Blue };
			DialogResult = true;
		}

		public static ImageAdjustColorDialogResult Run(Window parent, NEVariables variables)
		{
			var dialog = new ImageAdjustColorDialog(variables) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
