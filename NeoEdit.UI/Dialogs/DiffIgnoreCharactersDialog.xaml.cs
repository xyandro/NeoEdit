using System;
using System.Linq;
using System.Windows;
using NeoEdit.Common;
using NeoEdit.Common.Models;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class DiffIgnoreCharactersDialog
	{
		[DepProp]
		public string IgnoreCharacters { get { return UIHelper<DiffIgnoreCharactersDialog>.GetPropValue<string>(this); } set { UIHelper<DiffIgnoreCharactersDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool MatchCase { get { return UIHelper<DiffIgnoreCharactersDialog>.GetPropValue<bool>(this); } set { UIHelper<DiffIgnoreCharactersDialog>.SetPropValue(this, value); } }

		static DiffIgnoreCharactersDialog() { UIHelper<DiffIgnoreCharactersDialog>.Register(); }

		DiffIgnoreCharactersDialog(string ignoreCharacters)
		{
			InitializeComponent();
			IgnoreCharacters = ignoreCharacters;
			MatchCase = false;
		}

		DiffIgnoreCharactersDialogResult result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			var ignoreCharacterStr = new string(Helpers.GetCharsFromCharString(IgnoreCharacters).ToCharArray());
			if (!MatchCase)
				ignoreCharacterStr = new string((ignoreCharacterStr.ToLowerInvariant() + ignoreCharacterStr.ToUpperInvariant()).Distinct().ToArray());
			result = new DiffIgnoreCharactersDialogResult { IgnoreCharacters = ignoreCharacterStr };
			ignoreCharacters.AddCurrentSuggestion();
			DialogResult = true;
		}

		public static DiffIgnoreCharactersDialogResult Run(Window parent, string ignoreCharacters)
		{
			var dialog = new DiffIgnoreCharactersDialog(ignoreCharacters) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
