using System.Text.RegularExpressions;
using System.Windows;
using NeoEdit.Common.Expressions;
using NeoEdit.TextEdit.Controls;
using NeoEdit.TextEdit.Dialogs;

namespace NeoEdit.TextEdit.Dialogs
{
	partial class SelectSplitDialog
	{
		public class Result
		{
			public Regex Regex { get; set; }
			public string Index { get; set; }
			public bool IncludeResults { get; set; }
			public bool IncludeEmpty { get; set; }
			public bool Balanced { get; set; }
			public bool TrimWhitespace { get; set; }
		}

		[DepProp]
		public string Text { get { return UIHelper<SelectSplitDialog>.GetPropValue<string>(this); } set { UIHelper<SelectSplitDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string Index { get { return UIHelper<SelectSplitDialog>.GetPropValue<string>(this); } set { UIHelper<SelectSplitDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool MatchCase { get { return UIHelper<SelectSplitDialog>.GetPropValue<bool>(this); } set { UIHelper<SelectSplitDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool IsRegex { get { return UIHelper<SelectSplitDialog>.GetPropValue<bool>(this); } set { UIHelper<SelectSplitDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool IncludeResults { get { return UIHelper<SelectSplitDialog>.GetPropValue<bool>(this); } set { UIHelper<SelectSplitDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool IncludeEmpty { get { return UIHelper<SelectSplitDialog>.GetPropValue<bool>(this); } set { UIHelper<SelectSplitDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool Balanced { get { return UIHelper<SelectSplitDialog>.GetPropValue<bool>(this); } set { UIHelper<SelectSplitDialog>.SetPropValue(this, value); } }
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
			MatchCase = IsRegex = IncludeResults = IncludeEmpty = false;
			Balanced = TrimWhitespace = true;
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
			if (!MatchCase)
				options |= RegexOptions.IgnoreCase;
			result = new Result { Regex = new Regex(text, options), Index = Index, IncludeResults = IncludeResults, IncludeEmpty = IncludeEmpty, Balanced = Balanced, TrimWhitespace = TrimWhitespace };

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
}
