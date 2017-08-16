﻿using System.Windows;
using NeoEdit.GUI.Controls;

namespace NeoEdit.TextEdit.Dialogs
{
	partial class DiffFixWhitespaceDialog
	{
		internal class Result
		{
			public int LineStartTabStop { get; set; }
		}

		[DepProp]
		public int LineStartTabStop { get { return UIHelper<DiffFixWhitespaceDialog>.GetPropValue<int>(this); } set { UIHelper<DiffFixWhitespaceDialog>.SetPropValue(this, value); } }

		static DiffFixWhitespaceDialog() { UIHelper<DiffFixWhitespaceDialog>.Register(); }

		DiffFixWhitespaceDialog()
		{
			InitializeComponent();
			LineStartTabStop = 4;
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result { LineStartTabStop = LineStartTabStop };
			DialogResult = true;
		}

		public static Result Run(Window parent)
		{
			var dialog = new DiffFixWhitespaceDialog() { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}