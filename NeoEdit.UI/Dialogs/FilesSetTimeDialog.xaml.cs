﻿using System;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Expressions;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class FilesSetTimeDialog
	{
		[DepProp]
		public NEVariables Variables { get { return UIHelper<FilesSetTimeDialog>.GetPropValue<NEVariables>(this); } set { UIHelper<FilesSetTimeDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string Expression { get { return UIHelper<FilesSetTimeDialog>.GetPropValue<string>(this); } set { UIHelper<FilesSetTimeDialog>.SetPropValue(this, value); } }

		static FilesSetTimeDialog() { UIHelper<FilesSetTimeDialog>.Register(); }

		FilesSetTimeDialog(NEVariables variables, string expression)
		{
			InitializeComponent();

			Variables = variables;
			Expression = expression;
		}

		Configuration_Files_Set_Time result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Configuration_Files_Set_Time { Expression = Expression };
			DialogResult = true;
		}

		public static Configuration_Files_Set_Time Run(Window parent, NEVariables variables, string expression)
		{
			var dialog = new FilesSetTimeDialog(variables, expression) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
