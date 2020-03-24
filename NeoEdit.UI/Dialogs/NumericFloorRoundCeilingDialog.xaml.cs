using System;
using System.Windows;
using NeoEdit.Common.Expressions;
using NeoEdit.Common.Models;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class NumericFloorRoundCeilingDialog
	{
		[DepProp]
		public string BaseValue { get { return UIHelper<NumericFloorRoundCeilingDialog>.GetPropValue<string>(this); } set { UIHelper<NumericFloorRoundCeilingDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string Interval { get { return UIHelper<NumericFloorRoundCeilingDialog>.GetPropValue<string>(this); } set { UIHelper<NumericFloorRoundCeilingDialog>.SetPropValue(this, value); } }
		public NEVariables Variables { get; }

		static NumericFloorRoundCeilingDialog() { UIHelper<NumericFloorRoundCeilingDialog>.Register(); }

		NumericFloorRoundCeilingDialog(string title, NEVariables variables)
		{
			Variables = variables;
			InitializeComponent();
			Title = title;
			BaseValue = "0";
			Interval = "1";
		}

		NumericFloorRoundCeilingDialogResult result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new NumericFloorRoundCeilingDialogResult { BaseValue = BaseValue, Interval = Interval };
			baseValue.AddCurrentSuggestion();
			interval.AddCurrentSuggestion();
			DialogResult = true;
		}

		public static NumericFloorRoundCeilingDialogResult Run(Window parent, string title, NEVariables variables)
		{
			var dialog = new NumericFloorRoundCeilingDialog(title, variables) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
