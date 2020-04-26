using System;
using System.Windows;
using NeoEdit.Common;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Expressions;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class TextRandomTextDialog
	{
		[DepProp]
		public string Expression { get { return UIHelper<TextRandomTextDialog>.GetPropValue<string>(this); } set { UIHelper<TextRandomTextDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string Chars { get { return UIHelper<TextRandomTextDialog>.GetPropValue<string>(this); } set { UIHelper<TextRandomTextDialog>.SetPropValue(this, value); } }
		public NEVariables Variables { get; }

		static TextRandomTextDialog() { UIHelper<TextRandomTextDialog>.Register(); }

		TextRandomTextDialog(NEVariables variables)
		{
			Variables = variables;
			InitializeComponent();
			Chars = "a-zA-Z";
			Expression = "x";
		}

		TextRandomTextDialogResult result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			var chars = Helpers.GetCharsFromCharString(Chars);
			if (chars.Length == 0)
				return;

			result = new TextRandomTextDialogResult { Expression = Expression, Chars = chars };
			expression.AddCurrentSuggestion();
			DialogResult = true;
		}

		private void ExpressionHelp(object sender, RoutedEventArgs e) => ExpressionHelpDialog.Display(Variables);

		public static TextRandomTextDialogResult Run(Window parent, NEVariables variables)
		{
			var dialog = new TextRandomTextDialog(variables) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
