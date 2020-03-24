using System;
using System.Windows;
using NeoEdit.Common;
using NeoEdit.Common.Models;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class TextFirstDistinctDialog
	{
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

		TextFirstDistinctDialogResult result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			var chars = Helpers.GetCharsFromCharString(Chars);
			if (chars.Length == 0)
				return;

			result = new TextFirstDistinctDialogResult { Chars = chars, MatchCase = MatchCase };
			this.chars.AddCurrentSuggestion();
			DialogResult = true;
		}

		public static TextFirstDistinctDialogResult Run(Window parent)
		{
			var dialog = new TextFirstDistinctDialog() { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
