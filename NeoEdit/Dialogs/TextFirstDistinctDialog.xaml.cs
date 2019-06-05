using System.Windows;
using NeoEdit.Common;
using NeoEdit.Common.Controls;

namespace NeoEdit.Dialogs
{
	partial class TextFirstDistinctDialog
	{
		public class Result
		{
			public string Chars { get; set; }
			public bool MatchCase { get; set; }
		}

		[DepProp]
		public string Chars { get { return UIHelper<TextFirstDistinctDialog>.GetPropValue<string>(this); } set { UIHelper<TextFirstDistinctDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool MatchCase { get { return UIHelper<TextFirstDistinctDialog>.GetPropValue<bool>(this); } set { UIHelper<TextFirstDistinctDialog>.SetPropValue(this, value); } }

		static TextFirstDistinctDialog() { UIHelper<TextFirstDistinctDialog>.Register(); }

		TextFirstDistinctDialog()
		{
			InitializeComponent();
			Chars = chars.GetLastSuggestion() ?? "a-zA-Z";
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			var chars = Helpers.GetCharsFromCharString(Chars);
			if (chars.Length == 0)
				return;

			result = new Result { Chars = chars, MatchCase = MatchCase };
			this.chars.AddCurrentSuggestion();
			DialogResult = true;
		}

		public static Result Run(Window parent)
		{
			var dialog = new TextFirstDistinctDialog() { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
