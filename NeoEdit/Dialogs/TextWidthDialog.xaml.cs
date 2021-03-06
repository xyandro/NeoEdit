﻿using System.Windows;
using NeoEdit.Program.Controls;
using NeoEdit.Program.Expressions;

namespace NeoEdit.Program.Dialogs
{
	partial class TextWidthDialog
	{
		public enum TextLocation
		{
			Start,
			Middle,
			End,
		}

		public class Result
		{
			public string Expression { get; set; }
			public char PadChar { get; set; }
			public TextLocation Location { get; set; }
		}

		[DepProp]
		public string Expression { get { return UIHelper<TextWidthDialog>.GetPropValue<string>(this); } set { UIHelper<TextWidthDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string PadChar { get { return UIHelper<TextWidthDialog>.GetPropValue<string>(this); } set { UIHelper<TextWidthDialog>.SetPropValue(this, value); } }
		[DepProp]
		public TextLocation Location { get { return UIHelper<TextWidthDialog>.GetPropValue<TextLocation>(this); } set { UIHelper<TextWidthDialog>.SetPropValue(this, value); } }
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
			Location = TextLocation.End;
		}

		void StringClick(object sender, RoutedEventArgs e)
		{
			PadChar = " ";
			Location = TextLocation.Start;
		}

		private void ExpressionHelp(object sender, RoutedEventArgs e) => ExpressionHelpDialog.Display(Variables);

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if (PadChar.Length != 1)
				return;
			result = new Result { Expression = Expression, PadChar = PadChar[0], Location = Location };
			expression.AddCurrentSuggestion();
			DialogResult = true;
		}

		public static Result Run(Window parent, bool numeric, bool isSelect, NEVariables variables)
		{
			var dialog = new TextWidthDialog(numeric, isSelect, variables) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
