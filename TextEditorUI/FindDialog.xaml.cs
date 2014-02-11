using System;
using System.Text.RegularExpressions;
using System.Windows;
using NeoEdit.Common;

namespace NeoEdit.TextEditorUI
{
	public partial class FindDialog : Window
	{
		[DepProp]
		public string Text { get { return uiHelper.GetPropValue<string>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public bool SelectionOnly { get { return uiHelper.GetPropValue<bool>(); } set { uiHelper.SetPropValue(value); } }

		public Regex Regex { get; private set; }
		public bool SelectAll { get; private set; }

		static FindDialog() { UIHelper<FindDialog>.Register(); }

		readonly UIHelper<FindDialog> uiHelper;
		public FindDialog()
		{
			uiHelper = new UIHelper<FindDialog>(this);
			InitializeComponent();
		}

		void OkClick(object sender, RoutedEventArgs e)
		{
			if (String.IsNullOrEmpty(Text))
				return;

			var text = Text;
			if (regularExpression.IsChecked == false)
				text = Regex.Escape(text);
			if (wholeWords.IsChecked == true)
				text = @"\b" + text + @"\b";
			var options = RegexOptions.None;
			if (matchCase.IsChecked == false)
				options = RegexOptions.IgnoreCase;
			Regex = new Regex(text, options);
			SelectAll = sender == selectAll;

			DialogResult = true;
		}
	}
}
