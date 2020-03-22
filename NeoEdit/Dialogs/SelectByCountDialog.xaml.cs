using System;
using System.Windows;
using NeoEdit.Program.Controls;
using NeoEdit.Program.Models;

namespace NeoEdit.Program.Dialogs
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

		SelectByCountDialogResult result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if (((!MinCount.HasValue) && (!MaxCount.HasValue)) || (MaxCount < MinCount))
				return;
			result = new SelectByCountDialogResult { MinCount = MinCount, MaxCount = MaxCount };
			DialogResult = true;
		}

		public static SelectByCountDialogResult Run(Window parent)
		{
			var dialog = new SelectByCountDialog { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
