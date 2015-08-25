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

		static RevRegExDialog()
		{
			UIHelper<RevRegExDialog>.Register();
			UIHelper<RevRegExDialog>.AddCallback(a => a.RegEx, (obj, o, n) => obj.CalculateItems());
		}

		RevRegExDialog()
		{
			InitializeComponent();
			RegEx = "";
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result { RegEx = RegEx };
			DialogResult = true;
		}

		int Factorial(int val)
		{
			int result = 1;
			for (var ctr = 2; ctr <= val; ++ctr)
				result *= ctr;
			return result;
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
