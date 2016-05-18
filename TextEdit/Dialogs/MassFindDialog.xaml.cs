using System.Windows;
using NeoEdit.Common;
using NeoEdit.Common.Expressions;
using NeoEdit.GUI.Controls;

namespace NeoEdit.TextEdit.Dialogs
{
	partial class MassFindDialog
	{
		public class Result
		{
			public string Text { get; set; }
			public bool MatchCase { get; set; }
			public bool SelectionOnly { get; set; }
			public bool KeepMatching { get; set; }
			public bool RemoveMatching { get; set; }
		}

		[DepProp]
		public string Text { get { return UIHelper<MassFindDialog>.GetPropValue<string>(this); } set { UIHelper<MassFindDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool MatchCase { get { return UIHelper<MassFindDialog>.GetPropValue<bool>(this); } set { UIHelper<MassFindDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool SelectionOnly { get { return UIHelper<MassFindDialog>.GetPropValue<bool>(this); } set { UIHelper<MassFindDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool KeepMatching { get { return UIHelper<MassFindDialog>.GetPropValue<bool>(this); } set { UIHelper<MassFindDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool RemoveMatching { get { return UIHelper<MassFindDialog>.GetPropValue<bool>(this); } set { UIHelper<MassFindDialog>.SetPropValue(this, value); } }
		[DepProp]
		public NEVariables Variables { get { return UIHelper<MassFindDialog>.GetPropValue<NEVariables>(this); } set { UIHelper<MassFindDialog>.SetPropValue(this, value); } }

		static bool matchCaseVal, keepMatchingVal, removeMatchingVal;

		static MassFindDialog()
		{
			UIHelper<MassFindDialog>.Register();
			UIHelper<MassFindDialog>.AddCallback(a => a.SelectionOnly, (obj, o, n) => { if (!obj.SelectionOnly) obj.KeepMatching = obj.RemoveMatching = false; });
			UIHelper<MassFindDialog>.AddCallback(a => a.KeepMatching, (obj, o, n) => { if (obj.KeepMatching) { obj.SelectionOnly = true; obj.RemoveMatching = false; } });
			UIHelper<MassFindDialog>.AddCallback(a => a.RemoveMatching, (obj, o, n) => { if (obj.RemoveMatching) { obj.SelectionOnly = true; obj.KeepMatching = false; } });
		}

		MassFindDialog(string text, bool selectionOnly, NEVariables variables)
		{
			InitializeComponent();

			Text = text.CoalesceNullOrEmpty(this.text.GetLastSuggestion(), "");
			MatchCase = matchCaseVal;
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

			result = new Result { Text = Text, MatchCase = MatchCase, SelectionOnly = SelectionOnly, KeepMatching = KeepMatching, RemoveMatching = RemoveMatching };

			matchCaseVal = MatchCase;
			keepMatchingVal = KeepMatching;
			removeMatchingVal = RemoveMatching;

			text.AddCurrentSuggestion();

			DialogResult = true;
		}

		void Reset(object sender, RoutedEventArgs e) => MatchCase = KeepMatching = RemoveMatching = false;

		static public Result Run(Window parent, string text, bool selectionOnly, NEVariables variables)
		{
			var dialog = new MassFindDialog(text, selectionOnly, variables) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
