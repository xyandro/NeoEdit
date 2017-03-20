using System.Collections.Generic;
using System.Windows;
using NeoEdit.GUI.Controls;

namespace NeoEdit.TextEdit.Dialogs
{
	internal partial class SelectWholeWordDialog
	{
		internal class Result
		{
			public HashSet<char> Chars { get; set; }
		}

		[DepProp]
		public string Chars { get { return UIHelper<SelectWholeWordDialog>.GetPropValue<string>(this); } set { UIHelper<SelectWholeWordDialog>.SetPropValue(this, value); } }

		static SelectWholeWordDialog() { UIHelper<SelectWholeWordDialog>.Register(); }

		SelectWholeWordDialog()
		{
			InitializeComponent();
			Chars = @"a-zA-Z0-9_";
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result
			{
				Chars = new HashSet<char>(Misc.GetCharsFromRegexString(Chars).ToCharArray()),
			};
			chars.AddCurrentSuggestion();
			DialogResult = true;
		}

		public static Result Run(Window parent)
		{
			var dialog = new SelectWholeWordDialog() { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
