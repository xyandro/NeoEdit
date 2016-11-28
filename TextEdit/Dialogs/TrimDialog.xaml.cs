using System.Collections.Generic;
using System.Windows;
using NeoEdit.GUI.Controls;

namespace NeoEdit.TextEdit.Dialogs
{
	internal partial class TrimDialog
	{
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
		public string TrimChars { get { return UIHelper<TrimDialog>.GetPropValue<string>(this); } set { UIHelper<TrimDialog>.SetPropValue(this, value); } }
		[DepProp]
		public TrimLocation Location { get { return UIHelper<TrimDialog>.GetPropValue<TrimLocation>(this); } set { UIHelper<TrimDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool MatchCase { get { return UIHelper<TrimDialog>.GetPropValue<bool>(this); } set { UIHelper<TrimDialog>.SetPropValue(this, value); } }

		static TrimDialog() { UIHelper<TrimDialog>.Register(); }

		TrimDialog()
		{
			InitializeComponent();
			TrimChars = @" \t\r\n\v\f\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000";
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
				TrimChars = new HashSet<char>(Misc.GetCharsFromRegexString(TrimChars).ToCharArray(), comparer),
				Start = Location.HasFlag(TrimLocation.Start),
				End = Location.HasFlag(TrimLocation.End),
			};
			DialogResult = true;
		}

		public static Result Run(Window parent)
		{
			var dialog = new TrimDialog() { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
