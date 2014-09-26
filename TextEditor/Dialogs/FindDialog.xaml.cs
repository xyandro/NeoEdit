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
		[DepProp]
		public string Text { get { return uiHelper.GetPropValue<string>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public bool WholeWords { get { return uiHelper.GetPropValue<bool>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public bool MatchCase { get { return uiHelper.GetPropValue<bool>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public bool RegularExpression { get { return uiHelper.GetPropValue<bool>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public bool SelectionOnly { get { return uiHelper.GetPropValue<bool>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public bool IncludeEndings { get { return uiHelper.GetPropValue<bool>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public ObservableCollection<string> History { get { return uiHelper.GetPropValue<ObservableCollection<string>>(); } set { uiHelper.SetPropValue(value); } }

		public Regex Regex { get; private set; }
		public bool SelectAll { get; private set; }

		static ObservableCollection<string> StaticHistory = new ObservableCollection<string>();
		static bool wholeWordsVal, matchCaseVal, regularExpressionVal, includeEndingsVal;

		static FindDialog() { UIHelper<FindDialog>.Register(); }

		readonly UIHelper<FindDialog> uiHelper;
		public FindDialog()
		{
			uiHelper = new UIHelper<FindDialog>(this);
			History = StaticHistory;
			InitializeComponent();

			WholeWords = wholeWordsVal;
			MatchCase = matchCaseVal;
			RegularExpression = regularExpressionVal;
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

			wholeWordsVal = WholeWords == true;
			matchCaseVal = MatchCase == true;
			regularExpressionVal = RegularExpression == true;
			includeEndingsVal = IncludeEndings == true;

			var text = Text;
			History.Remove(text);
			History.Insert(0, text);
			Text = text;

			if (RegularExpression == false)
				text = Regex.Escape(text);
			if (WholeWords == true)
				text = @"\b" + text + @"\b";
			var options = RegexOptions.Compiled | RegexOptions.Singleline;
			if (MatchCase == false)
				options |= RegexOptions.IgnoreCase;
			Regex = new Regex(text, options);
			SelectAll = sender == selectAll;

			DialogResult = true;
		}
	}
}
