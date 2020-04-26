using System;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.RevRegEx;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Configure_Text_ReverseRegEx_Dialog
	{
		[DepProp]
		public string RegEx { get { return UIHelper<Configure_Text_ReverseRegEx_Dialog>.GetPropValue<string>(this); } set { UIHelper<Configure_Text_ReverseRegEx_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public long? NumResults { get { return UIHelper<Configure_Text_ReverseRegEx_Dialog>.GetPropValue<long?>(this); } set { UIHelper<Configure_Text_ReverseRegEx_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public int InfiniteCount { get { return UIHelper<Configure_Text_ReverseRegEx_Dialog>.GetPropValue<int>(this); } set { UIHelper<Configure_Text_ReverseRegEx_Dialog>.SetPropValue(this, value); } }

		static Configure_Text_ReverseRegEx_Dialog()
		{
			UIHelper<Configure_Text_ReverseRegEx_Dialog>.Register();
			UIHelper<Configure_Text_ReverseRegEx_Dialog>.AddCallback(a => a.RegEx, (obj, o, n) => obj.CalculateItems());
			UIHelper<Configure_Text_ReverseRegEx_Dialog>.AddCallback(a => a.InfiniteCount, (obj, o, n) => obj.CalculateItems());
		}

		Configure_Text_ReverseRegEx_Dialog()
		{
			InitializeComponent();
			RegEx = regex.GetLastSuggestion() ?? "";
			InfiniteCount = 10;
		}

		Configuration_Text_ReverseRegEx result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Configuration_Text_ReverseRegEx { RegEx = RegEx, InfiniteCount = InfiniteCount };
			regex.AddCurrentSuggestion();
			DialogResult = true;
		}

		void CalculateItems()
		{
			try { NumResults = RevRegExVisitor.Parse(RegEx, InfiniteCount).Count(); }
			catch { NumResults = null; }
		}

		public static Configuration_Text_ReverseRegEx Run(Window parent)
		{
			var dialog = new Configure_Text_ReverseRegEx_Dialog { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
