﻿using System.Collections.Generic;
using System.Windows;
using NeoEdit.Common.Expressions;
using NeoEdit.GUI.Controls;

namespace NeoEdit.TextEdit.Dialogs
{
	partial class RandomDataDialog
	{
		internal class Result
		{
			public string Expression { get; set; }
			public string Chars { get; set; }
		}

		[DepProp]
		public string Expression { get { return UIHelper<RandomDataDialog>.GetPropValue<string>(this); } set { UIHelper<RandomDataDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string Chars { get { return UIHelper<RandomDataDialog>.GetPropValue<string>(this); } set { UIHelper<RandomDataDialog>.SetPropValue(this, value); } }
		public NEVariables Variables { get; }

		static RandomDataDialog() { UIHelper<RandomDataDialog>.Register(); }

		RandomDataDialog(NEVariables variables)
		{
			Variables = variables;
			InitializeComponent();
			Chars = "a-zA-Z";
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			var chars = Misc.GetCharsFromRegexString(Chars);
			if (chars.Length == 0)
				return;

			result = new Result { Expression = Expression, Chars = chars };
			DialogResult = true;
		}

		private void ExpressionHelp(object sender, RoutedEventArgs e) => ExpressionHelpDialog.Display();

		public static Result Run(NEVariables variables, Window parent)
		{
			var dialog = new RandomDataDialog(variables) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
