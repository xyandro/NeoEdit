﻿using System;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Windows;
using NeoEdit.GUI.Controls;

namespace NeoEdit.GUI.Dialogs
{
	partial class FindTextDialog
	{
		public enum FindTextType
		{
			Single,
			Selections,
			Replace,
		}

		public enum GetRegExResultType
		{
			Next,
			All,
		}

		public class Result
		{
			public Regex Regex { get; set; }
			public string Replace { get; set; }
			public bool RegexGroups { get; set; }
			public bool SelectionOnly { get; set; }
			public bool KeepMatching { get; set; }
			public bool RemoveMatching { get; set; }
			public bool IncludeEndings { get; set; }
			public GetRegExResultType ResultType { get; set; }
		}

		[DepProp]
		public string Text { get { return UIHelper<FindTextDialog>.GetPropValue<string>(this); } set { UIHelper<FindTextDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string Replace { get { return UIHelper<FindTextDialog>.GetPropValue<string>(this); } set { UIHelper<FindTextDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool WholeWords { get { return UIHelper<FindTextDialog>.GetPropValue<bool>(this); } set { UIHelper<FindTextDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool MatchCase { get { return UIHelper<FindTextDialog>.GetPropValue<bool>(this); } set { UIHelper<FindTextDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool IsRegex { get { return UIHelper<FindTextDialog>.GetPropValue<bool>(this); } set { UIHelper<FindTextDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool RegexGroups { get { return UIHelper<FindTextDialog>.GetPropValue<bool>(this); } set { UIHelper<FindTextDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool SelectionOnly { get { return UIHelper<FindTextDialog>.GetPropValue<bool>(this); } set { UIHelper<FindTextDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool KeepMatching { get { return UIHelper<FindTextDialog>.GetPropValue<bool>(this); } set { UIHelper<FindTextDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool RemoveMatching { get { return UIHelper<FindTextDialog>.GetPropValue<bool>(this); } set { UIHelper<FindTextDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool IncludeEndings { get { return UIHelper<FindTextDialog>.GetPropValue<bool>(this); } set { UIHelper<FindTextDialog>.SetPropValue(this, value); } }
		[DepProp]
		public ObservableCollection<string> History { get { return UIHelper<FindTextDialog>.GetPropValue<ObservableCollection<string>>(this); } set { UIHelper<FindTextDialog>.SetPropValue(this, value); } }
		[DepProp]
		public ObservableCollection<string> ReplaceHistory { get { return UIHelper<FindTextDialog>.GetPropValue<ObservableCollection<string>>(this); } set { UIHelper<FindTextDialog>.SetPropValue(this, value); } }

		readonly static ObservableCollection<string> StaticHistory = new ObservableCollection<string>();
		readonly static ObservableCollection<string> StaticReplaceHistory = new ObservableCollection<string>();
		static bool wholeWordsVal, matchCaseVal, isRegexVal, regexGroupsVal, includeEndingsVal;

		static FindTextDialog()
		{
			UIHelper<FindTextDialog>.Register();
			UIHelper<FindTextDialog>.AddCallback(a => a.IsRegex, (obj, o, n) => { if (!obj.IsRegex) obj.RegexGroups = false; });
			UIHelper<FindTextDialog>.AddCallback(a => a.RegexGroups, (obj, o, n) => { if (obj.RegexGroups) obj.IsRegex = true; });
			UIHelper<FindTextDialog>.AddCallback(a => a.SelectionOnly, (obj, o, n) => { if (!obj.SelectionOnly) obj.KeepMatching = obj.RemoveMatching = false; });
			UIHelper<FindTextDialog>.AddCallback(a => a.KeepMatching, (obj, o, n) => { if (obj.KeepMatching) { obj.SelectionOnly = true; obj.RemoveMatching = false; } });
			UIHelper<FindTextDialog>.AddCallback(a => a.RemoveMatching, (obj, o, n) => { if (obj.RemoveMatching) { obj.SelectionOnly = true; obj.KeepMatching = false; } });
		}

		FindTextDialog(FindTextType findType, string _Text, bool _SelectionOnly)
		{
			InitializeComponent();

			History = StaticHistory;
			ReplaceHistory = StaticReplaceHistory;
			Text = _Text ?? "";
			Replace = "";
			SelectionOnly = _SelectionOnly;
			WholeWords = wholeWordsVal;
			MatchCase = matchCaseVal;
			RegexGroups = regexGroupsVal;
			IsRegex = isRegexVal;
			IncludeEndings = includeEndingsVal;

			if ((String.IsNullOrEmpty(Text)) && (History.Count != 0))
				Text = History[0];

			switch (findType)
			{
				case FindTextType.Single:
					byGroup.Visibility = selectionOnly.Visibility = keepMatching.Visibility = removeMatching.Visibility = selectAll.Visibility = Visibility.Collapsed;
					goto case FindTextType.Selections;
				case FindTextType.Selections:
					replaceLabel.Visibility = replace.Visibility = replaceButton.Visibility = Visibility.Collapsed;
					break;
				case FindTextType.Replace:
					if (ReplaceHistory.Count != 0)
						Replace = ReplaceHistory[0];
					byGroup.Visibility = find.Visibility = selectAll.Visibility = keepMatching.Visibility = removeMatching.Visibility = Visibility.Collapsed;
					RegexGroups = false;
					break;
			}
		}

		void Escape(object sender, RoutedEventArgs e)
		{
			Text = Regex.Escape(Text);
		}

		void Unescape(object sender, RoutedEventArgs e)
		{
			Text = Regex.Unescape(Text);
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if (String.IsNullOrEmpty(Text))
				return;

			var text = Text;
			var replace = Replace;
			if (!IsRegex)
			{
				text = Regex.Escape(text);
				replace = replace.Replace("$", "$$");
			}
			if (WholeWords)
				text = @"\b" + text + @"\b";
			var options = RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.Multiline;
			if (!MatchCase)
				options |= RegexOptions.IgnoreCase;
			result = new Result { Regex = new Regex(text, options), Replace = replace, RegexGroups = RegexGroups, SelectionOnly = SelectionOnly, KeepMatching = KeepMatching, RemoveMatching = RemoveMatching, IncludeEndings = IncludeEndings, ResultType = sender == selectAll ? GetRegExResultType.All : GetRegExResultType.Next };

			wholeWordsVal = WholeWords;
			matchCaseVal = MatchCase;
			isRegexVal = IsRegex;
			regexGroupsVal = RegexGroups;
			includeEndingsVal = IncludeEndings;

			text = Text;
			History.Remove(text);
			History.Insert(0, text);
			if (!String.IsNullOrEmpty(replace))
			{
				ReplaceHistory.Remove(replace);
				ReplaceHistory.Insert(0, replace);
			}

			DialogResult = true;
		}

		static public Result Run(Window parent, FindTextType findType, string text = null, bool selectionOnly = false)
		{
			var dialog = new FindTextDialog(findType, text, selectionOnly) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}

		void RegExHelp(object sender, RoutedEventArgs e)
		{
			RegExHelpDialog.Display();
		}
	}
}