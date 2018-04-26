﻿using System.Windows;
using NeoEdit.Common.Expressions;
using NeoEdit.GUI.Controls;

namespace NeoEdit.TextEdit.Dialogs
{
	internal partial class NumericRandomNumberDialog
	{
		internal class Result
		{
			public string MinValue { get; set; }
			public string MaxValue { get; set; }
		}

		[DepProp]
		public string MinValue { get { return UIHelper<NumericRandomNumberDialog>.GetPropValue<string>(this); } set { UIHelper<NumericRandomNumberDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string MaxValue { get { return UIHelper<NumericRandomNumberDialog>.GetPropValue<string>(this); } set { UIHelper<NumericRandomNumberDialog>.SetPropValue(this, value); } }
		public NEVariables Variables { get; }

		static NumericRandomNumberDialog() { UIHelper<NumericRandomNumberDialog>.Register(); }

		NumericRandomNumberDialog(NEVariables variables)
		{
			Variables = variables;
			InitializeComponent();

			MinValue = "1";
			MaxValue = "1000";
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result { MinValue = MinValue, MaxValue = MaxValue };
			DialogResult = true;
		}

		static public Result Run(Window parent, NEVariables variables)
		{
			var dialog = new NumericRandomNumberDialog(variables) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
