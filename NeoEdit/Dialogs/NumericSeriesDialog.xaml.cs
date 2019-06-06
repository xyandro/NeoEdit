using System.Windows;
using NeoEdit.Controls;
using NeoEdit.Expressions;

namespace NeoEdit.Dialogs
{
	partial class NumericSeriesDialog
	{
		public class Result
		{
			public string StartExpression { get; set; }
			public string IncrementExpression { get; set; }
		}

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

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result { StartExpression = StartExpression, IncrementExpression = IncrementExpression };
			DialogResult = true;
		}

		static public Result Run(Window parent, bool linear, NEVariables variables)
		{
			var dialog = new NumericSeriesDialog(linear, variables) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
