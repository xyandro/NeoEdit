using System;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.RevRegEx;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Text_Advanced_ReverseRegex_Dialog
	{
		[DepProp]
		public string RegEx { get { return UIHelper<Text_Advanced_ReverseRegex_Dialog>.GetPropValue<string>(this); } set { UIHelper<Text_Advanced_ReverseRegex_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public long? NumResults { get { return UIHelper<Text_Advanced_ReverseRegex_Dialog>.GetPropValue<long?>(this); } set { UIHelper<Text_Advanced_ReverseRegex_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public int InfiniteCount { get { return UIHelper<Text_Advanced_ReverseRegex_Dialog>.GetPropValue<int>(this); } set { UIHelper<Text_Advanced_ReverseRegex_Dialog>.SetPropValue(this, value); } }

		static Text_Advanced_ReverseRegex_Dialog()
		{
			UIHelper<Text_Advanced_ReverseRegex_Dialog>.Register();
			UIHelper<Text_Advanced_ReverseRegex_Dialog>.AddCallback(a => a.RegEx, (obj, o, n) => obj.CalculateItems());
			UIHelper<Text_Advanced_ReverseRegex_Dialog>.AddCallback(a => a.InfiniteCount, (obj, o, n) => obj.CalculateItems());
		}

		Text_Advanced_ReverseRegex_Dialog()
		{
			InitializeComponent();
			RegEx = regex.GetLastSuggestion() ?? "";
			InfiniteCount = 10;
		}

		Configuration_Text_Advanced_ReverseRegex result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Configuration_Text_Advanced_ReverseRegex { RegEx = RegEx, InfiniteCount = InfiniteCount };
			regex.AddCurrentSuggestion();
			DialogResult = true;
		}

		void CalculateItems()
		{
			try { NumResults = RevRegExVisitor.Parse(RegEx, InfiniteCount).Count(); }
			catch { NumResults = null; }
		}

		public static Configuration_Text_Advanced_ReverseRegex Run(Window parent)
		{
			var dialog = new Text_Advanced_ReverseRegex_Dialog { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
