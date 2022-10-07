using System;
using System.Collections.Generic;
using System.Windows;
using NeoEdit.Common;
using NeoEdit.Common.Configuration;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Text_SelectTrim_WholeBoundedWordTrim_Dialog
	{
		const string ALPHANUMERIC = @"a-zA-Z0-9_";
		const string STRING = @"""'\r\n";
		const string WHITESPACE = @" \t\r\n\v\f\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000";

		static readonly List<(string, string, string[])> data = new List<(string, string, string[])>
		{
			("Trim", "Trim", new string[] { WHITESPACE, STRING, ALPHANUMERIC }),
			("Select Whole Word", "SelectWholeWord", new string[] { ALPHANUMERIC, STRING, WHITESPACE }),
			("Select Bounded Word", "SelectBoundedWord", new string[] { STRING, WHITESPACE, ALPHANUMERIC }),
		};

		public enum TrimLocation
		{
			Start = 1,
			End = 2,
			Both = Start | End,
		}

		[DepProp]
		public string Chars { get { return UIHelper<Text_SelectTrim_WholeBoundedWordTrim_Dialog>.GetPropValue<string>(this); } set { UIHelper<Text_SelectTrim_WholeBoundedWordTrim_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public TrimLocation Location { get { return UIHelper<Text_SelectTrim_WholeBoundedWordTrim_Dialog>.GetPropValue<TrimLocation>(this); } set { UIHelper<Text_SelectTrim_WholeBoundedWordTrim_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool MatchCase { get { return UIHelper<Text_SelectTrim_WholeBoundedWordTrim_Dialog>.GetPropValue<bool>(this); } set { UIHelper<Text_SelectTrim_WholeBoundedWordTrim_Dialog>.SetPropValue(this, value); } }

		static Text_SelectTrim_WholeBoundedWordTrim_Dialog()
		{
			UIHelper<Text_SelectTrim_WholeBoundedWordTrim_Dialog>.Register();
			foreach (var item in data)
				AutoCompleteTextBox.AddTagSuggestions(item.Item2, item.Item3);
		}

		Text_SelectTrim_WholeBoundedWordTrim_Dialog(int index)
		{
			InitializeComponent();
			Title = data[index].Item1;
			chars.CompletionTag = data[index].Item2;
			Chars = data[index].Item3[0];
			Location = TrimLocation.Both;
			MatchCase = false;
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

		Configuration_Text_SelectTrim_WholeBoundedWordTrim result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			var useChars = Helpers.GetCharsFromCharString(Chars);
			if (!MatchCase)
				useChars += useChars.ToUpperInvariant();

			result = new Configuration_Text_SelectTrim_WholeBoundedWordTrim
			{
				Chars = new HashSet<char>(useChars.ToCharArray()),
				Start = Location.HasFlag(TrimLocation.Start),
				End = Location.HasFlag(TrimLocation.End),
			};
			chars.AddCurrentSuggestion();
			DialogResult = true;
		}

		public static Configuration_Text_SelectTrim_WholeBoundedWordTrim Run(Window parent, int index)
		{
			var dialog = new Text_SelectTrim_WholeBoundedWordTrim_Dialog(index) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
