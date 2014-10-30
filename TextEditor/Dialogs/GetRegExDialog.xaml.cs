using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Windows;
using NeoEdit.GUI.Common;

namespace NeoEdit.TextEditor.Dialogs
{
	partial class GetRegExDialog
	{
		internal enum GetRegExDialogType
		{
			Find,
			MatchSelections,
		}

		internal enum GetRegExResultType
		{
			Next,
			All,
		}

		internal class Result
		{
			public Regex Regex { get; set; }
			public bool RegexGroups { get; set; }
			public bool SelectionOnly { get; set; }
			public bool IncludeEndings { get; set; }
			public bool IncludeMatches { get; set; }
			public GetRegExResultType ResultType { get; set; }
		}

		[DepProp]
		public string Text { get { return UIHelper<GetRegExDialog>.GetPropValue<string>(this); } set { UIHelper<GetRegExDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool WholeWords { get { return UIHelper<GetRegExDialog>.GetPropValue<bool>(this); } set { UIHelper<GetRegExDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool MatchCase { get { return UIHelper<GetRegExDialog>.GetPropValue<bool>(this); } set { UIHelper<GetRegExDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool IsRegex { get { return UIHelper<GetRegExDialog>.GetPropValue<bool>(this); } set { UIHelper<GetRegExDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool RegexGroups { get { return UIHelper<GetRegExDialog>.GetPropValue<bool>(this); } set { UIHelper<GetRegExDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool SelectionOnly { get { return UIHelper<GetRegExDialog>.GetPropValue<bool>(this); } set { UIHelper<GetRegExDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool IncludeEndings { get { return UIHelper<GetRegExDialog>.GetPropValue<bool>(this); } set { UIHelper<GetRegExDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool IncludeMatches { get { return UIHelper<GetRegExDialog>.GetPropValue<bool>(this); } set { UIHelper<GetRegExDialog>.SetPropValue(this, value); } }
		[DepProp]
		public ObservableCollection<string> History { get { return UIHelper<GetRegExDialog>.GetPropValue<ObservableCollection<string>>(this); } set { UIHelper<GetRegExDialog>.SetPropValue(this, value); } }

		public Result result { get; private set; }

		readonly static Dictionary<GetRegExDialogType, ObservableCollection<string>> StaticHistory = new Dictionary<GetRegExDialogType, ObservableCollection<string>>();
		static bool wholeWordsVal, matchCaseVal, isRegexVal, regexGroupsVal, includeEndingsVal;

		static GetRegExDialog()
		{
			UIHelper<GetRegExDialog>.Register();
			UIHelper<GetRegExDialog>.AddCallback(a => a.IsRegex, (obj, o, n) => { if (!obj.IsRegex) obj.RegexGroups = false; });
			UIHelper<GetRegExDialog>.AddCallback(a => a.RegexGroups, (obj, o, n) => { if (obj.RegexGroups) obj.IsRegex = true; });
		}

		GetRegExDialog(GetRegExDialogType type, string _Text, bool _SelectionOnly)
		{
			InitializeComponent();

			if (!StaticHistory.ContainsKey(type))
				StaticHistory[type] = new ObservableCollection<string>();
			History = StaticHistory[type];
			Text = _Text;
			SelectionOnly = _SelectionOnly;
			WholeWords = wholeWordsVal;
			MatchCase = matchCaseVal;
			RegexGroups = regexGroupsVal;
			IsRegex = isRegexVal;
			IncludeEndings = includeEndingsVal;
			IncludeMatches = true;

			switch (type)
			{
				case GetRegExDialogType.Find:
					if ((Text == null) && (History.Count != 0))
						Text = History[0];
					includeMatches.Visibility = Visibility.Collapsed;
					break;
				case GetRegExDialogType.MatchSelections:
					byGroup.Visibility = selectionOnly.Visibility = includeLineEndings.Visibility = selectAll.Visibility = Visibility.Collapsed;
					break;
			}
		}

		void OkClick(object sender, RoutedEventArgs e)
		{
			if (String.IsNullOrEmpty(Text))
				return;

			var text = Text;
			if (!IsRegex)
				text = Regex.Escape(text);
			if (WholeWords)
				text = @"\b" + text + @"\b";
			var options = RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.Multiline;
			if (!MatchCase)
				options |= RegexOptions.IgnoreCase;
			result = new Result { Regex = new Regex(text, options), RegexGroups = RegexGroups, SelectionOnly = SelectionOnly, IncludeEndings = IncludeEndings, IncludeMatches = IncludeMatches, ResultType = sender == selectAll ? GetRegExResultType.All : GetRegExResultType.Next };

			wholeWordsVal = WholeWords;
			matchCaseVal = MatchCase;
			isRegexVal = IsRegex;
			regexGroupsVal = RegexGroups;
			includeEndingsVal = IncludeEndings;

			text = Text;
			History.Remove(text);
			History.Insert(0, text);

			DialogResult = true;
		}

		static public Result Run(GetRegExDialogType type, string text = null, bool selectionOnly = false)
		{
			var dialog = new GetRegExDialog(type, text, selectionOnly);
			return dialog.ShowDialog() == true ? dialog.result : null;
		}

		void RegExHelp(object sender, RoutedEventArgs e)
		{
			RegExHelpDialog.Display();
		}
	}
}
