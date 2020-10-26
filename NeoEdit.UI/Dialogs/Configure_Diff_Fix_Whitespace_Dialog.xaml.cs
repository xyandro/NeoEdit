﻿using System;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Configure_Diff_Fix_Whitespace_Dialog
	{
		[DepProp]
		public int LineStartTabStop { get { return UIHelper<Configure_Diff_Fix_Whitespace_Dialog>.GetPropValue<int>(this); } set { UIHelper<Configure_Diff_Fix_Whitespace_Dialog>.SetPropValue(this, value); } }

		static Configure_Diff_Fix_Whitespace_Dialog() { UIHelper<Configure_Diff_Fix_Whitespace_Dialog>.Register(); }

		Configure_Diff_Fix_Whitespace_Dialog()
		{
			InitializeComponent();
			LineStartTabStop = 4;
		}

		Configuration_Diff_Fix_Whitespace result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Configuration_Diff_Fix_Whitespace { LineStartTabStop = LineStartTabStop };
			DialogResult = true;
		}

		public static Configuration_Diff_Fix_Whitespace Run(Window parent)
		{
			var dialog = new Configure_Diff_Fix_Whitespace_Dialog() { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
