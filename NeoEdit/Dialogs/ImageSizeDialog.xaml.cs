﻿using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows;
using NeoEdit.Program;
using NeoEdit.Program.Controls;
using NeoEdit.Program.Expressions;

namespace NeoEdit.Program.Dialogs
{
	partial class ImageSizeDialog
	{
		public class Result
		{
			public string WidthExpression { get; set; }
			public string HeightExpression { get; set; }
			public InterpolationMode InterpolationMode { get; set; }
		}

		[DepProp]
		public string WidthExpression { get { return UIHelper<ImageSizeDialog>.GetPropValue<string>(this); } set { UIHelper<ImageSizeDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string HeightExpression { get { return UIHelper<ImageSizeDialog>.GetPropValue<string>(this); } set { UIHelper<ImageSizeDialog>.SetPropValue(this, value); } }
		[DepProp]
		public InterpolationMode InterpolationMode { get { return UIHelper<ImageSizeDialog>.GetPropValue<InterpolationMode>(this); } set { UIHelper<ImageSizeDialog>.SetPropValue(this, value); } }
		public NEVariables Variables { get; }
		public List<InterpolationMode> InterpolationModes { get; }

		static ImageSizeDialog()
		{
			UIHelper<ImageSizeDialog>.Register();
			UIHelper<ImageSizeDialog>.AddCallback(a => a.WidthExpression, (obj, o, n) => obj.SetWidth(o));
			UIHelper<ImageSizeDialog>.AddCallback(a => a.HeightExpression, (obj, o, n) => obj.SetHeight(o));
		}

		ImageSizeDialog(NEVariables variables)
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

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result { WidthExpression = WidthExpression, HeightExpression = HeightExpression, InterpolationMode = InterpolationMode };
			widthExpression.AddCurrentSuggestion();
			heightExpression.AddCurrentSuggestion();
			DialogResult = true;
		}

		public static Result Run(Window parent, NEVariables variables)
		{
			var dialog = new ImageSizeDialog(variables) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
