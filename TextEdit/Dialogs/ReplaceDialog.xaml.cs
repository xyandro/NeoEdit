using System.Text.RegularExpressions;
using System.Windows;
using NeoEdit.Common;
using NeoEdit.GUI.Controls;
using NeoEdit.GUI.Dialogs;

namespace NeoEdit.TextEdit.Dialogs
{
	partial class ReplaceDialog
	{
		public class Result
		{
			public Regex Regex { get; set; }
			public string Replace { get; set; }
			public bool SelectionOnly { get; set; }
			public bool MultiLine { get; set; }
		}

		[DepProp]
		public string Text { get { return UIHelper<ReplaceDialog>.GetPropValue<string>(this); } set { UIHelper<ReplaceDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string Replace { get { return UIHelper<ReplaceDialog>.GetPropValue<string>(this); } set { UIHelper<ReplaceDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool WholeWords { get { return UIHelper<ReplaceDialog>.GetPropValue<bool>(this); } set { UIHelper<ReplaceDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool MatchCase { get { return UIHelper<ReplaceDialog>.GetPropValue<bool>(this); } set { UIHelper<ReplaceDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool IsRegex { get { return UIHelper<ReplaceDialog>.GetPropValue<bool>(this); } set { UIHelper<ReplaceDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool SelectionOnly { get { return UIHelper<ReplaceDialog>.GetPropValue<bool>(this); } set { UIHelper<ReplaceDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool EntireSelection { get { return UIHelper<ReplaceDialog>.GetPropValue<bool>(this); } set { UIHelper<ReplaceDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool MultiLine { get { return UIHelper<ReplaceDialog>.GetPropValue<bool>(this); } set { UIHelper<ReplaceDialog>.SetPropValue(this, value); } }

		static bool wholeWordsVal, matchCaseVal, isRegexVal, multiLineVal;

		static ReplaceDialog()
		{
			UIHelper<ReplaceDialog>.Register();
			UIHelper<ReplaceDialog>.AddCallback(a => a.SelectionOnly, (obj, o, n) => { if (!obj.SelectionOnly) obj.EntireSelection = false; });
			UIHelper<ReplaceDialog>.AddCallback(a => a.EntireSelection, (obj, o, n) => { if (obj.EntireSelection) obj.SelectionOnly = true; });
		}

		ReplaceDialog(string _text, bool _selectionOnly)
		{
			InitializeComponent();

			Text = _text.CoalesceNullOrEmpty(text.GetLastSuggestion(), "");
			Replace = replace.GetLastSuggestion() ?? "";
			SelectionOnly = _selectionOnly;
			WholeWords = wholeWordsVal;
			MatchCase = matchCaseVal;
			IsRegex = isRegexVal;
			MultiLine = multiLineVal;
		}

		void Escape(object sender, RoutedEventArgs e) => Text = Regex.Escape(Text);
		void Unescape(object sender, RoutedEventArgs e) => Text = Regex.Unescape(Text);

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrEmpty(Text))
				return;

			var text = Text;
			var replace = Replace;
			if (!IsRegex)
			{
				text = Regex.Escape(text);
				replace = replace.Replace("$", "$$");
			}
			if (WholeWords)
				text = $"\\b{text}\\b";
			if (EntireSelection)
				text = $"\\A{text}\\Z";
			var options = RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.Multiline;
			if (!MatchCase)
				options |= RegexOptions.IgnoreCase;
			result = new Result { Regex = new Regex(text, options), Replace = replace, SelectionOnly = SelectionOnly, MultiLine = MultiLine };

			wholeWordsVal = WholeWords;
			matchCaseVal = MatchCase;
			isRegexVal = IsRegex;
			multiLineVal = MultiLine;

			text = Text;
			this.text.AddCurrentSuggestion();
			this.replace.AddCurrentSuggestion();

			DialogResult = true;
		}

		static public Result Run(Window parent, string text = null, bool selectionOnly = false)
		{
			var dialog = new ReplaceDialog(text, selectionOnly) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}

		void RegExHelp(object sender, RoutedEventArgs e) => RegExHelpDialog.Display();
	}
}
