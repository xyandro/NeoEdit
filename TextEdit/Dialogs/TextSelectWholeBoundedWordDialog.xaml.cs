using System.Collections.Generic;
using System.Windows;
using NeoEdit.GUI.Controls;

namespace NeoEdit.TextEdit.Dialogs
{
	internal partial class TextSelectWholeBoundedWordDialog
	{
		internal class Result
		{
			public HashSet<char> Chars { get; set; }
		}

		[DepProp]
		public bool WholeWord { get { return UIHelper<TextSelectWholeBoundedWordDialog>.GetPropValue<bool>(this); } set { UIHelper<TextSelectWholeBoundedWordDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string Chars { get { return UIHelper<TextSelectWholeBoundedWordDialog>.GetPropValue<string>(this); } set { UIHelper<TextSelectWholeBoundedWordDialog>.SetPropValue(this, value); } }

		static TextSelectWholeBoundedWordDialog() { UIHelper<TextSelectWholeBoundedWordDialog>.Register(); }

		TextSelectWholeBoundedWordDialog(bool wholeWord)
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
			var dialog = new TextSelectWholeBoundedWordDialog(wholeWord) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
