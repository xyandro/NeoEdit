﻿using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Expressions;
using NeoEdit.Common.Transform;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Image_Crop_Dialog
	{
		[DepProp]
		public string XExpression { get { return UIHelper<Image_Crop_Dialog>.GetPropValue<string>(this); } set { UIHelper<Image_Crop_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public string YExpression { get { return UIHelper<Image_Crop_Dialog>.GetPropValue<string>(this); } set { UIHelper<Image_Crop_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public string WidthExpression { get { return UIHelper<Image_Crop_Dialog>.GetPropValue<string>(this); } set { UIHelper<Image_Crop_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public string HeightExpression { get { return UIHelper<Image_Crop_Dialog>.GetPropValue<string>(this); } set { UIHelper<Image_Crop_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public string AspectRatio { get { return UIHelper<Image_Crop_Dialog>.GetPropValue<string>(this); } set { UIHelper<Image_Crop_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public string FillColor { get { return UIHelper<Image_Crop_Dialog>.GetPropValue<string>(this); } set { UIHelper<Image_Crop_Dialog>.SetPropValue(this, value); } }
		public NEVariables Variables { get; }

		static Image_Crop_Dialog()
		{
			UIHelper<Image_Crop_Dialog>.Register();
			UIHelper<Image_Crop_Dialog>.AddCallback(a => a.WidthExpression, (obj, o, n) => obj.SetWidth(o));
			UIHelper<Image_Crop_Dialog>.AddCallback(a => a.HeightExpression, (obj, o, n) => obj.SetHeight(o));
			UIHelper<Image_Crop_Dialog>.AddCallback(a => a.AspectRatio, (obj, o, n) => obj.SetAspectRatio(o));
		}

		Image_Crop_Dialog(NEVariables variables)
		{
			Variables = variables;

			InitializeComponent();

			OnResetClick();
		}

		bool controlDown => Keyboard.Modifiers.HasFlag(ModifierKeys.Control);

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
			SetAnchor(anchor[0], anchor[1]);
		}

		void SetAnchor(string vertAnchor, string horizAnchor)
		{
			switch (vertAnchor)
			{
				case "Top": YExpression = GetYTop(HeightExpression); break;
				case "Center": YExpression = GetYCenter(HeightExpression); break;
				case "Bottom": YExpression = GetYBottom(HeightExpression); break;
			}
			switch (horizAnchor)
			{
				case "Left": XExpression = GetXLeft(WidthExpression); break;
				case "Center": XExpression = GetXCenter(WidthExpression); break;
				case "Right": XExpression = GetXRight(WidthExpression); break;
			}
		}

		protected override void OnPreviewKeyDown(KeyEventArgs e)
		{
			if (controlDown)
			{
				e.Handled = true;
				switch (e.Key)
				{
					case Key.NumPad7: case Key.Home: SetAnchor("Top", "Left"); break;
					case Key.NumPad8: case Key.Up: SetAnchor("Top", "Center"); break;
					case Key.NumPad9: case Key.PageUp: SetAnchor("Top", "Right"); break;
					case Key.NumPad4: case Key.Left: SetAnchor("Center", "Left"); break;
					case Key.NumPad5: case Key.Clear: SetAnchor("Center", "Center"); break;
					case Key.NumPad6: case Key.Right: SetAnchor("Center", "Right"); break;
					case Key.NumPad1: case Key.End: SetAnchor("Bottom", "Left"); break;
					case Key.NumPad2: case Key.Down: SetAnchor("Bottom", "Center"); break;
					case Key.NumPad3: case Key.PageDown: SetAnchor("Bottom", "Right"); break;
					default: e.Handled = false; break;
				}
			}
			base.OnPreviewKeyDown(e);
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
			AspectRatio = "width/height";
			FillColor = "00000000";
			xExpression.SelectAll();
			yExpression.SelectAll();
			widthExpression.SelectAll();
			heightExpression.SelectAll();
			aspectRatio.SelectAll();
			fillColor.SelectAll();
		}

		void ExpressionHelp(object sender, RoutedEventArgs e) => ExpressionHelpDialog.Display(Variables);

		Configuration_Image_Crop result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Configuration_Image_Crop { XExpression = XExpression, YExpression = YExpression, WidthExpression = WidthExpression, HeightExpression = HeightExpression, FillColor = Colorer.StringToString(FillColor) };
			xExpression.AddCurrentSuggestion();
			yExpression.AddCurrentSuggestion();
			widthExpression.AddCurrentSuggestion();
			heightExpression.AddCurrentSuggestion();
			aspectRatio.AddCurrentSuggestion();
			fillColor.AddCurrentSuggestion();
			DialogResult = true;
		}

		public static Configuration_Image_Crop Run(Window parent, NEVariables variables)
		{
			var dialog = new Image_Crop_Dialog(variables) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}

	internal class CropImageToggleButtonConverter : MarkupExtension, IMultiValueConverter
	{
		public override object ProvideValue(IServiceProvider serviceProvider) => this;
		public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) => (values[0] as Image_Crop_Dialog).IsChecked(parameter as string);
		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) { throw new NotImplementedException(); }
	}
}
