using System.Text.RegularExpressions;
using System.Windows;
using NeoEdit.Common.Expressions;
using NeoEdit.GUI.Controls;
using NeoEdit.GUI.Dialogs;

namespace NeoEdit.TextEdit.Dialogs
{
	partial class MultiFindDialog
	{
		public class Result
		{
			public string Text { get; set; }
			public bool WholeWords { get; set; }
			public bool MatchCase { get; set; }
			public bool IsRegex { get; set; }
			public bool RegexGroups { get; set; }
			public bool SelectionOnly { get; set; }
			public bool EntireSelection { get; set; }
			public bool KeepMatching { get; set; }
			public bool RemoveMatching { get; set; }
			public bool MultiLine { get; set; }
		}

		[DepProp]
		public string Text { get { return UIHelper<MultiFindDialog>.GetPropValue<string>(this); } set { UIHelper<MultiFindDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool WholeWords { get { return UIHelper<MultiFindDialog>.GetPropValue<bool>(this); } set { UIHelper<MultiFindDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool MatchCase { get { return UIHelper<MultiFindDialog>.GetPropValue<bool>(this); } set { UIHelper<MultiFindDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool IsRegex { get { return UIHelper<MultiFindDialog>.GetPropValue<bool>(this); } set { UIHelper<MultiFindDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool RegexGroups { get { return UIHelper<MultiFindDialog>.GetPropValue<bool>(this); } set { UIHelper<MultiFindDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool SelectionOnly { get { return UIHelper<MultiFindDialog>.GetPropValue<bool>(this); } set { UIHelper<MultiFindDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool EntireSelection { get { return UIHelper<MultiFindDialog>.GetPropValue<bool>(this); } set { UIHelper<MultiFindDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool KeepMatching { get { return UIHelper<MultiFindDialog>.GetPropValue<bool>(this); } set { UIHelper<MultiFindDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool RemoveMatching { get { return UIHelper<MultiFindDialog>.GetPropValue<bool>(this); } set { UIHelper<MultiFindDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool MultiLine { get { return UIHelper<MultiFindDialog>.GetPropValue<bool>(this); } set { UIHelper<MultiFindDialog>.SetPropValue(this, value); } }
		[DepProp]
		public NEVariables Variables { get { return UIHelper<MultiFindDialog>.GetPropValue<NEVariables>(this); } set { UIHelper<MultiFindDialog>.SetPropValue(this, value); } }

		static bool wholeWordsVal, matchCaseVal, isRegexVal, regexGroupsVal, multiLineVal;

		static MultiFindDialog()
		{
			UIHelper<MultiFindDialog>.Register();
			UIHelper<MultiFindDialog>.AddCallback(a => a.IsRegex, (obj, o, n) => { if (!obj.IsRegex) obj.RegexGroups = false; });
			UIHelper<MultiFindDialog>.AddCallback(a => a.RegexGroups, (obj, o, n) => { if (obj.RegexGroups) obj.IsRegex = true; });
			UIHelper<MultiFindDialog>.AddCallback(a => a.SelectionOnly, (obj, o, n) => { if (!obj.SelectionOnly) obj.EntireSelection = obj.KeepMatching = obj.RemoveMatching = false; });
			UIHelper<MultiFindDialog>.AddCallback(a => a.EntireSelection, (obj, o, n) => { if (obj.EntireSelection) obj.SelectionOnly = true; });
			UIHelper<MultiFindDialog>.AddCallback(a => a.KeepMatching, (obj, o, n) => { if (obj.KeepMatching) { obj.SelectionOnly = true; obj.RemoveMatching = false; } });
			UIHelper<MultiFindDialog>.AddCallback(a => a.RemoveMatching, (obj, o, n) => { if (obj.RemoveMatching) { obj.SelectionOnly = true; obj.KeepMatching = false; } });
		}

		MultiFindDialog(NEVariables variables, bool selectionOnly)
		{
			InitializeComponent();

			Text = text.GetLastSuggestion() ?? "k";
			Variables = variables;
			SelectionOnly = selectionOnly;
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

			result = new Result { Text = Text, WholeWords = WholeWords, MatchCase = MatchCase, IsRegex = IsRegex, RegexGroups = RegexGroups, SelectionOnly = SelectionOnly, EntireSelection = EntireSelection, KeepMatching = KeepMatching, RemoveMatching = RemoveMatching, MultiLine = MultiLine };

			wholeWordsVal = WholeWords;
			matchCaseVal = MatchCase;
			isRegexVal = IsRegex;
			regexGroupsVal = RegexGroups;
			multiLineVal = MultiLine;

			text.AddCurrentSuggestion();

			DialogResult = true;
		}

		static public Result Run(Window parent, NEVariables variables, bool selectionOnly)
		{
			var dialog = new MultiFindDialog(variables, selectionOnly) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}

		void RegExHelp(object sender, RoutedEventArgs e) => RegExHelpDialog.Display();
	}
}
