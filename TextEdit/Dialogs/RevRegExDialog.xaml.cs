using System.Collections.ObjectModel;
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
		}

		[DepProp]
		public string RegEx { get { return UIHelper<RevRegExDialog>.GetPropValue<string>(this); } set { UIHelper<RevRegExDialog>.SetPropValue(this, value); } }
		[DepProp]
		public long NumResults { get { return UIHelper<RevRegExDialog>.GetPropValue<long>(this); } set { UIHelper<RevRegExDialog>.SetPropValue(this, value); } }
		[DepProp]
		public ObservableCollection<string> History { get { return UIHelper<RevRegExDialog>.GetPropValue<ObservableCollection<string>>(this); } set { UIHelper<RevRegExDialog>.SetPropValue(this, value); } }

		readonly static ObservableCollection<string> StaticHistory = new ObservableCollection<string>();

		static RevRegExDialog()
		{
			UIHelper<RevRegExDialog>.Register();
			UIHelper<RevRegExDialog>.AddCallback(a => a.RegEx, (obj, o, n) => obj.CalculateItems());
		}

		RevRegExDialog()
		{
			InitializeComponent();
			History = StaticHistory;
			RegEx = History.Count == 0 ? "" : History[0];
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result { RegEx = RegEx };
			History.Remove(RegEx);
			History.Insert(0, RegEx);
			DialogResult = true;
		}

		void CalculateItems()
		{
			NumResults = -1;
			NumResults = RevRegExVisitor.Parse(RegEx).Count();
		}

		public static Result Run(Window parent)
		{
			var dialog = new RevRegExDialog { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
