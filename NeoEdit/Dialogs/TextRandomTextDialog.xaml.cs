using System;
using System.Windows;
using NeoEdit.Program;
using NeoEdit.Program.Controls;
using NeoEdit.Program.Expressions;

namespace NeoEdit.Program.Dialogs
{
	partial class TextRandomTextDialog
	{
		public class Result
		{
			public string Expression { get; set; }
			public string Chars { get; set; }
		}

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

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			var chars = Helpers.GetCharsFromCharString(Chars);
			if (chars.Length == 0)
				return;

			result = new Result { Expression = Expression, Chars = chars };
			expression.AddCurrentSuggestion();
			DialogResult = true;
		}

		private void ExpressionHelp(object sender, RoutedEventArgs e) => ExpressionHelpDialog.Display(Variables);

		public static Result Run(Window parent, NEVariables variables)
		{
			var dialog = new TextRandomTextDialog(variables) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
