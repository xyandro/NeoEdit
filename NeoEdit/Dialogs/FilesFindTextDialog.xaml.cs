using System.Text.RegularExpressions;
using System.Windows;
using NeoEdit.Program.Controls;

namespace NeoEdit.Program.Dialogs
{
	partial class FilesFindTextDialog
	{
		public class Result
		{
			public Regex Regex { get; set; }
		}

		[DepProp]
		public string Text { get { return UIHelper<FilesFindTextDialog>.GetPropValue<string>(this); } set { UIHelper<FilesFindTextDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool WholeWords { get { return UIHelper<FilesFindTextDialog>.GetPropValue<bool>(this); } set { UIHelper<FilesFindTextDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool MatchCase { get { return UIHelper<FilesFindTextDialog>.GetPropValue<bool>(this); } set { UIHelper<FilesFindTextDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool IsRegex { get { return UIHelper<FilesFindTextDialog>.GetPropValue<bool>(this); } set { UIHelper<FilesFindTextDialog>.SetPropValue(this, value); } }

		static bool wholeWordsVal, matchCaseVal, isRegexVal;

		static FilesFindTextDialog() { UIHelper<FilesFindTextDialog>.Register(); }

		FilesFindTextDialog()
		{
			InitializeComponent();

			Text = text.GetLastSuggestion() ?? "";
			WholeWords = wholeWordsVal;
			MatchCase = matchCaseVal;
			IsRegex = isRegexVal;
		}

		void Escape(object sender, RoutedEventArgs e) => Text = Regex.Escape(Text);
		void Unescape(object sender, RoutedEventArgs e) => Text = Regex.Unescape(Text);

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrEmpty(Text))
				return;

			var text = Text;
			if (!IsRegex)
				text = Regex.Escape(text);
			if (WholeWords)
				text = $"\\b{text}\\b";
			var options = RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.Multiline;
			if (!MatchCase)
				options |= RegexOptions.IgnoreCase;
			result = new Result { Regex = new Regex(text, options) };

			wholeWordsVal = WholeWords;
			matchCaseVal = MatchCase;
			isRegexVal = IsRegex;

			text = Text;
			this.text.AddCurrentSuggestion();

			DialogResult = true;
		}

		static public Result Run(Window parent)
		{
			var dialog = new FilesFindTextDialog() { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}

		void RegExHelp(object sender, RoutedEventArgs e) => RegExHelpDialog.Display();
	}
}
