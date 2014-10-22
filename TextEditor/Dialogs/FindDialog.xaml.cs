using System;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using NeoEdit.GUI.Common;

namespace NeoEdit.TextEditor.Dialogs
{
	internal partial class FindDialog
	{
		internal class Result
		{
			public Regex Regex { get; set; }
			public bool SelectAll { get; set; }
			public bool SelectionOnly { get; set; }
			public bool IncludeEndings { get; set; }
			public bool RegexGroups { get; set; }
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
		public bool IncludeEndings { get { return UIHelper<FindDialog>.GetPropValue<bool>(this); } set { UIHelper<FindDialog>.SetPropValue(this, value); } }
		[DepProp]
		public ObservableCollection<string> History { get { return UIHelper<FindDialog>.GetPropValue<ObservableCollection<string>>(this); } set { UIHelper<FindDialog>.SetPropValue(this, value); } }

		public Result result { get; private set; }

		static ObservableCollection<string> StaticHistory = new ObservableCollection<string>();
		static bool wholeWordsVal, matchCaseVal, isRegexVal, regexGroupsVal, includeEndingsVal;

		static FindDialog()
		{
			UIHelper<FindDialog>.Register();
			UIHelper<FindDialog>.AddCallback(a => a.IsRegex, (obj, o, n) => { if (!obj.IsRegex) obj.RegexGroups = false; });
			UIHelper<FindDialog>.AddCallback(a => a.RegexGroups, (obj, o, n) => { if (obj.RegexGroups) obj.IsRegex = true; });
		}

		FindDialog()
		{
			History = StaticHistory;
			InitializeComponent();

			WholeWords = wholeWordsVal;
			MatchCase = matchCaseVal;
			RegexGroups = regexGroupsVal;
			IsRegex = isRegexVal;
			IncludeEndings = includeEndingsVal;

			Loaded += (s, e) =>
			{
				if ((Text == null) && (History.Count != 0))
					findText.SelectedIndex = 0;
				var textbox = findText.Template.FindName("PART_EditableTextBox", findText) as TextBox;
				textbox.AcceptsTab = true;
			};

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
			result = new Result { Regex = new Regex(text, options), SelectAll = sender == selectAll, SelectionOnly = SelectionOnly, IncludeEndings = IncludeEndings, RegexGroups = RegexGroups };

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

		static public Result Run(string text, bool selectionOnly)
		{
			var dialog = new FindDialog { Text = text, SelectionOnly = selectionOnly };
			return dialog.ShowDialog() == true ? dialog.result : null;
		}
	}
}
