using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows;
using NeoEdit.Common;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Expressions;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Configure_Image_Size_Dialog
	{
		[DepProp]
		public string WidthExpression { get { return UIHelper<Configure_Image_Size_Dialog>.GetPropValue<string>(this); } set { UIHelper<Configure_Image_Size_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public string HeightExpression { get { return UIHelper<Configure_Image_Size_Dialog>.GetPropValue<string>(this); } set { UIHelper<Configure_Image_Size_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public InterpolationMode InterpolationMode { get { return UIHelper<Configure_Image_Size_Dialog>.GetPropValue<InterpolationMode>(this); } set { UIHelper<Configure_Image_Size_Dialog>.SetPropValue(this, value); } }
		public NEVariables Variables { get; }
		public List<InterpolationMode> InterpolationModes { get; }

		static Configure_Image_Size_Dialog()
		{
			UIHelper<Configure_Image_Size_Dialog>.Register();
			UIHelper<Configure_Image_Size_Dialog>.AddCallback(a => a.WidthExpression, (obj, o, n) => obj.SetWidth(o));
			UIHelper<Configure_Image_Size_Dialog>.AddCallback(a => a.HeightExpression, (obj, o, n) => obj.SetHeight(o));
		}

		Configure_Image_Size_Dialog(NEVariables variables)
		{
			Variables = variables;
			InterpolationModes = Enum.GetValues(typeof(InterpolationMode)).OfType<InterpolationMode>().Except(InterpolationMode.Invalid).ToList();

			InitializeComponent();

			OnResetClick();
		}

		string GetARHeight(string width) => width == "width" ? "height" : $"({width}) * height / width";

		string GetARWidth(string height) => height == "height" ? "width" : $"({height}) * width / height";

		void SetWidth(string oldWidthExpression)
		{
			if (oldWidthExpression == null)
				return;

			if (HeightExpression == GetARHeight(oldWidthExpression))
				HeightExpression = GetARHeight(WidthExpression);
		}

		void SetHeight(string oldHeightExpression)
		{
			if (oldHeightExpression == null)
				return;

			if (WidthExpression == GetARWidth(oldHeightExpression))
				WidthExpression = GetARWidth(HeightExpression);
		}

		void OnResetClick(object sender = null, RoutedEventArgs e = null)
		{
			WidthExpression = "width";
			HeightExpression = "height";
			InterpolationMode = InterpolationMode.HighQualityBicubic;
		}

		void ExpressionHelp(object sender, RoutedEventArgs e) => ExpressionHelpDialog.Display(Variables);

		Configuration_Image_Size result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Configuration_Image_Size { WidthExpression = WidthExpression, HeightExpression = HeightExpression, InterpolationMode = InterpolationMode };
			widthExpression.AddCurrentSuggestion();
			heightExpression.AddCurrentSuggestion();
			DialogResult = true;
		}

		public static Configuration_Image_Size Run(Window parent, NEVariables variables)
		{
			var dialog = new Configure_Image_Size_Dialog(variables) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
