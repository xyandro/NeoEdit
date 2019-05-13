using System.Linq;
using System.Windows;
using NeoEdit;
using NeoEdit.Controls;

namespace NeoEdit.Dialogs
{
	partial class DiffIgnoreCharactersDialog
	{
		internal class Result
		{
			public string IgnoreCharacters { get; set; }
		}

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

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			var ignoreCharacterStr = new string(Helpers.GetCharsFromCharString(IgnoreCharacters).ToCharArray());
			if (!MatchCase)
				ignoreCharacterStr = new string((ignoreCharacterStr.ToLowerInvariant() + ignoreCharacterStr.ToUpperInvariant()).Distinct().ToArray());
			result = new Result { IgnoreCharacters = ignoreCharacterStr };
			ignoreCharacters.AddCurrentSuggestion();
			DialogResult = true;
		}

		public static Result Run(Window parent, string ignoreCharacters)
		{
			var dialog = new DiffIgnoreCharactersDialog(ignoreCharacters) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
