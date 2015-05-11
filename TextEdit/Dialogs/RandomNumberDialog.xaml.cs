using System;
using System.Windows;
using NeoEdit.GUI.Common;

namespace NeoEdit.TextEdit.Dialogs
{
	internal partial class RandomNumberDialog
	{
		internal class Result
		{
			public int MinValue { get; set; }
			public int MaxValue { get; set; }
		}

		[DepProp]
		public int MinValue { get { return UIHelper<RandomNumberDialog>.GetPropValue<int>(this); } set { UIHelper<RandomNumberDialog>.SetPropValue(this, value); } }
		[DepProp]
		public int MaxValue { get { return UIHelper<RandomNumberDialog>.GetPropValue<int>(this); } set { UIHelper<RandomNumberDialog>.SetPropValue(this, value); } }

		static RandomNumberDialog() { UIHelper<RandomNumberDialog>.Register(); }

		RandomNumberDialog()
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
			var dialog = new RandomNumberDialog { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
