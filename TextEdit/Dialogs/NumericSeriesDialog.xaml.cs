using System.Windows;
using NeoEdit.GUI.Controls;

namespace NeoEdit.TextEdit.Dialogs
{
	internal partial class NumericSeriesDialog
	{
		internal class Result
		{
			public double Start { get; set; }
			public double Increment { get; set; }
		}

		[DepProp]
		public double Start { get { return UIHelper<NumericSeriesDialog>.GetPropValue<double>(this); } set { UIHelper<NumericSeriesDialog>.SetPropValue(this, value); } }
		[DepProp]
		public double Increment { get { return UIHelper<NumericSeriesDialog>.GetPropValue<double>(this); } set { UIHelper<NumericSeriesDialog>.SetPropValue(this, value); } }

		static NumericSeriesDialog() { UIHelper<NumericSeriesDialog>.Register(); }

		NumericSeriesDialog(bool linear, double start, double increment)
		{
			InitializeComponent();
			if (!linear)
			{
				Title = "Geometric Series";
				incrementLabel.Content = "_Multiplier";
			}
			Start = start;
			Increment = increment;
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result { Start = Start, Increment = Increment };
			DialogResult = true;
		}

		static public Result Run(Window parent, bool linear, double start, double increment)
		{
			var dialog = new NumericSeriesDialog(linear, start, increment) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
