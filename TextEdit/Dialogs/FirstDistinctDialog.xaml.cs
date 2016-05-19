using System.Windows;
using NeoEdit.GUI.Controls;

namespace NeoEdit.TextEdit.Dialogs
{
	partial class FirstDistinctDialog
	{
		internal class Result
		{
			public string Chars { get; set; }
			public bool MatchCase { get; set; }
		}

		[DepProp]
		public string Chars { get { return UIHelper<FirstDistinctDialog>.GetPropValue<string>(this); } set { UIHelper<FirstDistinctDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool MatchCase { get { return UIHelper<FirstDistinctDialog>.GetPropValue<bool>(this); } set { UIHelper<FirstDistinctDialog>.SetPropValue(this, value); } }

		static FirstDistinctDialog() { UIHelper<FirstDistinctDialog>.Register(); }

		FirstDistinctDialog()
		{
			InitializeComponent();
			Chars = chars.GetLastSuggestion() ?? "a-zA-Z";
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			var chars = Misc.GetCharsFromRegexString(Chars);
			if (chars.Length == 0)
				return;

			result = new Result { Chars = chars, MatchCase = MatchCase };
			this.chars.AddCurrentSuggestion();
			DialogResult = true;
		}

		public static Result Run(Window parent)
		{
			var dialog = new FirstDistinctDialog() { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
