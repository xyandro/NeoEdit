using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using NeoEdit.Program.Controls;
using NeoEdit.Program.Expressions;

namespace NeoEdit.Program.Dialogs
{
	partial class SelectSplitDialog
	{
		public class Result
		{
			public Regex Regex { get; set; }
			public string Index { get; set; }
			public bool IncludeResults { get; set; }
			public bool ExcludeEmpty { get; set; }
			public bool BalanceStrings { get; set; }
			public bool BalanceParens { get; set; }
			public bool BalanceBrackets { get; set; }
			public bool BalanceBraces { get; set; }
			public bool BalanceLTGT { get; set; }
			public bool TrimWhitespace { get; set; }
		}

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

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrEmpty(Text))
				return;

			var text = Text;
			if (!IsRegex)
				text = Regex.Escape(text);
			var options = RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.Multiline;
			if (WholeWords)
				text = $"\\b{text}\\b";
			if (!MatchCase)
				options |= RegexOptions.IgnoreCase;
			result = new Result
			{
				Regex = new Regex(text, options),
				Index = Index,
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

		static public Result Run(Window parent, NEVariables variables)
		{
			var dialog = new SelectSplitDialog(variables) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
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
