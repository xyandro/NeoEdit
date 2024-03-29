﻿using System;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Expressions;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Image_Rotate_Dialog
	{
		[DepProp]
		public string AngleExpression { get { return UIHelper<Image_Rotate_Dialog>.GetPropValue<string>(this); } set { UIHelper<Image_Rotate_Dialog>.SetPropValue(this, value); } }
		public NEVariables Variables { get; }

		static Image_Rotate_Dialog() { UIHelper<Image_Rotate_Dialog>.Register(); }

		Image_Rotate_Dialog(NEVariables variables)
		{
			Variables = variables;

			InitializeComponent();

			AngleExpression = "0 deg";
		}

		void ExpressionHelp(object sender, RoutedEventArgs e) => ExpressionHelpDialog.Display(Variables);

		Configuration_Image_Rotate result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Configuration_Image_Rotate { AngleExpression = AngleExpression };
			angleExpression.AddCurrentSuggestion();
			DialogResult = true;
		}

		public static Configuration_Image_Rotate Run(Window parent, NEVariables variables)
		{
			var dialog = new Image_Rotate_Dialog(variables) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
