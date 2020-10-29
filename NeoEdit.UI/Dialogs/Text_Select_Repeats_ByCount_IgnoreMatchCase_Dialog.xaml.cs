using System;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Text_Select_Repeats_ByCount_IgnoreMatchCase_Dialog
	{
		[DepProp]
		public int? MinCount { get { return UIHelper<Text_Select_Repeats_ByCount_IgnoreMatchCase_Dialog>.GetPropValue<int?>(this); } set { UIHelper<Text_Select_Repeats_ByCount_IgnoreMatchCase_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public int? MaxCount { get { return UIHelper<Text_Select_Repeats_ByCount_IgnoreMatchCase_Dialog>.GetPropValue<int?>(this); } set { UIHelper<Text_Select_Repeats_ByCount_IgnoreMatchCase_Dialog>.SetPropValue(this, value); } }

		static Text_Select_Repeats_ByCount_IgnoreMatchCase_Dialog() { UIHelper<Text_Select_Repeats_ByCount_IgnoreMatchCase_Dialog>.Register(); }

		Text_Select_Repeats_ByCount_IgnoreMatchCase_Dialog()
		{
			InitializeComponent();
			MinCount = 2;
			MaxCount = null;
		}

		Configuration_Text_Select_Repeats_ByCount_IgnoreMatchCase result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if (((!MinCount.HasValue) && (!MaxCount.HasValue)) || (MaxCount < MinCount))
				return;
			result = new Configuration_Text_Select_Repeats_ByCount_IgnoreMatchCase { MinCount = MinCount, MaxCount = MaxCount };
			DialogResult = true;
		}

		public static Configuration_Text_Select_Repeats_ByCount_IgnoreMatchCase Run(Window parent)
		{
			var dialog = new Text_Select_Repeats_ByCount_IgnoreMatchCase_Dialog { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
