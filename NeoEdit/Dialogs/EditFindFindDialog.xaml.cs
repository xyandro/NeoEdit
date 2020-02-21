using System;
using System.Text.RegularExpressions;
using System.Windows;
using NeoEdit.Program.Controls;

namespace NeoEdit.Program.Dialogs
{
	partial class EditFindFindDialog
	{
		class CheckBoxStatus
		{
			public bool WholeWords { get; set; }
			public bool MatchCase { get; set; }
			public bool IsRegex { get; set; }
			public bool RegexGroups { get; set; }
			public bool EntireSelection { get; set; }
			public bool KeepMatching { get; set; }
			public bool RemoveMatching { get; set; }
			public bool AddMatches { get; set; }
		}

		public enum ResultType
		{
			None,
			CopyCount,
			FindNext,
			FindAll,
		}

		public class Result
		{
			public string Text { get; set; }
			public bool WholeWords { get; set; }
			public bool MatchCase { get; set; }
			public bool IsRegex { get; set; }
			public bool RegexGroups { get; set; }
			public bool SelectionOnly { get; set; }
			public bool EntireSelection { get; set; }
			public bool KeepMatching { get; set; }
			public bool RemoveMatching { get; set; }
			public bool AddMatches { get; set; }
			public ResultType Type { get; set; }
		}

		[DepProp]
		public string Text { get { return UIHelper<EditFindFindDialog>.GetPropValue<string>(this); } set { UIHelper<EditFindFindDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool WholeWords { get { return UIHelper<EditFindFindDialog>.GetPropValue<bool>(this); } set { UIHelper<EditFindFindDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool MatchCase { get { return UIHelper<EditFindFindDialog>.GetPropValue<bool>(this); } set { UIHelper<EditFindFindDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool IsRegex { get { return UIHelper<EditFindFindDialog>.GetPropValue<bool>(this); } set { UIHelper<EditFindFindDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool RegexGroups { get { return UIHelper<EditFindFindDialog>.GetPropValue<bool>(this); } set { UIHelper<EditFindFindDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool SelectionOnly { get { return UIHelper<EditFindFindDialog>.GetPropValue<bool>(this); } set { UIHelper<EditFindFindDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool EntireSelection { get { return UIHelper<EditFindFindDialog>.GetPropValue<bool>(this); } set { UIHelper<EditFindFindDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool KeepMatching { get { return UIHelper<EditFindFindDialog>.GetPropValue<bool>(this); } set { UIHelper<EditFindFindDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool RemoveMatching { get { return UIHelper<EditFindFindDialog>.GetPropValue<bool>(this); } set { UIHelper<EditFindFindDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool AddMatches { get { return UIHelper<EditFindFindDialog>.GetPropValue<bool>(this); } set { UIHelper<EditFindFindDialog>.SetPropValue(this, value); } }

		static EditFindFindDialog()
		{
			UIHelper<EditFindFindDialog>.Register();
			UIHelper<EditFindFindDialog>.AddCallback(a => a.IsRegex, (obj, o, n) => { if (!obj.IsRegex) obj.RegexGroups = false; });
			UIHelper<EditFindFindDialog>.AddCallback(a => a.RegexGroups, (obj, o, n) => { if (obj.RegexGroups) obj.IsRegex = true; });
			UIHelper<EditFindFindDialog>.AddCallback(a => a.SelectionOnly, (obj, o, n) => { if (!obj.SelectionOnly) obj.EntireSelection = obj.KeepMatching = obj.RemoveMatching = false; else obj.AddMatches = false; });
			UIHelper<EditFindFindDialog>.AddCallback(a => a.EntireSelection, (obj, o, n) => { if (obj.EntireSelection) obj.SelectionOnly = true; });
			UIHelper<EditFindFindDialog>.AddCallback(a => a.KeepMatching, (obj, o, n) => { if (obj.KeepMatching) { obj.SelectionOnly = true; obj.RemoveMatching = false; } });
			UIHelper<EditFindFindDialog>.AddCallback(a => a.RemoveMatching, (obj, o, n) => { if (obj.RemoveMatching) { obj.SelectionOnly = true; obj.KeepMatching = false; } });
			UIHelper<EditFindFindDialog>.AddCallback(a => a.AddMatches, (obj, o, n) => { if (obj.AddMatches) { obj.SelectionOnly = false; } });
		}

		EditFindFindDialog(string text, bool selectionOnly)
		{
			InitializeComponent();

			SelectionOnly = selectionOnly;
			Text = text.CoalesceNullOrEmpty(this.text.GetLastSuggestion(), "");
			SetCheckBoxStatus(this.text.GetLastSuggestionData() as CheckBoxStatus);
		}

		CheckBoxStatus GetCheckBoxStatus()
		{
			return new CheckBoxStatus
			{
				WholeWords = WholeWords,
				MatchCase = MatchCase,
				IsRegex = IsRegex,
				RegexGroups = RegexGroups,
				EntireSelection = EntireSelection,
				KeepMatching = KeepMatching,
				RemoveMatching = RemoveMatching,
				AddMatches = AddMatches,
			};
		}

		void SetCheckBoxStatus(CheckBoxStatus checkBoxStatus)
		{
			if (checkBoxStatus == null)
				return;

			WholeWords = checkBoxStatus.WholeWords;
			MatchCase = checkBoxStatus.MatchCase;
			IsRegex = checkBoxStatus.IsRegex;
			RegexGroups = checkBoxStatus.RegexGroups;
			EntireSelection = checkBoxStatus.EntireSelection;
			KeepMatching = checkBoxStatus.KeepMatching;
			RemoveMatching = checkBoxStatus.RemoveMatching;
			AddMatches = checkBoxStatus.AddMatches;
		}

		void OnAcceptSuggestion(string text, object data) => SetCheckBoxStatus(data as CheckBoxStatus);

		void Escape(object sender, RoutedEventArgs e) => Text = Regex.Escape(Text);
		void Unescape(object sender, RoutedEventArgs e) => Text = Regex.Unescape(Text);

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrEmpty(Text))
				return;

			result = new Result { Text = Text, WholeWords = WholeWords, MatchCase = MatchCase, IsRegex = IsRegex, RegexGroups = RegexGroups, SelectionOnly = SelectionOnly, EntireSelection = EntireSelection, KeepMatching = KeepMatching, RemoveMatching = RemoveMatching, AddMatches = AddMatches };
			if (sender == copyCount)
				result.Type = ResultType.CopyCount;
			else if (sender == findNext)
				result.Type = ResultType.FindNext;
			else if (sender == findAll)
				result.Type = ResultType.FindAll;
			else
				throw new Exception("Invalid search type");

			text.AddCurrentSuggestion(GetCheckBoxStatus());

			DialogResult = true;
		}

		void RegExHelp(object sender, RoutedEventArgs e) => RegExHelpDialog.Display();

		void Reset(object sender, RoutedEventArgs e) => WholeWords = MatchCase = IsRegex = RegexGroups = SelectionOnly = EntireSelection = KeepMatching = RemoveMatching = AddMatches = false;

		static public Result Run(Window parent, string text, bool selectionOnly)
		{
			var dialog = new EditFindFindDialog(text, selectionOnly) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
