using System;
using System.Windows;
using NeoEdit.Common;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Expressions;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Text_Random_Dialog
	{
		[DepProp]
		public string Expression { get { return UIHelper<Text_Random_Dialog>.GetPropValue<string>(this); } set { UIHelper<Text_Random_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public string Chars { get { return UIHelper<Text_Random_Dialog>.GetPropValue<string>(this); } set { UIHelper<Text_Random_Dialog>.SetPropValue(this, value); } }
		public NEVariables Variables { get; }

		static Text_Random_Dialog() { UIHelper<Text_Random_Dialog>.Register(); }

		Text_Random_Dialog(NEVariables variables)
		{
			Variables = variables;
			InitializeComponent();
			Chars = "a-zA-Z";
			Expression = "x";
		}

		Configuration_Text_Random result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			var chars = Helpers.GetCharsFromCharString(Chars);
			if (chars.Length == 0)
				return;

			result = new Configuration_Text_Random { Expression = Expression, Chars = chars };
			expression.AddCurrentSuggestion();
			DialogResult = true;
		}

		private void ExpressionHelp(object sender, RoutedEventArgs e) => ExpressionHelpDialog.Display(Variables);

		public static Configuration_Text_Random Run(Window parent, NEVariables variables)
		{
			var dialog = new Text_Random_Dialog(variables) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
