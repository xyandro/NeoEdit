using System;
using System.Text.RegularExpressions;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Text_Find_RegexReplace_Dialog
	{
		class CheckBoxStatus
		{
			public bool WholeWords { get; set; }
			public bool MatchCase { get; set; }
			public bool EntireSelection { get; set; }
		}

		[DepProp]
		public string Text { get { return UIHelper<Text_Find_RegexReplace_Dialog>.GetPropValue<string>(this); } set { UIHelper<Text_Find_RegexReplace_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public string Replace { get { return UIHelper<Text_Find_RegexReplace_Dialog>.GetPropValue<string>(this); } set { UIHelper<Text_Find_RegexReplace_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool WholeWords { get { return UIHelper<Text_Find_RegexReplace_Dialog>.GetPropValue<bool>(this); } set { UIHelper<Text_Find_RegexReplace_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool MatchCase { get { return UIHelper<Text_Find_RegexReplace_Dialog>.GetPropValue<bool>(this); } set { UIHelper<Text_Find_RegexReplace_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool SelectionOnly { get { return UIHelper<Text_Find_RegexReplace_Dialog>.GetPropValue<bool>(this); } set { UIHelper<Text_Find_RegexReplace_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool EntireSelection { get { return UIHelper<Text_Find_RegexReplace_Dialog>.GetPropValue<bool>(this); } set { UIHelper<Text_Find_RegexReplace_Dialog>.SetPropValue(this, value); } }

		static Text_Find_RegexReplace_Dialog()
		{
			UIHelper<Text_Find_RegexReplace_Dialog>.Register();
			UIHelper<Text_Find_RegexReplace_Dialog>.AddCallback(a => a.SelectionOnly, (obj, o, n) => { if (!obj.SelectionOnly) obj.EntireSelection = false; });
			UIHelper<Text_Find_RegexReplace_Dialog>.AddCallback(a => a.EntireSelection, (obj, o, n) => { if (obj.EntireSelection) obj.SelectionOnly = true; });
		}

		Text_Find_RegexReplace_Dialog(string text)
		{
			InitializeComponent();

			Text = text ?? "";
			Replace = "";
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

		Configuration_Text_Find_RegexReplace result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrEmpty(Text))
				return;

			result = new Configuration_Text_Find_RegexReplace { Text = Text, Replace = Replace, WholeWords = WholeWords, MatchCase = MatchCase, SelectionOnly = SelectionOnly, EntireSelection = EntireSelection };
			text.AddCurrentSuggestion(GetCheckBoxStatus());
			replace.AddCurrentSuggestion();

			DialogResult = true;
		}

		void RegExHelp(object sender, RoutedEventArgs e) => RegExHelpDialog.Display();

		void Reset(object sender, RoutedEventArgs e) => WholeWords = MatchCase = SelectionOnly = EntireSelection = false;

		public static Configuration_Text_Find_RegexReplace Run(Window parent, string text)
		{
			var dialog = new Text_Find_RegexReplace_Dialog(text) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
