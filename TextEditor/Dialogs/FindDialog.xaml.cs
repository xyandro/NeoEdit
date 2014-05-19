﻿using System;
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

			wholeWords.IsChecked = wholeWordsVal;
			matchCase.IsChecked = matchCaseVal;
			regularExpression.IsChecked = regularExpressionVal;
			includeEndings.IsChecked = includeEndingsVal;

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

			wholeWordsVal = wholeWords.IsChecked == true;
			matchCaseVal = matchCase.IsChecked == true;
			regularExpressionVal = regularExpression.IsChecked == true;
			includeEndingsVal = includeEndings.IsChecked == true;

			var text = Text;
			History.Remove(text);
			History.Insert(0, text);
			Text = text;

			if (regularExpression.IsChecked == false)
				text = Regex.Escape(text);
			if (wholeWords.IsChecked == true)
				text = @"\b" + text + @"\b";
			var options = RegexOptions.Compiled | RegexOptions.Singleline;
			if (matchCase.IsChecked == false)
				options |= RegexOptions.IgnoreCase;
			Regex = new Regex(text, options);
			SelectAll = sender == selectAll;

			DialogResult = true;
		}
	}
}
