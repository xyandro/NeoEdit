using System;
using System.Collections.Generic;
using System.Windows;
using NeoEdit.Common;
using NeoEdit.Common.Configuration;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Configure_Text_Select_WholeBoundedWord_Dialog
	{
		const string WHOLEWORDCOMPLETIONTAG = "SelectWholeWord";
		const string BOUNDEDWORDCOMPLETIONTAG = "SelectBoundedWord";

		const string ALPHANUMERIC = @"a-zA-Z0-9_";
		const string STRING = @"""'\r\n";
		const string WHITESPACE = @" \t\r\n\v\f\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000";

		public enum TrimLocation
		{
			Start = 1,
			End = 2,
			Both = Start | End,
		}

		[DepProp]
		public string Chars { get { return UIHelper<Configure_Text_Select_WholeBoundedWord_Dialog>.GetPropValue<string>(this); } set { UIHelper<Configure_Text_Select_WholeBoundedWord_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public TrimLocation Location { get { return UIHelper<Configure_Text_Select_WholeBoundedWord_Dialog>.GetPropValue<TrimLocation>(this); } set { UIHelper<Configure_Text_Select_WholeBoundedWord_Dialog>.SetPropValue(this, value); } }

		static Configure_Text_Select_WholeBoundedWord_Dialog()
		{
			UIHelper<Configure_Text_Select_WholeBoundedWord_Dialog>.Register();
			AutoCompleteTextBox.AddTagSuggestions(WHOLEWORDCOMPLETIONTAG, ALPHANUMERIC, STRING, WHITESPACE);
			AutoCompleteTextBox.AddTagSuggestions(BOUNDEDWORDCOMPLETIONTAG, STRING, WHITESPACE, ALPHANUMERIC);
		}

		Configure_Text_Select_WholeBoundedWord_Dialog(bool wholeWord)
		{
			InitializeComponent();
			Title = $"Select {(wholeWord ? "Whole" : "Bounded")} Word";
			chars.CompletionTag = wholeWord ? WHOLEWORDCOMPLETIONTAG : BOUNDEDWORDCOMPLETIONTAG;
			Chars = wholeWord ? ALPHANUMERIC : STRING;
			Location = TrimLocation.Both;
		}

		void OnTypeClick(object sender, RoutedEventArgs e)
		{
			switch ((sender as FrameworkElement).Tag)
			{
				case "Words": Chars = ALPHANUMERIC; break;
				case "Space": Chars = WHITESPACE; break;
				case "Strings": Chars = STRING; break;
			}
		}

		Configuration_Text_Select_WholeBoundedWord result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Configuration_Text_Select_WholeBoundedWord
			{
				Chars = new HashSet<char>(Helpers.GetCharsFromCharString(Chars).ToCharArray()),
				Start = Location.HasFlag(TrimLocation.Start),
				End = Location.HasFlag(TrimLocation.End),
			};
			chars.AddCurrentSuggestion();
			DialogResult = true;
		}

		public static Configuration_Text_Select_WholeBoundedWord Run(Window parent, bool wholeWord)
		{
			var dialog = new Configure_Text_Select_WholeBoundedWord_Dialog(wholeWord) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
