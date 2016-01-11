using System.Windows;
using NeoEdit.GUI.Controls;

namespace NeoEdit.TextEdit.Dialogs
{
	internal partial class NumericSeriesDialog
	{
		internal class Result
		{
			public double Start { get; set; }
			public double Multiplier { get; set; }
		}

		[DepProp]
		public double Start { get { return UIHelper<NumericSeriesDialog>.GetPropValue<double>(this); } set { UIHelper<NumericSeriesDialog>.SetPropValue(this, value); } }
		[DepProp]
		public double Multiplier { get { return UIHelper<NumericSeriesDialog>.GetPropValue<double>(this); } set { UIHelper<NumericSeriesDialog>.SetPropValue(this, value); } }

		static NumericSeriesDialog() { UIHelper<NumericSeriesDialog>.Register(); }

		NumericSeriesDialog(double start, double multiplier)
		{
			InitializeComponent();
			Start = start;
			Multiplier = multiplier;
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result { Start = Start, Multiplier = Multiplier };
			DialogResult = true;
		}

		static public Result Run(Window parent, double start, double multiplier)
		{
			var dialog = new NumericSeriesDialog(start, multiplier) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
