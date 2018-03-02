using System.Collections.Generic;
using System.Windows;
using NeoEdit.GUI.Controls;

namespace NeoEdit.TextEdit.Dialogs
{
	internal partial class TextSelectWholeBoundedWordDialog
	{
		const string WHOLEWORDCOMPLETIONTAG = "SelectWholeWord";
		const string BOUNDEDWORDCOMPLETIONTAG = "SelectBoundedWord";

		const string ALPHANUMERIC = @"a-zA-Z0-9_";
		const string STRING = @"""'\r\n";
		const string WHITESPACE = @" \t\r\n\v\f\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000";

		internal enum TrimLocation
		{
			Start = 1,
			End = 2,
			Both = Start | End,
		}

		internal class Result
		{
			public HashSet<char> Chars { get; set; }
			public bool Start { get; set; }
			public bool End { get; set; }
		}

		[DepProp]
		public string Chars { get { return UIHelper<TextSelectWholeBoundedWordDialog>.GetPropValue<string>(this); } set { UIHelper<TextSelectWholeBoundedWordDialog>.SetPropValue(this, value); } }
		[DepProp]
		public TrimLocation Location { get { return UIHelper<TextSelectWholeBoundedWordDialog>.GetPropValue<TrimLocation>(this); } set { UIHelper<TextSelectWholeBoundedWordDialog>.SetPropValue(this, value); } }

		static TextSelectWholeBoundedWordDialog()
		{
			UIHelper<TextSelectWholeBoundedWordDialog>.Register();
			AutoCompleteTextBox.AddTagSuggestions(WHOLEWORDCOMPLETIONTAG, ALPHANUMERIC, STRING, WHITESPACE);
			AutoCompleteTextBox.AddTagSuggestions(BOUNDEDWORDCOMPLETIONTAG, STRING, WHITESPACE, ALPHANUMERIC);
		}

		TextSelectWholeBoundedWordDialog(bool wholeWord)
		{
			InitializeComponent();
			Title = $"Select {(wholeWord ? "Whole" : "Bounded")} Word";
			chars.CompletionTag = wholeWord ? WHOLEWORDCOMPLETIONTAG : BOUNDEDWORDCOMPLETIONTAG;
			Chars = wholeWord ? ALPHANUMERIC : STRING;
			Location = TrimLocation.Both;
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result
			{
				Chars = new HashSet<char>(Misc.GetCharsFromRegexString(Chars).ToCharArray()),
				Start = Location.HasFlag(TrimLocation.Start),
				End = Location.HasFlag(TrimLocation.End),
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
