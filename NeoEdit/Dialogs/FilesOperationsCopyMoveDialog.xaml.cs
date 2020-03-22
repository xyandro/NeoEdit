﻿using System;
using System.Windows;
using NeoEdit.Program.Controls;
using NeoEdit.Program.Expressions;
using NeoEdit.Program.Models;

namespace NeoEdit.Program.Dialogs
{
	partial class FilesOperationsCopyMoveDialog
	{
		[DepProp]
		public string OldFileName { get { return UIHelper<FilesOperationsCopyMoveDialog>.GetPropValue<string>(this); } set { UIHelper<FilesOperationsCopyMoveDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string NewFileName { get { return UIHelper<FilesOperationsCopyMoveDialog>.GetPropValue<string>(this); } set { UIHelper<FilesOperationsCopyMoveDialog>.SetPropValue(this, value); } }

		public NEVariables Variables { get; }

		static FilesOperationsCopyMoveDialog() { UIHelper<FilesOperationsCopyMoveDialog>.Register(); }

		FilesOperationsCopyMoveDialog(NEVariables variables, bool move)
		{
			Variables = variables;
			InitializeComponent();

			Title = move ? "Move Files" : "Copy Files";

			OldFileName = NewFileName = "x";
		}

		private void ExpressionHelp(object sender, RoutedEventArgs e) => ExpressionHelpDialog.Display(Variables);

		FilesOperationsCopyMoveDialogResult result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new FilesOperationsCopyMoveDialogResult { OldFileName = OldFileName, NewFileName = NewFileName };
			oldFileName.AddCurrentSuggestion();
			newFileName.AddCurrentSuggestion();
			DialogResult = true;
		}

		public static FilesOperationsCopyMoveDialogResult Run(Window parent, NEVariables variables, bool move)
		{
			var dialog = new FilesOperationsCopyMoveDialog(variables, move) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
