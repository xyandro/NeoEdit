using System.Text.RegularExpressions;
using System.Windows;
using NeoEdit.Program.Controls;

namespace NeoEdit.Program.Dialogs
{
	partial class FilesFindRegexDialog
	{
		public class Result
		{
			public Regex Regex { get; set; }
		}

		[DepProp]
		public string Text { get { return UIHelper<FilesFindRegexDialog>.GetPropValue<string>(this); } set { UIHelper<FilesFindRegexDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool WholeWords { get { return UIHelper<FilesFindRegexDialog>.GetPropValue<bool>(this); } set { UIHelper<FilesFindRegexDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool MatchCase { get { return UIHelper<FilesFindRegexDialog>.GetPropValue<bool>(this); } set { UIHelper<FilesFindRegexDialog>.SetPropValue(this, value); } }

		static bool wholeWordsVal, matchCaseVal;

		static FilesFindRegexDialog() { UIHelper<FilesFindRegexDialog>.Register(); }

		FilesFindRegexDialog()
		{
			InitializeComponent();

			Text = text.GetLastSuggestion() ?? "";
			WholeWords = wholeWordsVal;
			MatchCase = matchCaseVal;
		}

		void Reset(object sender, RoutedEventArgs e) => WholeWords = MatchCase = false;

		void Escape(object sender, RoutedEventArgs e) => Text = Regex.Escape(Text);
		void Unescape(object sender, RoutedEventArgs e) => Text = Regex.Unescape(Text);

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrEmpty(Text))
				return;

			var text = Text;
			if (WholeWords)
				text = $"\\b{text}\\b";
			var options = RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.Multiline;
			if (!MatchCase)
				options |= RegexOptions.IgnoreCase;
			result = new Result { Regex = new Regex(text, options) };

			wholeWordsVal = WholeWords;
			matchCaseVal = MatchCase;

			this.text.AddCurrentSuggestion();

			DialogResult = true;
		}

		static public Result Run(Window parent)
		{
			var dialog = new FilesFindRegexDialog() { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}

		void RegExHelp(object sender, RoutedEventArgs e) => RegExHelpDialog.Display();
	}
}
