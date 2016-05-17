using System.Text.RegularExpressions;
using System.Windows;
using NeoEdit.Common;
using NeoEdit.GUI.Controls;
using NeoEdit.GUI.Dialogs;

namespace NeoEdit.TextEdit.Dialogs
{
	partial class FindDialog
	{
		public class Result
		{
			public Regex Regex { get; set; }
			public bool RegexGroups { get; set; }
			public bool SelectionOnly { get; set; }
			public bool KeepMatching { get; set; }
			public bool RemoveMatching { get; set; }
			public bool MultiLine { get; set; }
			public bool All { get; set; }
		}

		[DepProp]
		public string Text { get { return UIHelper<FindDialog>.GetPropValue<string>(this); } set { UIHelper<FindDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool WholeWords { get { return UIHelper<FindDialog>.GetPropValue<bool>(this); } set { UIHelper<FindDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool MatchCase { get { return UIHelper<FindDialog>.GetPropValue<bool>(this); } set { UIHelper<FindDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool IsRegex { get { return UIHelper<FindDialog>.GetPropValue<bool>(this); } set { UIHelper<FindDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool RegexGroups { get { return UIHelper<FindDialog>.GetPropValue<bool>(this); } set { UIHelper<FindDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool SelectionOnly { get { return UIHelper<FindDialog>.GetPropValue<bool>(this); } set { UIHelper<FindDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool EntireSelection { get { return UIHelper<FindDialog>.GetPropValue<bool>(this); } set { UIHelper<FindDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool KeepMatching { get { return UIHelper<FindDialog>.GetPropValue<bool>(this); } set { UIHelper<FindDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool RemoveMatching { get { return UIHelper<FindDialog>.GetPropValue<bool>(this); } set { UIHelper<FindDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool MultiLine { get { return UIHelper<FindDialog>.GetPropValue<bool>(this); } set { UIHelper<FindDialog>.SetPropValue(this, value); } }

		static bool wholeWordsVal, matchCaseVal, isRegexVal, regexGroupsVal, multiLineVal;

		static FindDialog()
		{
			UIHelper<FindDialog>.Register();
			UIHelper<FindDialog>.AddCallback(a => a.IsRegex, (obj, o, n) => { if (!obj.IsRegex) obj.RegexGroups = false; });
			UIHelper<FindDialog>.AddCallback(a => a.RegexGroups, (obj, o, n) => { if (obj.RegexGroups) obj.IsRegex = true; });
			UIHelper<FindDialog>.AddCallback(a => a.SelectionOnly, (obj, o, n) => { if (!obj.SelectionOnly) obj.EntireSelection = obj.KeepMatching = obj.RemoveMatching = false; });
			UIHelper<FindDialog>.AddCallback(a => a.EntireSelection, (obj, o, n) => { if (obj.EntireSelection) obj.SelectionOnly = true; });
			UIHelper<FindDialog>.AddCallback(a => a.KeepMatching, (obj, o, n) => { if (obj.KeepMatching) { obj.SelectionOnly = true; obj.RemoveMatching = false; } });
			UIHelper<FindDialog>.AddCallback(a => a.RemoveMatching, (obj, o, n) => { if (obj.RemoveMatching) { obj.SelectionOnly = true; obj.KeepMatching = false; } });
		}

		FindDialog(string _Text, bool _SelectionOnly)
		{
			InitializeComponent();

			Text = _Text.CoalesceNullOrEmpty(text.GetLastSuggestion(), "");
			SelectionOnly = _SelectionOnly;
			WholeWords = wholeWordsVal;
			MatchCase = matchCaseVal;
			RegexGroups = regexGroupsVal;
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
			if (!IsRegex)
				text = Regex.Escape(text);
			if (WholeWords)
				text = $"\\b{text}\\b";
			if (EntireSelection)
				text = $"\\A{text}\\Z";
			var options = RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.Multiline;
			if (!MatchCase)
				options |= RegexOptions.IgnoreCase;
			result = new Result { Regex = new Regex(text, options), RegexGroups = RegexGroups, SelectionOnly = SelectionOnly, KeepMatching = KeepMatching, RemoveMatching = RemoveMatching, MultiLine = MultiLine, All = sender == selectAll };

			wholeWordsVal = WholeWords;
			matchCaseVal = MatchCase;
			isRegexVal = IsRegex;
			regexGroupsVal = RegexGroups;
			multiLineVal = MultiLine;

			text = Text;
			this.text.AddCurrentSuggestion();

			DialogResult = true;
		}

		static public Result Run(Window parent, string text = null, bool selectionOnly = false)
		{
			var dialog = new FindDialog(text, selectionOnly) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}

		void RegExHelp(object sender, RoutedEventArgs e) => RegExHelpDialog.Display();
	}
}
