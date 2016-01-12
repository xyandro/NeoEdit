using System.Windows;
using NeoEdit.GUI.Controls;

namespace NeoEdit.TextEdit.Dialogs
{
	internal partial class ScaleDialog
	{
		internal class Result
		{
			public double PrevMin { get; set; }
			public double PrevMax { get; set; }
			public double NewMin { get; set; }
			public double NewMax { get; set; }
		}

		[DepProp]
		public double PrevMin { get { return UIHelper<ScaleDialog>.GetPropValue<double>(this); } set { UIHelper<ScaleDialog>.SetPropValue(this, value); } }
		[DepProp]
		public double PrevMax { get { return UIHelper<ScaleDialog>.GetPropValue<double>(this); } set { UIHelper<ScaleDialog>.SetPropValue(this, value); } }
		[DepProp]
		public double NewMin { get { return UIHelper<ScaleDialog>.GetPropValue<double>(this); } set { UIHelper<ScaleDialog>.SetPropValue(this, value); } }
		[DepProp]
		public double NewMax { get { return UIHelper<ScaleDialog>.GetPropValue<double>(this); } set { UIHelper<ScaleDialog>.SetPropValue(this, value); } }

		static ScaleDialog() { UIHelper<ScaleDialog>.Register(); }

		ScaleDialog()
		{
			InitializeComponent();
			PrevMin = NewMin = 1;
			PrevMax = NewMax = 10;
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result { PrevMin = PrevMin, PrevMax = PrevMax, NewMin = NewMin, NewMax = NewMax };
			DialogResult = true;
		}

		static public Result Run(Window parent)
		{
			var dialog = new ScaleDialog() { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
