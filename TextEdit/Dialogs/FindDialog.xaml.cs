using System.Text.RegularExpressions;
using System.Windows;
using NeoEdit.Common;
using NeoEdit.Common.Expressions;
using NeoEdit.GUI.Controls;
using NeoEdit.GUI.Dialogs;

namespace NeoEdit.TextEdit.Dialogs
{
	partial class FindDialog
	{
		public class Result
		{
			public string Text { get; set; }
			public bool WholeWords { get; set; }
			public bool MatchCase { get; set; }
			public bool MultiLine { get; set; }
			public bool IsExpression { get; set; }
			public bool IsRegex { get; set; }
			public bool RegexGroups { get; set; }
			public bool SelectionOnly { get; set; }
			public bool EntireSelection { get; set; }
			public bool KeepMatching { get; set; }
			public bool RemoveMatching { get; set; }
			public bool All { get; set; }
		}

		[DepProp]
		public string Text { get { return UIHelper<FindDialog>.GetPropValue<string>(this); } set { UIHelper<FindDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool WholeWords { get { return UIHelper<FindDialog>.GetPropValue<bool>(this); } set { UIHelper<FindDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool MatchCase { get { return UIHelper<FindDialog>.GetPropValue<bool>(this); } set { UIHelper<FindDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool IsExpression { get { return UIHelper<FindDialog>.GetPropValue<bool>(this); } set { UIHelper<FindDialog>.SetPropValue(this, value); } }
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
		[DepProp]
		public NEVariables Variables { get { return UIHelper<FindDialog>.GetPropValue<NEVariables>(this); } set { UIHelper<FindDialog>.SetPropValue(this, value); } }

		static bool wholeWordsVal, matchCaseVal, multiLineVal, isExpressionVal, isRegexVal, regexGroupsVal, entireSelectionVal, keepMatchingVal, removeMatchingVal;

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

		FindDialog(string text, bool selectionOnly, NEVariables variables)
		{
			InitializeComponent();

			Text = text.CoalesceNullOrEmpty(this.text.GetLastSuggestion(), "");
			WholeWords = wholeWordsVal;
			MatchCase = matchCaseVal;
			MultiLine = multiLineVal;
			IsExpression = isExpressionVal;
			IsRegex = isRegexVal;
			RegexGroups = regexGroupsVal;
			EntireSelection = entireSelectionVal;
			KeepMatching = keepMatchingVal;
			RemoveMatching = removeMatchingVal;
			SelectionOnly = selectionOnly;
			Variables = variables;
		}

		void Escape(object sender, RoutedEventArgs e) => Text = Regex.Escape(Text);
		void Unescape(object sender, RoutedEventArgs e) => Text = Regex.Unescape(Text);

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrEmpty(Text))
				return;

			result = new Result { Text = Text, WholeWords = WholeWords, MatchCase = MatchCase, IsExpression = IsExpression, IsRegex = IsRegex, RegexGroups = RegexGroups, SelectionOnly = SelectionOnly, EntireSelection = EntireSelection, KeepMatching = KeepMatching, RemoveMatching = RemoveMatching, MultiLine = MultiLine, All = sender == selectAll };

			wholeWordsVal = WholeWords;
			matchCaseVal = MatchCase;
			multiLineVal = MultiLine;
			isExpressionVal = IsExpression;
			isRegexVal = IsRegex;
			regexGroupsVal = RegexGroups;
			entireSelectionVal = EntireSelection;
			keepMatchingVal = KeepMatching;
			removeMatchingVal = RemoveMatching;

			text.AddCurrentSuggestion();

			DialogResult = true;
		}

		void RegExHelp(object sender, RoutedEventArgs e) => RegExHelpDialog.Display();

		void Reset(object sender, RoutedEventArgs e) => WholeWords = MatchCase = MultiLine = IsExpression = IsRegex = RegexGroups = EntireSelection = KeepMatching = RemoveMatching = false;

		static public Result Run(Window parent, string text, bool selectionOnly, NEVariables variables)
		{
			var dialog = new FindDialog(text, selectionOnly, variables) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
