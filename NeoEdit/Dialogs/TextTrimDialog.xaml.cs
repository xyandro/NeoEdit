using System.Collections.Generic;
using System.Windows;
using NeoEdit;
using NeoEdit.Controls;

namespace NeoEdit.Dialogs
{
	internal partial class TextTrimDialog
	{
		const string WHITESPACE = @" \t\r\n\v\f\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000";

		internal enum TrimLocation
		{
			Start = 1,
			End = 2,
			Both = Start | End,
		}

		internal class Result
		{
			public HashSet<char> TrimChars { get; set; }
			public bool Start { get; set; }
			public bool End { get; set; }
		}

		[DepProp]
		public string TrimChars { get { return UIHelper<TextTrimDialog>.GetPropValue<string>(this); } set { UIHelper<TextTrimDialog>.SetPropValue(this, value); } }
		[DepProp]
		public TrimLocation Location { get { return UIHelper<TextTrimDialog>.GetPropValue<TrimLocation>(this); } set { UIHelper<TextTrimDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool MatchCase { get { return UIHelper<TextTrimDialog>.GetPropValue<bool>(this); } set { UIHelper<TextTrimDialog>.SetPropValue(this, value); } }

		static TextTrimDialog()
		{
			UIHelper<TextTrimDialog>.Register();
			AutoCompleteTextBox.AddTagSuggestions("TextTrimDialog", WHITESPACE);
		}

		TextTrimDialog()
		{
			InitializeComponent();
			TrimChars = WHITESPACE;
			Location = TrimLocation.Both;
			MatchCase = false;
		}

		class NoCaseCharComparer : IEqualityComparer<char>
		{
			public bool Equals(char val1, char val2) => char.ToLowerInvariant(val1) == char.ToLowerInvariant(val2);
			public int GetHashCode(char val) => char.ToLowerInvariant(val).GetHashCode();
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			var comparer = MatchCase ? EqualityComparer<char>.Default : (IEqualityComparer<char>)new NoCaseCharComparer();
			result = new Result
			{
				TrimChars = new HashSet<char>(Helpers.GetCharsFromCharString(TrimChars).ToCharArray(), comparer),
				Start = Location.HasFlag(TrimLocation.Start),
				End = Location.HasFlag(TrimLocation.End),
			};
			trimChars.AddCurrentSuggestion();
			DialogResult = true;
		}

		public static Result Run(Window parent)
		{
			var dialog = new TextTrimDialog() { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
