using System;
using System.Linq;
using System.Windows;
using NeoEdit.Common;
using NeoEdit.Common.Configuration;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Diff_IgnoreCharacters_Dialog
	{
		[DepProp]
		public string IgnoreCharacters { get { return UIHelper<Diff_IgnoreCharacters_Dialog>.GetPropValue<string>(this); } set { UIHelper<Diff_IgnoreCharacters_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool MatchCase { get { return UIHelper<Diff_IgnoreCharacters_Dialog>.GetPropValue<bool>(this); } set { UIHelper<Diff_IgnoreCharacters_Dialog>.SetPropValue(this, value); } }

		static Diff_IgnoreCharacters_Dialog() { UIHelper<Diff_IgnoreCharacters_Dialog>.Register(); }

		Diff_IgnoreCharacters_Dialog(string ignoreCharacters)
		{
			InitializeComponent();
			IgnoreCharacters = ignoreCharacters;
			MatchCase = false;
		}

		Configuration_Diff_IgnoreCharacters result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			var ignoreCharacterStr = new string(Helpers.GetCharsFromCharString(IgnoreCharacters).ToCharArray());
			if (!MatchCase)
				ignoreCharacterStr = new string((ignoreCharacterStr.ToLowerInvariant() + ignoreCharacterStr.ToUpperInvariant()).Distinct().ToArray());
			result = new Configuration_Diff_IgnoreCharacters { IgnoreCharacters = ignoreCharacterStr };
			ignoreCharacters.AddCurrentSuggestion();
			DialogResult = true;
		}

		public static Configuration_Diff_IgnoreCharacters Run(Window parent, string ignoreCharacters)
		{
			var dialog = new Diff_IgnoreCharacters_Dialog(ignoreCharacters) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
