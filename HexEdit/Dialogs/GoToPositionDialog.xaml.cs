using System;
using System.Windows;
using NeoEdit.GUI.Common;

namespace NeoEdit.HexEdit.Dialogs
{
	partial class GoToPositionDialog
	{
		[DepProp]
		public long MinValue { get { return UIHelper<GoToPositionDialog>.GetPropValue<long>(this); } set { UIHelper<GoToPositionDialog>.SetPropValue(this, value); } }
		[DepProp]
		public long MaxValue { get { return UIHelper<GoToPositionDialog>.GetPropValue<long>(this); } set { UIHelper<GoToPositionDialog>.SetPropValue(this, value); } }
		[DepProp]
		public long Value { get { return UIHelper<GoToPositionDialog>.GetPropValue<long>(this); } set { UIHelper<GoToPositionDialog>.SetPropValue(this, value); } }

		static GoToPositionDialog()
		{
			UIHelper<GoToPositionDialog>.Register();
			UIHelper<GoToPositionDialog>.AddCoerce(a => a.Value, (obj, val) => Math.Max(obj.MinValue, Math.Min(val, obj.MaxValue)));
		}

		GoToPositionDialog(long minValue, long maxValue, long value)
		{
			InitializeComponent();
			MinValue = minValue;
			MaxValue = maxValue;
			Value = value;
		}

		void OkClick(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
		}

		public static long? Run(long minValue, long maxValue, long value)
		{
			var dialog = new GoToPositionDialog(minValue, maxValue, value);
			return dialog.ShowDialog() == true ? (long?)dialog.Value : null;
		}
	}
}
