using System;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Expressions;
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

		Configuration_Numeric_Floor result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Configuration_Numeric_Floor { BaseValue = BaseValue, Interval = Interval };
			baseValue.AddCurrentSuggestion();
			interval.AddCurrentSuggestion();
			DialogResult = true;
		}

		public static Configuration_Numeric_Floor Run(Window parent, string title, NEVariables variables)
		{
			var dialog = new NumericFloorRoundCeilingDialog(title, variables) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
