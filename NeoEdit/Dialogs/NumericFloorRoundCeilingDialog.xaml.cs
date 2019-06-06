using System.Windows;
using NeoEdit.Controls;
using NeoEdit.Expressions;

namespace NeoEdit.Dialogs
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

		NumericFloorRoundCeilingDialog(NEVariables variables)
		{
			Variables = variables;
			InitializeComponent();
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

		static public Result Run(Window parent, NEVariables variables)
		{
			var dialog = new NumericFloorRoundCeilingDialog(variables) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
