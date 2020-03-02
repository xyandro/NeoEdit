using System.Windows;
using NeoEdit.Program;
using NeoEdit.Program.Controls;
using NeoEdit.Program.Expressions;

namespace NeoEdit.Program.Dialogs
{
	partial class EditFindMassFindDialog
	{
		public class Result
		{
			public string Text { get; set; }
			public bool WholeWords { get; set; }
			public bool MatchCase { get; set; }
			public bool SelectionOnly { get; set; }
			public bool EntireSelection { get; set; }
			public bool KeepMatching { get; set; }
			public bool RemoveMatching { get; set; }
		}

		[DepProp]
		public string Text { get { return UIHelper<EditFindMassFindDialog>.GetPropValue<string>(this); } set { UIHelper<EditFindMassFindDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool WholeWords { get { return UIHelper<EditFindMassFindDialog>.GetPropValue<bool>(this); } set { UIHelper<EditFindMassFindDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool MatchCase { get { return UIHelper<EditFindMassFindDialog>.GetPropValue<bool>(this); } set { UIHelper<EditFindMassFindDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool SelectionOnly { get { return UIHelper<EditFindMassFindDialog>.GetPropValue<bool>(this); } set { UIHelper<EditFindMassFindDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool EntireSelection { get { return UIHelper<EditFindMassFindDialog>.GetPropValue<bool>(this); } set { UIHelper<EditFindMassFindDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool KeepMatching { get { return UIHelper<EditFindMassFindDialog>.GetPropValue<bool>(this); } set { UIHelper<EditFindMassFindDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool RemoveMatching { get { return UIHelper<EditFindMassFindDialog>.GetPropValue<bool>(this); } set { UIHelper<EditFindMassFindDialog>.SetPropValue(this, value); } }
		[DepProp]
		public NEVariables Variables { get { return UIHelper<EditFindMassFindDialog>.GetPropValue<NEVariables>(this); } set { UIHelper<EditFindMassFindDialog>.SetPropValue(this, value); } }

		static bool wholeWordsVal, matchCaseVal, entireSelectionVal, keepMatchingVal, removeMatchingVal;

		static EditFindMassFindDialog()
		{
			UIHelper<EditFindMassFindDialog>.Register();
			UIHelper<EditFindMassFindDialog>.AddCallback(a => a.SelectionOnly, (obj, o, n) => { if (!obj.SelectionOnly) obj.EntireSelection = obj.KeepMatching = obj.RemoveMatching = false; });
			UIHelper<EditFindMassFindDialog>.AddCallback(a => a.EntireSelection, (obj, o, n) => { if (obj.EntireSelection) obj.SelectionOnly = true; });
			UIHelper<EditFindMassFindDialog>.AddCallback(a => a.KeepMatching, (obj, o, n) => { if (obj.KeepMatching) { obj.SelectionOnly = true; obj.RemoveMatching = false; } });
			UIHelper<EditFindMassFindDialog>.AddCallback(a => a.RemoveMatching, (obj, o, n) => { if (obj.RemoveMatching) { obj.SelectionOnly = true; obj.KeepMatching = false; } });
		}

		EditFindMassFindDialog(bool selectionOnly, NEVariables variables)
		{
			InitializeComponent();

			Text = text.GetLastSuggestion().CoalesceNullOrEmpty("k");
			WholeWords = wholeWordsVal;
			MatchCase = matchCaseVal;
			EntireSelection = entireSelectionVal;
			KeepMatching = keepMatchingVal;
			RemoveMatching = removeMatchingVal;
			SelectionOnly = selectionOnly;
			Variables = variables;
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrEmpty(Text))
				return;

			result = new Result { Text = Text, WholeWords = WholeWords, MatchCase = MatchCase, SelectionOnly = SelectionOnly, EntireSelection = EntireSelection, KeepMatching = KeepMatching, RemoveMatching = RemoveMatching };

			wholeWordsVal = WholeWords;
			matchCaseVal = MatchCase;
			entireSelectionVal = EntireSelection;
			keepMatchingVal = KeepMatching;
			removeMatchingVal = RemoveMatching;

			text.AddCurrentSuggestion();

			DialogResult = true;
		}

		void Reset(object sender, RoutedEventArgs e) => WholeWords = MatchCase = SelectionOnly = EntireSelection = KeepMatching = RemoveMatching = false;

		static public Result Run(Window parent, bool selectionOnly, NEVariables variables)
		{
			var dialog = new EditFindMassFindDialog(selectionOnly, variables) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
