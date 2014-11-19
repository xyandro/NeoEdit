﻿using System.Windows;
using NeoEdit.GUI.Common;
using NeoEdit.GUI.Dialogs;

namespace NeoEdit.TextEditor.Dialogs
{
	internal partial class LimitDialog
	{
		internal class Result : IDialogResult
		{
			public int SelMult { get; set; }
			public bool IgnoreBlank { get; set; }
			public int NumSels { get; set; }
		}

		[DepProp]
		public int SelMult { get { return UIHelper<LimitDialog>.GetPropValue<int>(this); } set { UIHelper<LimitDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool IgnoreBlank { get { return UIHelper<LimitDialog>.GetPropValue<bool>(this); } set { UIHelper<LimitDialog>.SetPropValue(this, value); } }
		[DepProp]
		public int NumSels { get { return UIHelper<LimitDialog>.GetPropValue<int>(this); } set { UIHelper<LimitDialog>.SetPropValue(this, value); } }
		[DepProp]
		public int MaxSels { get { return UIHelper<LimitDialog>.GetPropValue<int>(this); } set { UIHelper<LimitDialog>.SetPropValue(this, value); } }

		static LimitDialog() { UIHelper<LimitDialog>.Register(); }

		LimitDialog(int maxSels)
		{
			InitializeComponent();

			NumSels = MaxSels = maxSels;
			SelMult = 1;
			IgnoreBlank = false;
		}

		Result response = null;
		void OkClick(object sender, RoutedEventArgs e)
		{
			response = new Result { SelMult = SelMult, IgnoreBlank = IgnoreBlank, NumSels = NumSels };
			DialogResult = true;
		}

		public static Result Run(int numSels)
		{
			var dialog = new LimitDialog(numSels);
			if (dialog.ShowDialog() != true)
				return null;

			return dialog.response;
		}
	}
}
