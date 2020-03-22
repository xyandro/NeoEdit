﻿using System;
using System.Windows;
using NeoEdit.Program.Controls;
using NeoEdit.Program.Expressions;
using NeoEdit.Program.Models;

namespace NeoEdit.Program.Dialogs
{
	partial class NumericScaleDialog
	{
		[DepProp]
		public string PrevMin { get { return UIHelper<NumericScaleDialog>.GetPropValue<string>(this); } set { UIHelper<NumericScaleDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string PrevMax { get { return UIHelper<NumericScaleDialog>.GetPropValue<string>(this); } set { UIHelper<NumericScaleDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string NewMin { get { return UIHelper<NumericScaleDialog>.GetPropValue<string>(this); } set { UIHelper<NumericScaleDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string NewMax { get { return UIHelper<NumericScaleDialog>.GetPropValue<string>(this); } set { UIHelper<NumericScaleDialog>.SetPropValue(this, value); } }

		public NEVariables Variables { get; }

		static NumericScaleDialog() { UIHelper<NumericScaleDialog>.Register(); }

		NumericScaleDialog(NEVariables variables)
		{
			Variables = variables;
			InitializeComponent();
			PrevMin = NewMin = "xmin";
			PrevMax = NewMax = "xmax";
		}

		NumericScaleDialogResult result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new NumericScaleDialogResult { PrevMin = PrevMin, PrevMax = PrevMax, NewMin = NewMin, NewMax = NewMax };
			DialogResult = true;
		}

		public static NumericScaleDialogResult Run(Window parent, NEVariables variables)
		{
			var dialog = new NumericScaleDialog(variables) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
