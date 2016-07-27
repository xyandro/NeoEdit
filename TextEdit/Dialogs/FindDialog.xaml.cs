using System;
using System.Text.RegularExpressions;
using System.Windows;
using NeoEdit.Common;
using NeoEdit.GUI.Controls;
using NeoEdit.GUI.Dialogs;

namespace NeoEdit.TextEdit.Dialogs
{
	partial class FindDialog
	{
		public enum ResultType
		{
			None,
			CopyCount,
			Find,
			SelectAll,
		}

		public class Result
		{
			public string Text { get; set; }
			public bool WholeWords { get; set; }
			public bool MatchCase { get; set; }
			public bool MultiLine { get; set; }
			public bool IsRegex { get; set; }
			public bool RegexGroups { get; set; }
			public bool SelectionOnly { get; set; }
			public bool EntireSelection { get; set; }
			public bool KeepMatching { get; set; }
			public bool RemoveMatching { get; set; }
			public ResultType Type { get; set; }
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

		static bool wholeWordsVal, matchCaseVal, multiLineVal, isRegexVal, regexGroupsVal, entireSelectionVal, keepMatchingVal, removeMatchingVal;

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

		FindDialog(string text, bool selectionOnly)
		{
			InitializeComponent();

			Text = text.CoalesceNullOrEmpty(this.text.GetLastSuggestion(), "");
			WholeWords = wholeWordsVal;
			MatchCase = matchCaseVal;
			MultiLine = multiLineVal;
			IsRegex = isRegexVal;
			RegexGroups = regexGroupsVal;
			EntireSelection = entireSelectionVal;
			KeepMatching = keepMatchingVal;
			RemoveMatching = removeMatchingVal;
			SelectionOnly = selectionOnly;
		}

		void Escape(object sender, RoutedEventArgs e) => Text = Regex.Escape(Text);
		void Unescape(object sender, RoutedEventArgs e) => Text = Regex.Unescape(Text);

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrEmpty(Text))
				return;

			result = new Result { Text = Text, WholeWords = WholeWords, MatchCase = MatchCase, IsRegex = IsRegex, RegexGroups = RegexGroups, SelectionOnly = SelectionOnly, EntireSelection = EntireSelection, KeepMatching = KeepMatching, RemoveMatching = RemoveMatching, MultiLine = MultiLine };
			if (sender == copyCount)
				result.Type = ResultType.CopyCount;
			else if (sender == find)
				result.Type = ResultType.Find;
			else if (sender == selectAll)
				result.Type = ResultType.SelectAll;
			else
				throw new Exception("Invalid search type");

			wholeWordsVal = WholeWords;
			matchCaseVal = MatchCase;
			multiLineVal = MultiLine;
			isRegexVal = IsRegex;
			regexGroupsVal = RegexGroups;
			entireSelectionVal = EntireSelection;
			keepMatchingVal = KeepMatching;
			removeMatchingVal = RemoveMatching;

			text.AddCurrentSuggestion();

			DialogResult = true;
		}

		void RegExHelp(object sender, RoutedEventArgs e) => RegExHelpDialog.Display();

		void Reset(object sender, RoutedEventArgs e) => WholeWords = MatchCase = MultiLine = IsRegex = RegexGroups = EntireSelection = KeepMatching = RemoveMatching = false;

		static public Result Run(Window parent, string text, bool selectionOnly)
		{
			var dialog = new FindDialog(text, selectionOnly) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
