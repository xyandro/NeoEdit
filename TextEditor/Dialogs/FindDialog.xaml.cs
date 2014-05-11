using System;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Windows;
using NeoEdit.GUI.Common;

namespace NeoEdit.TextEditor.Dialogs
{
	public partial class FindDialog : Window
	{
		static ObservableCollection<string> StaticHistory = new ObservableCollection<string>();

		[DepProp]
		public string Text { get { return uiHelper.GetPropValue<string>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public bool SelectionOnly { get { return uiHelper.GetPropValue<bool>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public ObservableCollection<string> History { get { return uiHelper.GetPropValue<ObservableCollection<string>>(); } set { uiHelper.SetPropValue(value); } }

		public Regex Regex { get; private set; }
		public bool SelectAll { get; private set; }

		static bool wholeWordsVal, matchCaseVal, regularExpressionVal;

		static FindDialog() { UIHelper<FindDialog>.Register(); }

		readonly UIHelper<FindDialog> uiHelper;
		public FindDialog()
		{
			uiHelper = new UIHelper<FindDialog>(this);
			History = StaticHistory;
			InitializeComponent();

			wholeWords.IsChecked = wholeWordsVal;
			matchCase.IsChecked = matchCaseVal;
			regularExpression.IsChecked = regularExpressionVal;

			Loaded += (s, e) => { if ((Text == null) && (History.Count != 0)) findText.SelectedIndex = 0; };
		}

		void OkClick(object sender, RoutedEventArgs e)
		{
			if (String.IsNullOrEmpty(Text))
				return;

			wholeWordsVal = wholeWords.IsChecked == true;
			matchCaseVal = matchCase.IsChecked == true;
			regularExpressionVal = regularExpression.IsChecked == true;

			var text = Text;
			History.Remove(text);
			History.Insert(0, text);
			Text = text;

			if (regularExpression.IsChecked == false)
				text = Regex.Escape(text);
			if (wholeWords.IsChecked == true)
				text = @"\b" + text + @"\b";
			var options = RegexOptions.None;
			if (matchCase.IsChecked == false)
				options = RegexOptions.IgnoreCase;
			Regex = new Regex(text, options);
			SelectAll = sender == selectAll;

			DialogResult = true;
		}
	}
}
