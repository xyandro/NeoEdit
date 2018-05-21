using System.Text.RegularExpressions;
using System.Windows;
using NeoEdit.GUI.Controls;
using NeoEdit.GUI.Dialogs;

namespace NeoEdit.TextEdit.Dialogs
{
	partial class SelectSplitDialog
	{
		public class Result
		{
			public Regex Regex { get; set; }
			public bool IncludeResults { get; set; }
			public bool IncludeEmpty { get; set; }
		}

		[DepProp]
		public string Text { get { return UIHelper<SelectSplitDialog>.GetPropValue<string>(this); } set { UIHelper<SelectSplitDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool MatchCase { get { return UIHelper<SelectSplitDialog>.GetPropValue<bool>(this); } set { UIHelper<SelectSplitDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool IsRegex { get { return UIHelper<SelectSplitDialog>.GetPropValue<bool>(this); } set { UIHelper<SelectSplitDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool IncludeResults { get { return UIHelper<SelectSplitDialog>.GetPropValue<bool>(this); } set { UIHelper<SelectSplitDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool IncludeEmpty { get { return UIHelper<SelectSplitDialog>.GetPropValue<bool>(this); } set { UIHelper<SelectSplitDialog>.SetPropValue(this, value); } }

		static SelectSplitDialog() { UIHelper<SelectSplitDialog>.Register(); }

		SelectSplitDialog()
		{
			InitializeComponent();

			Text = "";
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrEmpty(Text))
				return;

			var text = Text;
			if (!IsRegex)
				text = Regex.Escape(text);
			var options = RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.Multiline;
			if (!MatchCase)
				options |= RegexOptions.IgnoreCase;
			result = new Result { Regex = new Regex(text, options), IncludeResults = IncludeResults, IncludeEmpty = IncludeEmpty };

			this.text.AddCurrentSuggestion();

			DialogResult = true;
		}

		void RegExHelp(object sender, RoutedEventArgs e) => RegExHelpDialog.Display();

		static public Result Run(Window parent)
		{
			var dialog = new SelectSplitDialog() { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
