using System;
using System.Windows;
using NeoEdit.GUI.Controls;

namespace NeoEdit.TextEdit.Dialogs
{
	internal partial class NumericRandomNumberDialog
	{
		internal class Result
		{
			public int MinValue { get; set; }
			public int MaxValue { get; set; }
		}

		[DepProp]
		public int MinValue { get { return UIHelper<NumericRandomNumberDialog>.GetPropValue<int>(this); } set { UIHelper<NumericRandomNumberDialog>.SetPropValue(this, value); } }
		[DepProp]
		public int MaxValue { get { return UIHelper<NumericRandomNumberDialog>.GetPropValue<int>(this); } set { UIHelper<NumericRandomNumberDialog>.SetPropValue(this, value); } }

		static NumericRandomNumberDialog() { UIHelper<NumericRandomNumberDialog>.Register(); }

		NumericRandomNumberDialog()
		{
			InitializeComponent();

			MinValue = 1;
			MaxValue = 1000;
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result { MinValue = Math.Min(MinValue, MaxValue), MaxValue = Math.Max(MinValue, MaxValue) };
			DialogResult = true;
		}

		static public Result Run(Window parent)
		{
			var dialog = new NumericRandomNumberDialog { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
