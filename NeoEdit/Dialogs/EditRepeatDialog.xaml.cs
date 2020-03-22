﻿using System;
using System.Windows;
using NeoEdit.Program.Controls;
using NeoEdit.Program.Expressions;
using NeoEdit.Program.Models;

namespace NeoEdit.Program.Dialogs
{
	partial class EditRepeatDialog
	{
		[DepProp]
		public string Expression { get { return UIHelper<EditRepeatDialog>.GetPropValue<string>(this); } set { UIHelper<EditRepeatDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool SelectRepetitions { get { return UIHelper<EditRepeatDialog>.GetPropValue<bool>(this); } set { UIHelper<EditRepeatDialog>.SetPropValue(this, value); } }
		public NEVariables Variables { get; }

		static EditRepeatDialog() { UIHelper<EditRepeatDialog>.Register(); }

		EditRepeatDialog(bool selectRepetitions, NEVariables variables)
		{
			Variables = variables;
			InitializeComponent();

			Expression = "1";
			SelectRepetitions = selectRepetitions;
		}

		private void ExpressionHelp(object sender, RoutedEventArgs e) => ExpressionHelpDialog.Display(Variables);

		EditRepeatDialogResult result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new EditRepeatDialogResult { Expression = Expression, SelectRepetitions = SelectRepetitions };
			expression.AddCurrentSuggestion();
			DialogResult = true;
		}

		static public EditRepeatDialogResult Run(Window parent, bool selectRepetitions, NEVariables variables)
		{
			var dialog = new EditRepeatDialog(selectRepetitions, variables) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
