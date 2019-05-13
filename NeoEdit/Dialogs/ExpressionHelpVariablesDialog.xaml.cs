﻿using System.Windows;
using NeoEdit.Expressions;
using NeoEdit.Controls;

namespace NeoEdit.Dialogs
{
	partial class ExpressionHelpVariablesDialog
	{
		[DepProp]
		NEVariables Variables { get { return UIHelper<ExpressionHelpVariablesDialog>.GetPropValue<NEVariables>(this); } set { UIHelper<ExpressionHelpVariablesDialog>.SetPropValue(this, value); } }

		static ExpressionHelpVariablesDialog() { UIHelper<ExpressionHelpVariablesDialog>.Register(); }

		ExpressionHelpVariablesDialog(NEVariables variables)
		{
			InitializeComponent();
			Variables = variables;
		}

		void OkClick(object sender, RoutedEventArgs e) => DialogResult = true;

		public static void Run(NEVariables variables = null) => new ExpressionHelpVariablesDialog(variables).ShowDialog();
	}
}