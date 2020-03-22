using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using NeoEdit.Program.Controls;
using NeoEdit.Program.Expressions;
using NeoEdit.Program.Models;

namespace NeoEdit.Program.Dialogs
{
	partial class SelectSplitDialog
	{
		[DepProp]
		public string Text { get { return UIHelper<SelectSplitDialog>.GetPropValue<string>(this); } set { UIHelper<SelectSplitDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string Index { get { return UIHelper<SelectSplitDialog>.GetPropValue<string>(this); } set { UIHelper<SelectSplitDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool WholeWords { get { return UIHelper<SelectSplitDialog>.GetPropValue<bool>(this); } set { UIHelper<SelectSplitDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool MatchCase { get { return UIHelper<SelectSplitDialog>.GetPropValue<bool>(this); } set { UIHelper<SelectSplitDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool IsRegex { get { return UIHelper<SelectSplitDialog>.GetPropValue<bool>(this); } set { UIHelper<SelectSplitDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool IncludeResults { get { return UIHelper<SelectSplitDialog>.GetPropValue<bool>(this); } set { UIHelper<SelectSplitDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool ExcludeEmpty { get { return UIHelper<SelectSplitDialog>.GetPropValue<bool>(this); } set { UIHelper<SelectSplitDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool BalanceStrings { get { return UIHelper<SelectSplitDialog>.GetPropValue<bool>(this); } set { UIHelper<SelectSplitDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool BalanceParens { get { return UIHelper<SelectSplitDialog>.GetPropValue<bool>(this); } set { UIHelper<SelectSplitDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool BalanceBrackets { get { return UIHelper<SelectSplitDialog>.GetPropValue<bool>(this); } set { UIHelper<SelectSplitDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool BalanceBraces { get { return UIHelper<SelectSplitDialog>.GetPropValue<bool>(this); } set { UIHelper<SelectSplitDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool BalanceLTGT { get { return UIHelper<SelectSplitDialog>.GetPropValue<bool>(this); } set { UIHelper<SelectSplitDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool TrimWhitespace { get { return UIHelper<SelectSplitDialog>.GetPropValue<bool>(this); } set { UIHelper<SelectSplitDialog>.SetPropValue(this, value); } }
		public NEVariables Variables { get; }

		static SelectSplitDialog() { UIHelper<SelectSplitDialog>.Register(); }

		SelectSplitDialog(NEVariables variables)
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
		}

		SelectSplitDialogResult result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrEmpty(Text))
				return;

			result = new SelectSplitDialogResult
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

			this.text.AddCurrentSuggestion();

			DialogResult = true;
		}

		void RegExHelp(object sender, RoutedEventArgs e) => RegExHelpDialog.Display();

		static public SelectSplitDialogResult Run(Window parent, NEVariables variables)
		{
			var dialog = new SelectSplitDialog(variables) { Owner = parent };
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
