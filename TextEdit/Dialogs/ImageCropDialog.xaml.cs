using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using NeoEdit.Common.Expressions;
using NeoEdit.GUI.Controls;
using NeoEdit.GUI.Dialogs;

namespace NeoEdit.TextEdit.Dialogs
{
	internal partial class ImageCropDialog
	{
		internal class Result
		{
			public string XExpression { get; set; }
			public string YExpression { get; set; }
			public string WidthExpression { get; set; }
			public string HeightExpression { get; set; }
		}

		[DepProp]
		public string XExpression { get { return UIHelper<ImageCropDialog>.GetPropValue<string>(this); } set { UIHelper<ImageCropDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string YExpression { get { return UIHelper<ImageCropDialog>.GetPropValue<string>(this); } set { UIHelper<ImageCropDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string WidthExpression { get { return UIHelper<ImageCropDialog>.GetPropValue<string>(this); } set { UIHelper<ImageCropDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string HeightExpression { get { return UIHelper<ImageCropDialog>.GetPropValue<string>(this); } set { UIHelper<ImageCropDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string AspectRatio { get { return UIHelper<ImageCropDialog>.GetPropValue<string>(this); } set { UIHelper<ImageCropDialog>.SetPropValue(this, value); } }
		public NEVariables Variables { get; }

		static ImageCropDialog()
		{
			UIHelper<ImageCropDialog>.Register();
			UIHelper<ImageCropDialog>.AddCallback(a => a.WidthExpression, (obj, o, n) => obj.SetWidth(o));
			UIHelper<ImageCropDialog>.AddCallback(a => a.HeightExpression, (obj, o, n) => obj.SetHeight(o));
			UIHelper<ImageCropDialog>.AddCallback(a => a.AspectRatio, (obj, o, n) => obj.SetAspectRatio(o));
		}

		ImageCropDialog(NEVariables variables)
		{
			Variables = variables;

			InitializeComponent();

			OnResetClick();
		}

		string GetARWidth(string height) => height == "height" ? "width" : $"({height}) * width / height";
		string GetARHeight(string width) => width == "width" ? "height" : $"({width}) * height / width";

		string GetRatioWidth(string aspectRatio) => $"min(width, height * ({aspectRatio}))";
		string GetRatioHeight(string aspectRatio) => $"min(width / ({aspectRatio}), height)";

		string GetXLeft(string width) => "0";
		string GetXCenter(string width) => $"(({width}) - width) / 2";
		string GetXRight(string width) => $"({width}) - width";
		string GetYTop(string height) => "0";
		string GetYCenter(string height) => $"(({height}) - height) / 2";
		string GetYBottom(string height) => $"({height}) - height";

		void SetWidth(string oldWidthExpression)
		{
			if (oldWidthExpression == null)
				return;

			if (HeightExpression == GetARHeight(oldWidthExpression))
				HeightExpression = GetARHeight(WidthExpression);
			if (XExpression == GetXLeft(oldWidthExpression))
				XExpression = GetXLeft(WidthExpression);
			else if (XExpression == GetXCenter(oldWidthExpression))
				XExpression = GetXCenter(WidthExpression);
			else if (XExpression == GetXRight(oldWidthExpression))
				XExpression = GetXRight(WidthExpression);
		}

		void SetHeight(string oldHeightExpression)
		{
			if (oldHeightExpression == null)
				return;

			if (WidthExpression == GetARWidth(oldHeightExpression))
				WidthExpression = GetARWidth(HeightExpression);
			if (YExpression == GetYTop(oldHeightExpression))
				YExpression = GetYTop(HeightExpression);
			else if (YExpression == GetYCenter(oldHeightExpression))
				YExpression = GetYCenter(HeightExpression);
			else if (YExpression == GetYBottom(oldHeightExpression))
				YExpression = GetYBottom(HeightExpression);
		}

		void SetAspectRatio(string oldAspectRatio)
		{
			if (oldAspectRatio == null)
				return;

			if (WidthExpression == GetRatioWidth(oldAspectRatio))
				WidthExpression = GetRatioWidth(AspectRatio);
			if (HeightExpression == GetRatioHeight(oldAspectRatio))
				HeightExpression = GetRatioHeight(AspectRatio);
		}

		void OnChangeAnchor(object sender, RoutedEventArgs e)
		{
			var anchor = ((sender as FrameworkElement).Tag as string).Split(',');
			switch (anchor[0])
			{
				case "Top": YExpression = GetYTop(HeightExpression); break;
				case "Center": YExpression = GetYCenter(HeightExpression); break;
				case "Bottom": YExpression = GetYBottom(HeightExpression); break;
			}
			switch (anchor[1])
			{
				case "Left": XExpression = GetXLeft(WidthExpression); break;
				case "Center": XExpression = GetXCenter(WidthExpression); break;
				case "Right": XExpression = GetXRight(WidthExpression); break;
			}
		}

		internal bool IsChecked(string position)
		{
			var anchor = position.Split(',');
			switch (anchor[0])
			{
				case "Top": if (YExpression != GetYTop(HeightExpression)) return false; break;
				case "Center": if (YExpression != GetYCenter(HeightExpression)) return false; break;
				case "Bottom": if (YExpression != GetYBottom(HeightExpression)) return false; break;
			}
			switch (anchor[1])
			{
				case "Left": if (XExpression != GetXLeft(WidthExpression)) return false; break;
				case "Center": if (XExpression != GetXCenter(WidthExpression)) return false; break;
				case "Right": if (XExpression != GetXRight(WidthExpression)) return false; break;
			}
			return true;
		}

		void OnApplyAspectRatio(object sender, RoutedEventArgs e)
		{
			WidthExpression = GetRatioWidth(AspectRatio);
			HeightExpression = GetRatioHeight(AspectRatio);
		}

		void OnResetClick(object sender = null, RoutedEventArgs e = null)
		{
			WidthExpression = "width";
			HeightExpression = "height";
			XExpression = GetXCenter(WidthExpression);
			YExpression = GetYCenter(HeightExpression);
			AspectRatio = "5/7";
		}

		void ExpressionHelp(object sender, RoutedEventArgs e) => ExpressionHelpDialog.Display(Variables);

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result { XExpression = XExpression, YExpression = YExpression, WidthExpression = WidthExpression, HeightExpression = HeightExpression };
			xExpression.AddCurrentSuggestion();
			yExpression.AddCurrentSuggestion();
			widthExpression.AddCurrentSuggestion();
			heightExpression.AddCurrentSuggestion();
			DialogResult = true;
		}

		public static Result Run(Window parent, NEVariables variables)
		{
			var dialog = new ImageCropDialog(variables) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}

	internal class CropImageToggleButtonConverter : MarkupExtension, IMultiValueConverter
	{
		public override object ProvideValue(IServiceProvider serviceProvider) => this;
		public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) => (values[0] as ImageCropDialog).IsChecked(parameter as string);
		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) { throw new NotImplementedException(); }
	}
}
