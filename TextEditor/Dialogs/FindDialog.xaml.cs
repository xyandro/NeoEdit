using System;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using NeoEdit.GUI.Common;

namespace NeoEdit.TextEditor.Dialogs
{
	public partial class FindDialog : Window
	{
		public class Result
		{
			public Regex Regex { get; private set; }
			public bool SelectAll { get; private set; }
			public bool SelectionOnly { get; private set; }
			public bool IncludeEndings { get; private set; }
			public bool RegexGroups { get; private set; }

			public Result(Regex regex, bool selectAll, bool selectionOnly, bool includeEndings, bool regexGroups)
			{
				Regex = regex;
				SelectAll = selectAll;
				SelectionOnly = selectionOnly;
				IncludeEndings = includeEndings;
				RegexGroups = regexGroups;
			}
		}

		[DepProp]
		public string Text { get { return uiHelper.GetPropValue<string>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public bool WholeWords { get { return uiHelper.GetPropValue<bool>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public bool MatchCase { get { return uiHelper.GetPropValue<bool>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public bool IsRegex { get { return uiHelper.GetPropValue<bool>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public bool RegexGroups { get { return uiHelper.GetPropValue<bool>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public bool SelectionOnly { get { return uiHelper.GetPropValue<bool>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public bool IncludeEndings { get { return uiHelper.GetPropValue<bool>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public ObservableCollection<string> History { get { return uiHelper.GetPropValue<ObservableCollection<string>>(); } set { uiHelper.SetPropValue(value); } }

		public Result result { get; private set; }

		static ObservableCollection<string> StaticHistory = new ObservableCollection<string>();
		static bool wholeWordsVal, matchCaseVal, isRegexVal, regexGroupsVal, includeEndingsVal;

		static FindDialog() { UIHelper<FindDialog>.Register(); }

		readonly UIHelper<FindDialog> uiHelper;
		FindDialog()
		{
			uiHelper = new UIHelper<FindDialog>(this);
			History = StaticHistory;
			InitializeComponent();

			uiHelper.AddCallback(a => a.IsRegex, (o, n) => { if (!IsRegex) RegexGroups = false; });
			uiHelper.AddCallback(a => a.RegexGroups, (o, n) => { if (RegexGroups) IsRegex = true; });

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
			result = new Result(new Regex(text, options), sender == selectAll, SelectionOnly, IncludeEndings, RegexGroups);

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
			if (dialog.ShowDialog() != true)
				return null;
			return dialog.result;
		}
	}
}
