using System.Text.RegularExpressions;
using System.Windows;
using NeoEdit.GUI.Controls;

namespace NeoEdit.GUI.Dialogs
{
	partial class FindTextDialog
	{
		public class Result
		{
			public Regex Regex { get; set; }
			public bool MultiLine { get; set; }
		}

		[DepProp]
		public string Text { get { return UIHelper<FindTextDialog>.GetPropValue<string>(this); } set { UIHelper<FindTextDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool WholeWords { get { return UIHelper<FindTextDialog>.GetPropValue<bool>(this); } set { UIHelper<FindTextDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool MatchCase { get { return UIHelper<FindTextDialog>.GetPropValue<bool>(this); } set { UIHelper<FindTextDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool IsRegex { get { return UIHelper<FindTextDialog>.GetPropValue<bool>(this); } set { UIHelper<FindTextDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool MultiLine { get { return UIHelper<FindTextDialog>.GetPropValue<bool>(this); } set { UIHelper<FindTextDialog>.SetPropValue(this, value); } }

		static bool wholeWordsVal, matchCaseVal, isRegexVal, multiLineVal;

		static FindTextDialog() { UIHelper<FindTextDialog>.Register(); }

		FindTextDialog()
		{
			InitializeComponent();

			Text = text.GetLastSuggestion() ?? "";
			WholeWords = wholeWordsVal;
			MatchCase = matchCaseVal;
			IsRegex = isRegexVal;
			MultiLine = multiLineVal;
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
			result = new Result { Regex = new Regex(text, options), MultiLine = MultiLine };

			wholeWordsVal = WholeWords;
			matchCaseVal = MatchCase;
			isRegexVal = IsRegex;
			multiLineVal = MultiLine;

			text = Text;
			this.text.AddCurrentSuggestion();

			DialogResult = true;
		}

		static public Result Run(Window parent)
		{
			var dialog = new FindTextDialog() { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}

		void RegExHelp(object sender, RoutedEventArgs e) => RegExHelpDialog.Display();
	}
}
