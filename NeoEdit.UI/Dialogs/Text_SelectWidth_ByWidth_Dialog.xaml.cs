using System;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Expressions;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Text_SelectWidth_ByWidth_Dialog
	{
		[DepProp]
		public string Expression { get { return UIHelper<Text_SelectWidth_ByWidth_Dialog>.GetPropValue<string>(this); } set { UIHelper<Text_SelectWidth_ByWidth_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public string PadChar { get { return UIHelper<Text_SelectWidth_ByWidth_Dialog>.GetPropValue<string>(this); } set { UIHelper<Text_SelectWidth_ByWidth_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public Configuration_Text_SelectWidth_ByWidth.TextLocation Location { get { return UIHelper<Text_SelectWidth_ByWidth_Dialog>.GetPropValue<Configuration_Text_SelectWidth_ByWidth.TextLocation>(this); } set { UIHelper<Text_SelectWidth_ByWidth_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool IsSelect { get { return UIHelper<Text_SelectWidth_ByWidth_Dialog>.GetPropValue<bool>(this); } set { UIHelper<Text_SelectWidth_ByWidth_Dialog>.SetPropValue(this, value); } }
		public NEVariables Variables { get; }

		static Text_SelectWidth_ByWidth_Dialog() { UIHelper<Text_SelectWidth_ByWidth_Dialog>.Register(); }

		Text_SelectWidth_ByWidth_Dialog(bool numeric, bool isSelect, NEVariables variables)
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
			Location = Configuration_Text_SelectWidth_ByWidth.TextLocation.End;
		}

		void StringClick(object sender, RoutedEventArgs e)
		{
			PadChar = " ";
			Location = Configuration_Text_SelectWidth_ByWidth.TextLocation.Start;
		}

		private void ExpressionHelp(object sender, RoutedEventArgs e) => ExpressionHelpDialog.Display(Variables);

		Configuration_Text_SelectWidth_ByWidth result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if (PadChar.Length != 1)
				return;
			result = new Configuration_Text_SelectWidth_ByWidth { Expression = Expression, PadChar = PadChar[0], Location = Location };
			expression.AddCurrentSuggestion();
			DialogResult = true;
		}

		public static Configuration_Text_SelectWidth_ByWidth Run(Window parent, bool numeric, bool isSelect, NEVariables variables)
		{
			var dialog = new Text_SelectWidth_ByWidth_Dialog(numeric, isSelect, variables) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
