using System.Collections.Generic;
using System.Windows;
using NeoEdit.GUI.Controls;

namespace NeoEdit.TextEdit.Dialogs
{
	internal partial class SelectWordDialog
	{
		internal class Result
		{
			public HashSet<char> Chars { get; set; }
		}

		[DepProp]
		public bool WholeWord { get { return UIHelper<SelectWordDialog>.GetPropValue<bool>(this); } set { UIHelper<SelectWordDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string Chars { get { return UIHelper<SelectWordDialog>.GetPropValue<string>(this); } set { UIHelper<SelectWordDialog>.SetPropValue(this, value); } }

		static SelectWordDialog() { UIHelper<SelectWordDialog>.Register(); }

		SelectWordDialog(bool wholeWord)
		{
			InitializeComponent();
			WholeWord = wholeWord;
			Chars = wholeWord ? @"a-zA-Z0-9_" : "\"'";
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result
			{
				Chars = new HashSet<char>(Misc.GetCharsFromRegexString(Chars).ToCharArray()),
			};
			chars.AddCurrentSuggestion();
			DialogResult = true;
		}

		public static Result Run(Window parent, bool wholeWord)
		{
			var dialog = new SelectWordDialog(wholeWord) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
