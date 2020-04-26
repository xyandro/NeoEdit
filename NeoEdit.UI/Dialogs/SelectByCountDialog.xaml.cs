using System;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class SelectByCountDialog
	{
		[DepProp]
		public int? MinCount { get { return UIHelper<SelectByCountDialog>.GetPropValue<int?>(this); } set { UIHelper<SelectByCountDialog>.SetPropValue(this, value); } }
		[DepProp]
		public int? MaxCount { get { return UIHelper<SelectByCountDialog>.GetPropValue<int?>(this); } set { UIHelper<SelectByCountDialog>.SetPropValue(this, value); } }

		static SelectByCountDialog() { UIHelper<SelectByCountDialog>.Register(); }

		SelectByCountDialog()
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
			var dialog = new SelectByCountDialog { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
