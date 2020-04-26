using System;
using System.Collections.Generic;
using System.Windows;
using NeoEdit.Common;
using NeoEdit.Common.Configuration;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Configure_Text_Select_Trim_Dialog
	{
		const string WHITESPACE = @" \t\r\n\v\f\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000";

		public enum TrimLocation
		{
			Start = 1,
			End = 2,
			Both = Start | End,
		}

		[DepProp]
		public string TrimChars { get { return UIHelper<Configure_Text_Select_Trim_Dialog>.GetPropValue<string>(this); } set { UIHelper<Configure_Text_Select_Trim_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public TrimLocation Location { get { return UIHelper<Configure_Text_Select_Trim_Dialog>.GetPropValue<TrimLocation>(this); } set { UIHelper<Configure_Text_Select_Trim_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool MatchCase { get { return UIHelper<Configure_Text_Select_Trim_Dialog>.GetPropValue<bool>(this); } set { UIHelper<Configure_Text_Select_Trim_Dialog>.SetPropValue(this, value); } }

		static Configure_Text_Select_Trim_Dialog()
		{
			UIHelper<Configure_Text_Select_Trim_Dialog>.Register();
			AutoCompleteTextBox.AddTagSuggestions("Configure_Text_Select_Trim_Dialog", WHITESPACE);
		}

		Configure_Text_Select_Trim_Dialog()
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

		Configuration_Text_Select_Trim result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			var comparer = MatchCase ? EqualityComparer<char>.Default : (IEqualityComparer<char>)new NoCaseCharComparer();
			result = new Configuration_Text_Select_Trim
			{
				TrimChars = new HashSet<char>(Helpers.GetCharsFromCharString(TrimChars).ToCharArray(), comparer),
				Start = Location.HasFlag(TrimLocation.Start),
				End = Location.HasFlag(TrimLocation.End),
			};
			trimChars.AddCurrentSuggestion();
			DialogResult = true;
		}

		public static Configuration_Text_Select_Trim Run(Window parent)
		{
			var dialog = new Configure_Text_Select_Trim_Dialog() { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
