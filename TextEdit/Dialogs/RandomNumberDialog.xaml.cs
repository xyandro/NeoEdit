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
		public int MinValue { get { return UIHelper<RandomNumberDialog>.GetPropValue(() => this.MinValue); } set { UIHelper<RandomNumberDialog>.SetPropValue(() => this.MinValue, value); } }
		[DepProp]
		public int MaxValue { get { return UIHelper<RandomNumberDialog>.GetPropValue(() => this.MaxValue); } set { UIHelper<RandomNumberDialog>.SetPropValue(() => this.MaxValue, value); } }

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

		static public Result Run()
		{
			var dialog = new RandomNumberDialog();
			return dialog.ShowDialog() == true ? dialog.result : null;
		}
	}
}
