using System.Windows;
using NeoEdit.GUI.Controls;

namespace NeoEdit.TextEdit.Dialogs
{
	internal partial class FloorRoundCeilingDialog
	{
		internal class Result
		{
			public decimal Interval { get; set; }
		}

		[DepProp]
		public decimal Interval { get { return UIHelper<FloorRoundCeilingDialog>.GetPropValue<decimal>(this); } set { UIHelper<FloorRoundCeilingDialog>.SetPropValue(this, value); } }

		static FloorRoundCeilingDialog() { UIHelper<FloorRoundCeilingDialog>.Register(); }

		FloorRoundCeilingDialog()
		{
			InitializeComponent();
			Interval = 1;
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result { Interval = Interval };
			DialogResult = true;
		}

		static public Result Run(Window parent)
		{
			var dialog = new FloorRoundCeilingDialog() { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
