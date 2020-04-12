﻿using System;
using System.Windows;
using NeoEdit.Common.Expressions;
using NeoEdit.Common.Models;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class NumericSeriesDialog
	{
		[DepProp]
		public string StartExpression { get { return UIHelper<NumericSeriesDialog>.GetPropValue<string>(this); } set { UIHelper<NumericSeriesDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string IncrementExpression { get { return UIHelper<NumericSeriesDialog>.GetPropValue<string>(this); } set { UIHelper<NumericSeriesDialog>.SetPropValue(this, value); } }

		public NEVariables Variables { get; }

		static NumericSeriesDialog() { UIHelper<NumericSeriesDialog>.Register(); }

		NumericSeriesDialog(bool linear, NEVariables variables)
		{
			Variables = variables;

			InitializeComponent();
			if (linear)
			{
				StartExpression = "linestart";
				IncrementExpression = "lineincrement";
			}
			else
			{
				StartExpression = "geostart";
				IncrementExpression = "geoincrement";
				Title = "Geometric Series";
				incrementLabel.Content = "_Multiplier";
			}
		}

		NumericSeriesDialogResult result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new NumericSeriesDialogResult { StartExpression = StartExpression, IncrementExpression = IncrementExpression };
			DialogResult = true;
		}

		public static NumericSeriesDialogResult Run(Window parent, bool linear, NEVariables variables)
		{
			var dialog = new NumericSeriesDialog(linear, variables) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}