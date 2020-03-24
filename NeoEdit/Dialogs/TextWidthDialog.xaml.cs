using System;
using System.Windows;
using NeoEdit.Common.Expressions;
using NeoEdit.Common.Models;
using NeoEdit.Program.Controls;

namespace NeoEdit.Program.Dialogs
{
	partial class TextWidthDialog
	{
		[DepProp]
		public string Expression { get { return UIHelper<TextWidthDialog>.GetPropValue<string>(this); } set { UIHelper<TextWidthDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string PadChar { get { return UIHelper<TextWidthDialog>.GetPropValue<string>(this); } set { UIHelper<TextWidthDialog>.SetPropValue(this, value); } }
		[DepProp]
		public TextWidthDialogResult.TextLocation Location { get { return UIHelper<TextWidthDialog>.GetPropValue<TextWidthDialogResult.TextLocation>(this); } set { UIHelper<TextWidthDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool IsSelect { get { return UIHelper<TextWidthDialog>.GetPropValue<bool>(this); } set { UIHelper<TextWidthDialog>.SetPropValue(this, value); } }
		public NEVariables Variables { get; }

		static TextWidthDialog() { UIHelper<TextWidthDialog>.Register(); }

		TextWidthDialog(bool numeric, bool isSelect, NEVariables variables)
		{
			Variables = variables;
			InitializeComponent();

			padChar.GotFocus += (s, e) => padChar.SelectAll();

			IsSelect = isSelect;

			Expression = "xlmax";
			if (numeric)
				NumericClick(null, null);
			else
				StringClick(null, null);
		}

		void NumericClick(object sender, RoutedEventArgs e)
		{
			PadChar = "0";
			Location = TextWidthDialogResult.TextLocation.End;
		}

		void StringClick(object sender, RoutedEventArgs e)
		{
			PadChar = " ";
			Location = TextWidthDialogResult.TextLocation.Start;
		}

		private void ExpressionHelp(object sender, RoutedEventArgs e) => ExpressionHelpDialog.Display(Variables);

		TextWidthDialogResult result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if (PadChar.Length != 1)
				return;
			result = new TextWidthDialogResult { Expression = Expression, PadChar = PadChar[0], Location = Location };
			expression.AddCurrentSuggestion();
			DialogResult = true;
		}

		public static TextWidthDialogResult Run(Window parent, bool numeric, bool isSelect, NEVariables variables)
		{
			var dialog = new TextWidthDialog(numeric, isSelect, variables) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
