using System;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Configure_Select_Repeats_ByCount_Dialog
	{
		[DepProp]
		public int? MinCount { get { return UIHelper<Configure_Select_Repeats_ByCount_Dialog>.GetPropValue<int?>(this); } set { UIHelper<Configure_Select_Repeats_ByCount_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public int? MaxCount { get { return UIHelper<Configure_Select_Repeats_ByCount_Dialog>.GetPropValue<int?>(this); } set { UIHelper<Configure_Select_Repeats_ByCount_Dialog>.SetPropValue(this, value); } }

		static Configure_Select_Repeats_ByCount_Dialog() { UIHelper<Configure_Select_Repeats_ByCount_Dialog>.Register(); }

		Configure_Select_Repeats_ByCount_Dialog()
		{
			InitializeComponent();
			MinCount = 2;
			MaxCount = null;
		}

		Configuration_Select_Repeats_ByCount result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if (((!MinCount.HasValue) && (!MaxCount.HasValue)) || (MaxCount < MinCount))
				return;
			result = new Configuration_Select_Repeats_ByCount { MinCount = MinCount, MaxCount = MaxCount };
			DialogResult = true;
		}

		public static Configuration_Select_Repeats_ByCount Run(Window parent)
		{
			var dialog = new Configure_Select_Repeats_ByCount_Dialog { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
