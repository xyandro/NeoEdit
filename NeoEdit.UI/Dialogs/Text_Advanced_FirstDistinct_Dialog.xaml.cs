using System;
using System.Windows;
using NeoEdit.Common;
using NeoEdit.Common.Configuration;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Text_Advanced_FirstDistinct_Dialog
	{
		[DepProp]
		public string Chars { get { return UIHelper<Text_Advanced_FirstDistinct_Dialog>.GetPropValue<string>(this); } set { UIHelper<Text_Advanced_FirstDistinct_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool MatchCase { get { return UIHelper<Text_Advanced_FirstDistinct_Dialog>.GetPropValue<bool>(this); } set { UIHelper<Text_Advanced_FirstDistinct_Dialog>.SetPropValue(this, value); } }

		static Text_Advanced_FirstDistinct_Dialog() { UIHelper<Text_Advanced_FirstDistinct_Dialog>.Register(); }

		Text_Advanced_FirstDistinct_Dialog()
		{
			InitializeComponent();
			Chars = chars.GetLastSuggestion() ?? "a-zA-Z";
		}

		Configuration_Text_Advanced_FirstDistinct result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			var chars = Helpers.GetCharsFromCharString(Chars);
			if (chars.Length == 0)
				return;

			result = new Configuration_Text_Advanced_FirstDistinct { Chars = chars, MatchCase = MatchCase };
			this.chars.AddCurrentSuggestion();
			DialogResult = true;
		}

		public static Configuration_Text_Advanced_FirstDistinct Run(Window parent)
		{
			var dialog = new Text_Advanced_FirstDistinct_Dialog() { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
