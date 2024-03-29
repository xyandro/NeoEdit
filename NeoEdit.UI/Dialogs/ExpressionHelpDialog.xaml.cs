﻿using System.Windows;
using NeoEdit.Common.Expressions;

namespace NeoEdit.UI.Dialogs
{
	partial class ExpressionHelpDialog
	{
		readonly NEVariables Variables;
		ExpressionHelpDialog(NEVariables variables = null)
		{
			Variables = variables;
			InitializeComponent();
			ShowVariables.IsEnabled = variables != null;
		}

		void OkClick(object sender, RoutedEventArgs e) => Close();

		void ShowVariablesClick(object sender, RoutedEventArgs e) => ExpressionHelpVariablesDialog.Run(Variables);

		void ShowUnitsClick(object sender, RoutedEventArgs e) => ExpressionHelpUnitsDialog.Run();

		void ShowKeyboardShortcutsClick(object sender, RoutedEventArgs e) => ExpressionShortcutsDialog.Run();

		public static void Display(NEVariables variables = null) => new ExpressionHelpDialog(variables);
	}
}
