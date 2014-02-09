using System;
using System.Text.RegularExpressions;
using System.Windows;
using NeoEdit.Common;

namespace NeoEdit.TextEditorUI
{
	public partial class FindDialog : Window
	{
		static FindDialog() { UIHelper<FindDialog>.Register(); }

		readonly UIHelper<FindDialog> uiHelper;
		FindDialog()
		{
			uiHelper = new UIHelper<FindDialog>(this);
			InitializeComponent();
		}

		void OkClick(object sender, RoutedEventArgs e)
		{
			if (String.IsNullOrEmpty(findText.Text))
				return;

			DialogResult = true;
		}

		public static void Run(out Regex regex, out bool selectionOnly)
		{
			regex = null;
			selectionOnly = false;

			var find = new FindDialog();
			if (find.ShowDialog() == false)
				return;

			var findText = find.findText.Text;
			if (find.regularExpression.IsChecked == false)
				findText = Regex.Escape(findText);
			if (find.wholeWords.IsChecked == true)
				findText = @"\b" + findText + @"\b";
			var options = RegexOptions.None;
			if (find.matchCase.IsChecked == false)
				options = RegexOptions.IgnoreCase;
			regex = new Regex(findText, options);
			selectionOnly = find.selectionOnly.IsChecked == true;
		}
	}
}
