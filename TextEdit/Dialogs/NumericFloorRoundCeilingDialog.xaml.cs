using System.Windows;
using NeoEdit.Common.Expressions;
using NeoEdit.GUI.Controls;

namespace NeoEdit.TextEdit.Dialogs
{
	internal partial class NumericFloorRoundCeilingDialog
	{
		internal class Result
		{
			public string Interval { get; set; }
		}

		[DepProp]
		public string Interval { get { return UIHelper<NumericFloorRoundCeilingDialog>.GetPropValue<string>(this); } set { UIHelper<NumericFloorRoundCeilingDialog>.SetPropValue(this, value); } }
		public NEVariables Variables { get; }

		static NumericFloorRoundCeilingDialog() { UIHelper<NumericFloorRoundCeilingDialog>.Register(); }

		NumericFloorRoundCeilingDialog(NEVariables variables)
		{
			Variables = variables;
			InitializeComponent();
			Interval = "1";
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result { Interval = Interval };
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
