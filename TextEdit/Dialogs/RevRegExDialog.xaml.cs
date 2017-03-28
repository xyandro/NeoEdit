using System.Windows;
using NeoEdit.GUI.Controls;
using NeoEdit.TextEdit.RevRegEx;

namespace NeoEdit.TextEdit.Dialogs
{
	internal partial class RevRegExDialog
	{
		internal class Result
		{
			public string RegEx { get; set; }
			public int InfiniteCount { get; set; }
		}

		[DepProp]
		public string RegEx { get { return UIHelper<RevRegExDialog>.GetPropValue<string>(this); } set { UIHelper<RevRegExDialog>.SetPropValue(this, value); } }
		[DepProp]
		public long? NumResults { get { return UIHelper<RevRegExDialog>.GetPropValue<long?>(this); } set { UIHelper<RevRegExDialog>.SetPropValue(this, value); } }
		[DepProp]
		public int InfiniteCount { get { return UIHelper<RevRegExDialog>.GetPropValue<int>(this); } set { UIHelper<RevRegExDialog>.SetPropValue(this, value); } }

		static RevRegExDialog()
		{
			UIHelper<RevRegExDialog>.Register();
			UIHelper<RevRegExDialog>.AddCallback(a => a.RegEx, (obj, o, n) => obj.CalculateItems());
			UIHelper<RevRegExDialog>.AddCallback(a => a.InfiniteCount, (obj, o, n) => obj.CalculateItems());
		}

		RevRegExDialog()
		{
			InitializeComponent();
			RegEx = regex.GetLastSuggestion() ?? "";
			InfiniteCount = 10;
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result { RegEx = RegEx, InfiniteCount = InfiniteCount };
			regex.AddCurrentSuggestion();
			DialogResult = true;
		}

		void CalculateItems()
		{
			try { NumResults = RevRegExVisitor.Parse(RegEx, InfiniteCount).Count(); }
			catch { NumResults = null; }
		}

		public static Result Run(Window parent)
		{
			var dialog = new RevRegExDialog { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
