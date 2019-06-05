using System.Text.RegularExpressions;
using System.Windows;
using NeoEdit.Common;
using NeoEdit.Common.Controls;
using NeoEdit.Common.Dialogs;

namespace NeoEdit.Dialogs
{
	partial class EditFindReplaceDialog
	{
		class CheckBoxStatus
		{
			public bool WholeWords { get; set; }
			public bool MatchCase { get; set; }
			public bool MultiLine { get; set; }
			public bool IsRegex { get; set; }
			public bool EntireSelection { get; set; }
		}

		public class Result
		{
			public string Text { get; set; }
			public string Replace { get; set; }
			public bool WholeWords { get; set; }
			public bool MatchCase { get; set; }
			public bool MultiLine { get; set; }
			public bool IsRegex { get; set; }
			public bool SelectionOnly { get; set; }
			public bool EntireSelection { get; set; }
		}

		[DepProp]
		public string Text { get { return UIHelper<EditFindReplaceDialog>.GetPropValue<string>(this); } set { UIHelper<EditFindReplaceDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string Replace { get { return UIHelper<EditFindReplaceDialog>.GetPropValue<string>(this); } set { UIHelper<EditFindReplaceDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool WholeWords { get { return UIHelper<EditFindReplaceDialog>.GetPropValue<bool>(this); } set { UIHelper<EditFindReplaceDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool MatchCase { get { return UIHelper<EditFindReplaceDialog>.GetPropValue<bool>(this); } set { UIHelper<EditFindReplaceDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool MultiLine { get { return UIHelper<EditFindReplaceDialog>.GetPropValue<bool>(this); } set { UIHelper<EditFindReplaceDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool IsRegex { get { return UIHelper<EditFindReplaceDialog>.GetPropValue<bool>(this); } set { UIHelper<EditFindReplaceDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool SelectionOnly { get { return UIHelper<EditFindReplaceDialog>.GetPropValue<bool>(this); } set { UIHelper<EditFindReplaceDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool EntireSelection { get { return UIHelper<EditFindReplaceDialog>.GetPropValue<bool>(this); } set { UIHelper<EditFindReplaceDialog>.SetPropValue(this, value); } }

		static EditFindReplaceDialog()
		{
			UIHelper<EditFindReplaceDialog>.Register();
			UIHelper<EditFindReplaceDialog>.AddCallback(a => a.SelectionOnly, (obj, o, n) => { if (!obj.SelectionOnly) obj.EntireSelection = false; });
			UIHelper<EditFindReplaceDialog>.AddCallback(a => a.EntireSelection, (obj, o, n) => { if (obj.EntireSelection) obj.SelectionOnly = true; });
		}

		EditFindReplaceDialog(string text, bool selectionOnly)
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
				MultiLine = MultiLine,
				IsRegex = IsRegex,
				EntireSelection = EntireSelection,
			};
		}

		void SetCheckBoxStatus(CheckBoxStatus checkBoxStatus)
		{
			if (checkBoxStatus == null)
				return;

			WholeWords = checkBoxStatus.WholeWords;
			MatchCase = checkBoxStatus.MatchCase;
			MultiLine = checkBoxStatus.MultiLine;
			IsRegex = checkBoxStatus.IsRegex;
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

			result = new Result { Text = Text, Replace = Replace, WholeWords = WholeWords, MatchCase = MatchCase, MultiLine = MultiLine, IsRegex = IsRegex, SelectionOnly = SelectionOnly, EntireSelection = EntireSelection };
			text.AddCurrentSuggestion(GetCheckBoxStatus());
			replace.AddCurrentSuggestion();

			DialogResult = true;
		}

		void RegExHelp(object sender, RoutedEventArgs e) => RegExHelpDialog.Display();

		void Reset(object sender, RoutedEventArgs e) => WholeWords = MatchCase = MultiLine = IsRegex = SelectionOnly = EntireSelection = false;

		static public Result Run(Window parent, string text, bool selectionOnly)
		{
			var dialog = new EditFindReplaceDialog(text, selectionOnly) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
