﻿using System.Windows;
using NeoEdit.Program.Controls;

namespace NeoEdit.Program.Dialogs
{
	partial class SelectByCountDialog
	{
		public class Result
		{
			public int? MinCount { get; set; }
			public int? MaxCount { get; set; }
		}

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

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if (((!MinCount.HasValue) && (!MaxCount.HasValue)) || (MaxCount < MinCount))
				return;
			result = new Result { MinCount = MinCount, MaxCount = MaxCount };
			DialogResult = true;
		}

		public static Result Run(Window parent)
		{
			var dialog = new SelectByCountDialog { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
