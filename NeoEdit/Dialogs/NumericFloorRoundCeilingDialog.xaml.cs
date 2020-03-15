using System;
using System.Windows;
using NeoEdit.Program.Controls;
using NeoEdit.Program.Expressions;

namespace NeoEdit.Program.Dialogs
{
	partial class NumericFloorRoundCeilingDialog
	{
		public class Result
		{
			public string BaseValue { get; set; }
			public string Interval { get; set; }
		}

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

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result { BaseValue = BaseValue, Interval = Interval };
			baseValue.AddCurrentSuggestion();
			interval.AddCurrentSuggestion();
			DialogResult = true;
		}

		static public Result Run(Window parent, string title, NEVariables variables)
		{
			var dialog = new NumericFloorRoundCeilingDialog(title, variables) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
