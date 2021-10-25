using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Expressions;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Text_Select_Split_Dialog
	{
		[DepProp]
		public string Text { get { return UIHelper<Text_Select_Split_Dialog>.GetPropValue<string>(this); } set { UIHelper<Text_Select_Split_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public string Index { get { return UIHelper<Text_Select_Split_Dialog>.GetPropValue<string>(this); } set { UIHelper<Text_Select_Split_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool WholeWords { get { return UIHelper<Text_Select_Split_Dialog>.GetPropValue<bool>(this); } set { UIHelper<Text_Select_Split_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool MatchCase { get { return UIHelper<Text_Select_Split_Dialog>.GetPropValue<bool>(this); } set { UIHelper<Text_Select_Split_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool IsRegex { get { return UIHelper<Text_Select_Split_Dialog>.GetPropValue<bool>(this); } set { UIHelper<Text_Select_Split_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool IncludeResults { get { return UIHelper<Text_Select_Split_Dialog>.GetPropValue<bool>(this); } set { UIHelper<Text_Select_Split_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool ExcludeEmpty { get { return UIHelper<Text_Select_Split_Dialog>.GetPropValue<bool>(this); } set { UIHelper<Text_Select_Split_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool BalanceStrings { get { return UIHelper<Text_Select_Split_Dialog>.GetPropValue<bool>(this); } set { UIHelper<Text_Select_Split_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool BalanceParens { get { return UIHelper<Text_Select_Split_Dialog>.GetPropValue<bool>(this); } set { UIHelper<Text_Select_Split_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool BalanceBrackets { get { return UIHelper<Text_Select_Split_Dialog>.GetPropValue<bool>(this); } set { UIHelper<Text_Select_Split_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool BalanceBraces { get { return UIHelper<Text_Select_Split_Dialog>.GetPropValue<bool>(this); } set { UIHelper<Text_Select_Split_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool BalanceLTGT { get { return UIHelper<Text_Select_Split_Dialog>.GetPropValue<bool>(this); } set { UIHelper<Text_Select_Split_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool TrimWhitespace { get { return UIHelper<Text_Select_Split_Dialog>.GetPropValue<bool>(this); } set { UIHelper<Text_Select_Split_Dialog>.SetPropValue(this, value); } }
		public NEVariables Variables { get; }

		static Text_Select_Split_Dialog() { UIHelper<Text_Select_Split_Dialog>.Register(); }

		Text_Select_Split_Dialog(NEVariables variables)
		{
			Variables = variables;
			InitializeComponent();
			Reset();
			Text = ",";
		}

		void Reset(object sender = null, RoutedEventArgs e = null)
		{
			Index = "0";
			WholeWords = MatchCase = IsRegex = IncludeResults = ExcludeEmpty = false;
			BalanceStrings = BalanceParens = BalanceBrackets = BalanceBraces = TrimWhitespace = true;
			BalanceLTGT = false;
			text.SelectAll();
			index.SelectAll();
		}

		Configuration_Text_Select_Split result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrEmpty(Text))
				return;

			result = new Configuration_Text_Select_Split
			{
				Text = Text,
				Index = Index,
				WholeWords = WholeWords,
				MatchCase = MatchCase,
				IsRegex = IsRegex,
				IncludeResults = IncludeResults,
				ExcludeEmpty = ExcludeEmpty,
				BalanceStrings = BalanceStrings,
				BalanceParens = BalanceParens,
				BalanceBrackets = BalanceBrackets,
				BalanceBraces = BalanceBraces,
				BalanceLTGT = BalanceLTGT,
				TrimWhitespace = TrimWhitespace,
			};

			text.AddCurrentSuggestion();

			DialogResult = true;
		}

		void RegExHelp(object sender, RoutedEventArgs e) => RegExHelpDialog.Display();

		public static Configuration_Text_Select_Split Run(Window parent, NEVariables variables)
		{
			var dialog = new Text_Select_Split_Dialog(variables) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}

	class CheckboxConverter : MarkupExtension, IMultiValueConverter
	{
		public override object ProvideValue(IServiceProvider serviceProvider) => this;

		public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		{
			var result = default(bool?);
			foreach (var value in values)
			{
				if (value is bool b)
				{
					if (!result.HasValue)
						result = b;
					if (result != b)
						return null;
				}
			}
			return result;
		}

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => Enumerable.Repeat(value, targetTypes.Length).ToArray();
	}
}
