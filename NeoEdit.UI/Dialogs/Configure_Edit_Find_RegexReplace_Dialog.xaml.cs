using System;
using System.Text.RegularExpressions;
using System.Windows;
using NeoEdit.Common;
using NeoEdit.Common.Configuration;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Configure_Edit_Find_RegexReplace_Dialog
	{
		class CheckBoxStatus
		{
			public bool WholeWords { get; set; }
			public bool MatchCase { get; set; }
			public bool EntireSelection { get; set; }
		}

		[DepProp]
		public string Text { get { return UIHelper<Configure_Edit_Find_RegexReplace_Dialog>.GetPropValue<string>(this); } set { UIHelper<Configure_Edit_Find_RegexReplace_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public string Replace { get { return UIHelper<Configure_Edit_Find_RegexReplace_Dialog>.GetPropValue<string>(this); } set { UIHelper<Configure_Edit_Find_RegexReplace_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool WholeWords { get { return UIHelper<Configure_Edit_Find_RegexReplace_Dialog>.GetPropValue<bool>(this); } set { UIHelper<Configure_Edit_Find_RegexReplace_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool MatchCase { get { return UIHelper<Configure_Edit_Find_RegexReplace_Dialog>.GetPropValue<bool>(this); } set { UIHelper<Configure_Edit_Find_RegexReplace_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool SelectionOnly { get { return UIHelper<Configure_Edit_Find_RegexReplace_Dialog>.GetPropValue<bool>(this); } set { UIHelper<Configure_Edit_Find_RegexReplace_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool EntireSelection { get { return UIHelper<Configure_Edit_Find_RegexReplace_Dialog>.GetPropValue<bool>(this); } set { UIHelper<Configure_Edit_Find_RegexReplace_Dialog>.SetPropValue(this, value); } }

		static Configure_Edit_Find_RegexReplace_Dialog()
		{
			UIHelper<Configure_Edit_Find_RegexReplace_Dialog>.Register();
			UIHelper<Configure_Edit_Find_RegexReplace_Dialog>.AddCallback(a => a.SelectionOnly, (obj, o, n) => { if (!obj.SelectionOnly) obj.EntireSelection = false; });
			UIHelper<Configure_Edit_Find_RegexReplace_Dialog>.AddCallback(a => a.EntireSelection, (obj, o, n) => { if (obj.EntireSelection) obj.SelectionOnly = true; });
		}

		Configure_Edit_Find_RegexReplace_Dialog(string text, bool selectionOnly)
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

		Configuration_Edit_Find_RegexReplace result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrEmpty(Text))
				return;

			result = new Configuration_Edit_Find_RegexReplace { Text = Text, Replace = Replace, WholeWords = WholeWords, MatchCase = MatchCase, SelectionOnly = SelectionOnly, EntireSelection = EntireSelection };
			text.AddCurrentSuggestion(GetCheckBoxStatus());
			replace.AddCurrentSuggestion();

			DialogResult = true;
		}

		void RegExHelp(object sender, RoutedEventArgs e) => RegExHelpDialog.Display();

		void Reset(object sender, RoutedEventArgs e) => WholeWords = MatchCase = SelectionOnly = EntireSelection = false;

		public static Configuration_Edit_Find_RegexReplace Run(Window parent, string text, bool selectionOnly)
		{
			var dialog = new Configure_Edit_Find_RegexReplace_Dialog(text, selectionOnly) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
