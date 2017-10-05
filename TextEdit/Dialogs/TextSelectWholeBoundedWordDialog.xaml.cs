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
			if (wholeWord)
				chars.AddSuggestions(@"a-zA-Z0-9_");
			else
				chars.AddSuggestions(@"""'\r\n", @" \t\r\n\v\f\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000");
			Chars = chars.GetLastSuggestion();
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
