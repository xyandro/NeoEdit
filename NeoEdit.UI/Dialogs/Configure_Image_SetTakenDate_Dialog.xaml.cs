﻿using System;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Expressions;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Configure_Image_SetTakenDate_Dialog
	{
		[DepProp]
		public string FileName { get { return UIHelper<Configure_Image_SetTakenDate_Dialog>.GetPropValue<string>(this); } set { UIHelper<Configure_Image_SetTakenDate_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public string DateTime { get { return UIHelper<Configure_Image_SetTakenDate_Dialog>.GetPropValue<string>(this); } set { UIHelper<Configure_Image_SetTakenDate_Dialog>.SetPropValue(this, value); } }

		public NEVariables Variables { get; }

		static Configure_Image_SetTakenDate_Dialog() { UIHelper<Configure_Image_SetTakenDate_Dialog>.Register(); }

		Configure_Image_SetTakenDate_Dialog(NEVariables variables)
		{
			Variables = variables;
			InitializeComponent();

			FileName = "r1";
			DateTime = "x";
		}

		private void ExpressionHelp(object sender, RoutedEventArgs e) => ExpressionHelpDialog.Display(Variables);

		Configuration_Image_SetTakenDate result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Configuration_Image_SetTakenDate { FileName = FileName, DateTime = DateTime };
			fileName.AddCurrentSuggestion();
			dateTime.AddCurrentSuggestion();
			DialogResult = true;
		}

		public static Configuration_Image_SetTakenDate Run(Window parent, NEVariables variables)
		{
			var dialog = new Configure_Image_SetTakenDate_Dialog(variables) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
