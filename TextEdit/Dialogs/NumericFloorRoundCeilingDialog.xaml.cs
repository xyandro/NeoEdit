using System.Windows;
using NeoEdit.GUI.Controls;

namespace NeoEdit.TextEdit.Dialogs
{
	internal partial class NumericFloorRoundCeilingDialog
	{
		internal class Result
		{
			public double Interval { get; set; }
		}

		[DepProp]
		public string Interval { get { return UIHelper<NumericFloorRoundCeilingDialog>.GetPropValue<string>(this); } set { UIHelper<NumericFloorRoundCeilingDialog>.SetPropValue(this, value); } }

		static NumericFloorRoundCeilingDialog() { UIHelper<NumericFloorRoundCeilingDialog>.Register(); }

		NumericFloorRoundCeilingDialog()
		{
			InitializeComponent();
			Interval = "1";
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result { Interval = double.Parse(Interval) };
			DialogResult = true;
		}

		static public Result Run(Window parent)
		{
			var dialog = new NumericFloorRoundCeilingDialog() { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
