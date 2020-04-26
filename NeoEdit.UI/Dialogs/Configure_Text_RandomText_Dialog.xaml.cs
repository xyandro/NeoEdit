using System;
using System.Windows;
using NeoEdit.Common;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Expressions;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Configure_Text_RandomText_Dialog
	{
		[DepProp]
		public string Expression { get { return UIHelper<Configure_Text_RandomText_Dialog>.GetPropValue<string>(this); } set { UIHelper<Configure_Text_RandomText_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public string Chars { get { return UIHelper<Configure_Text_RandomText_Dialog>.GetPropValue<string>(this); } set { UIHelper<Configure_Text_RandomText_Dialog>.SetPropValue(this, value); } }
		public NEVariables Variables { get; }

		static Configure_Text_RandomText_Dialog() { UIHelper<Configure_Text_RandomText_Dialog>.Register(); }

		Configure_Text_RandomText_Dialog(NEVariables variables)
		{
			Variables = variables;
			InitializeComponent();
			Chars = "a-zA-Z";
			Expression = "x";
		}

		Configuration_Text_RandomText result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			var chars = Helpers.GetCharsFromCharString(Chars);
			if (chars.Length == 0)
				return;

			result = new Configuration_Text_RandomText { Expression = Expression, Chars = chars };
			expression.AddCurrentSuggestion();
			DialogResult = true;
		}

		private void ExpressionHelp(object sender, RoutedEventArgs e) => ExpressionHelpDialog.Display(Variables);

		public static Configuration_Text_RandomText Run(Window parent, NEVariables variables)
		{
			var dialog = new Configure_Text_RandomText_Dialog(variables) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
