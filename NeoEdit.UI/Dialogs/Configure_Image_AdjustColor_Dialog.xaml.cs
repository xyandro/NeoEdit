using System;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Expressions;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Configure_Image_AdjustColor_Dialog
	{
		[DepProp]
		public string Expression { get { return UIHelper<Configure_Image_AdjustColor_Dialog>.GetPropValue<string>(this); } set { UIHelper<Configure_Image_AdjustColor_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool Alpha { get { return UIHelper<Configure_Image_AdjustColor_Dialog>.GetPropValue<bool>(this); } set { UIHelper<Configure_Image_AdjustColor_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool Red { get { return UIHelper<Configure_Image_AdjustColor_Dialog>.GetPropValue<bool>(this); } set { UIHelper<Configure_Image_AdjustColor_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool Green { get { return UIHelper<Configure_Image_AdjustColor_Dialog>.GetPropValue<bool>(this); } set { UIHelper<Configure_Image_AdjustColor_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool Blue { get { return UIHelper<Configure_Image_AdjustColor_Dialog>.GetPropValue<bool>(this); } set { UIHelper<Configure_Image_AdjustColor_Dialog>.SetPropValue(this, value); } }
		public NEVariables Variables { get; }

		static Configure_Image_AdjustColor_Dialog() { UIHelper<Configure_Image_AdjustColor_Dialog>.Register(); }

		Configure_Image_AdjustColor_Dialog(NEVariables variables)
		{
			Variables = variables;
			InitializeComponent();
			Expression = "1";
			Red = Green = Blue = true;
		}

		Configuration_Image_AdjustColor result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			expression.AddCurrentSuggestion();
			result = new Configuration_Image_AdjustColor { Expression = Expression, Alpha = Alpha, Red = Red, Green = Green, Blue = Blue };
			DialogResult = true;
		}

		public static Configuration_Image_AdjustColor Run(Window parent, NEVariables variables)
		{
			var dialog = new Configure_Image_AdjustColor_Dialog(variables) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
