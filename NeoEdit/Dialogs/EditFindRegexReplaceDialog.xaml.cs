﻿using System.Text.RegularExpressions;
using System.Windows;
using NeoEdit.Program.Controls;

namespace NeoEdit.Program.Dialogs
{
	partial class EditFindRegexReplaceDialog
	{
		class CheckBoxStatus
		{
			public bool WholeWords { get; set; }
			public bool MatchCase { get; set; }
			public bool EntireSelection { get; set; }
		}

		public class Result
		{
			public string Text { get; set; }
			public string Replace { get; set; }
			public bool WholeWords { get; set; }
			public bool MatchCase { get; set; }
			public bool SelectionOnly { get; set; }
			public bool EntireSelection { get; set; }
		}

		[DepProp]
		public string Text { get { return UIHelper<EditFindRegexReplaceDialog>.GetPropValue<string>(this); } set { UIHelper<EditFindRegexReplaceDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string Replace { get { return UIHelper<EditFindRegexReplaceDialog>.GetPropValue<string>(this); } set { UIHelper<EditFindRegexReplaceDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool WholeWords { get { return UIHelper<EditFindRegexReplaceDialog>.GetPropValue<bool>(this); } set { UIHelper<EditFindRegexReplaceDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool MatchCase { get { return UIHelper<EditFindRegexReplaceDialog>.GetPropValue<bool>(this); } set { UIHelper<EditFindRegexReplaceDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool SelectionOnly { get { return UIHelper<EditFindRegexReplaceDialog>.GetPropValue<bool>(this); } set { UIHelper<EditFindRegexReplaceDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool EntireSelection { get { return UIHelper<EditFindRegexReplaceDialog>.GetPropValue<bool>(this); } set { UIHelper<EditFindRegexReplaceDialog>.SetPropValue(this, value); } }

		static EditFindRegexReplaceDialog()
		{
			UIHelper<EditFindRegexReplaceDialog>.Register();
			UIHelper<EditFindRegexReplaceDialog>.AddCallback(a => a.SelectionOnly, (obj, o, n) => { if (!obj.SelectionOnly) obj.EntireSelection = false; });
			UIHelper<EditFindRegexReplaceDialog>.AddCallback(a => a.EntireSelection, (obj, o, n) => { if (obj.EntireSelection) obj.SelectionOnly = true; });
		}

		EditFindRegexReplaceDialog(string text, bool selectionOnly)
		{
			InitializeComponent();

			SelectionOnly = selectionOnly;
			Text = text.CoalesceNullOrEmpty(this.text.GetLastSuggestion(), "");
			Replace = "";
			SetCheckBoxStatus(this.text.GetLastSuggestionData() as CheckBoxStatus);
		}

		CheckBoxStatus GetCheckBoxStatus()
		{
			return new CheckBoxStatus
			{
				WholeWords = WholeWords,
				MatchCase = MatchCase,
				EntireSelection = EntireSelection,
			};
		}

		void SetCheckBoxStatus(CheckBoxStatus checkBoxStatus)
		{
			if (checkBoxStatus == null)
				return;

			WholeWords = checkBoxStatus.WholeWords;
			MatchCase = checkBoxStatus.MatchCase;
			EntireSelection = checkBoxStatus.EntireSelection;
		}

		void OnAcceptSuggestion(string text, object data) => SetCheckBoxStatus(data as CheckBoxStatus);

		void Escape(object sender, RoutedEventArgs e) => Text = Regex.Escape(Text);
		void Unescape(object sender, RoutedEventArgs e) => Text = Regex.Unescape(Text);

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrEmpty(Text))
				return;

			result = new Result { Text = Text, Replace = Replace, WholeWords = WholeWords, MatchCase = MatchCase, SelectionOnly = SelectionOnly, EntireSelection = EntireSelection };
			text.AddCurrentSuggestion(GetCheckBoxStatus());
			replace.AddCurrentSuggestion();

			DialogResult = true;
		}

		void RegExHelp(object sender, RoutedEventArgs e) => RegExHelpDialog.Display();

		void Reset(object sender, RoutedEventArgs e) => WholeWords = MatchCase = SelectionOnly = EntireSelection = false;

		static public Result Run(Window parent, string text, bool selectionOnly)
		{
			var dialog = new EditFindRegexReplaceDialog(text, selectionOnly) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
