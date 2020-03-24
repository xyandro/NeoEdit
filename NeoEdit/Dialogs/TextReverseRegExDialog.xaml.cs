using System;
using System.Windows;
using NeoEdit.Common.Models;
using NeoEdit.Common.RevRegEx;
using NeoEdit.Program.Controls;

namespace NeoEdit.Program.Dialogs
{
	partial class TextReverseRegExDialog
	{
		[DepProp]
		public string RegEx { get { return UIHelper<TextReverseRegExDialog>.GetPropValue<string>(this); } set { UIHelper<TextReverseRegExDialog>.SetPropValue(this, value); } }
		[DepProp]
		public long? NumResults { get { return UIHelper<TextReverseRegExDialog>.GetPropValue<long?>(this); } set { UIHelper<TextReverseRegExDialog>.SetPropValue(this, value); } }
		[DepProp]
		public int InfiniteCount { get { return UIHelper<TextReverseRegExDialog>.GetPropValue<int>(this); } set { UIHelper<TextReverseRegExDialog>.SetPropValue(this, value); } }

		static TextReverseRegExDialog()
		{
			UIHelper<TextReverseRegExDialog>.Register();
			UIHelper<TextReverseRegExDialog>.AddCallback(a => a.RegEx, (obj, o, n) => obj.CalculateItems());
			UIHelper<TextReverseRegExDialog>.AddCallback(a => a.InfiniteCount, (obj, o, n) => obj.CalculateItems());
		}

		TextReverseRegExDialog()
		{
			InitializeComponent();
			RegEx = regex.GetLastSuggestion() ?? "";
			InfiniteCount = 10;
		}

		TextReverseRegExDialogResult result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new TextReverseRegExDialogResult { RegEx = RegEx, InfiniteCount = InfiniteCount };
			regex.AddCurrentSuggestion();
			DialogResult = true;
		}

		void CalculateItems()
		{
			try { NumResults = RevRegExVisitor.Parse(RegEx, InfiniteCount).Count(); }
			catch { NumResults = null; }
		}

		public static TextReverseRegExDialogResult Run(Window parent)
		{
			var dialog = new TextReverseRegExDialog { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
