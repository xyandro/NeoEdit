using System;
using System.Windows;
using NeoEdit.Common;
using NeoEdit.Common.Configuration;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Configure_Text_FirstDistinct_Dialog
	{
		[DepProp]
		public string Chars { get { return UIHelper<Configure_Text_FirstDistinct_Dialog>.GetPropValue<string>(this); } set { UIHelper<Configure_Text_FirstDistinct_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool MatchCase { get { return UIHelper<Configure_Text_FirstDistinct_Dialog>.GetPropValue<bool>(this); } set { UIHelper<Configure_Text_FirstDistinct_Dialog>.SetPropValue(this, value); } }

		static Configure_Text_FirstDistinct_Dialog() { UIHelper<Configure_Text_FirstDistinct_Dialog>.Register(); }

		Configure_Text_FirstDistinct_Dialog()
		{
			InitializeComponent();
			Chars = chars.GetLastSuggestion() ?? "a-zA-Z";
		}

		Configuration_Text_FirstDistinct result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			var chars = Helpers.GetCharsFromCharString(Chars);
			if (chars.Length == 0)
				return;

			result = new Configuration_Text_FirstDistinct { Chars = chars, MatchCase = MatchCase };
			this.chars.AddCurrentSuggestion();
			DialogResult = true;
		}

		public static Configuration_Text_FirstDistinct Run(Window parent)
		{
			var dialog = new Configure_Text_FirstDistinct_Dialog() { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
