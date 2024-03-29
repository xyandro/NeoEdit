﻿using System;
using System.Collections.Generic;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Expressions;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Files_Set_Size_Dialog
	{
		[DepProp]
		public string Expression { get { return UIHelper<Files_Set_Size_Dialog>.GetPropValue<string>(this); } set { UIHelper<Files_Set_Size_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public long Factor { get { return UIHelper<Files_Set_Size_Dialog>.GetPropValue<long>(this); } set { UIHelper<Files_Set_Size_Dialog>.SetPropValue(this, value); } }
		public Dictionary<string, long> FactorDict { get; }
		public NEVariables Variables { get; }

		static Files_Set_Size_Dialog() { UIHelper<Files_Set_Size_Dialog>.Register(); }

		Files_Set_Size_Dialog(NEVariables variables)
		{
			FactorDict = new Dictionary<string, long>
			{
				["GB"] = 1 << 30,
				["MB"] = 1 << 20,
				["KB"] = 1 << 10,
				["bytes"] = 1 << 0,
			};
			Variables = variables;
			InitializeComponent();
			Expression = "size";
			Factor = 1;
		}

		Configuration_Files_Set_Size result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Configuration_Files_Set_Size { Expression = Expression, Factor = Factor };
			expression.AddCurrentSuggestion();
			DialogResult = true;
		}

		void ExpressionHelp(object sender, RoutedEventArgs e) => ExpressionHelpDialog.Display(Variables);

		public static Configuration_Files_Set_Size Run(Window parent, NEVariables variables)
		{
			var dialog = new Files_Set_Size_Dialog(variables) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
