using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using NeoEdit.GUI.Controls;
using NeoEdit.TextEdit.RevRegEx;

namespace NeoEdit.TextEdit.Dialogs
{
	internal partial class RevRegExDialog
	{
		internal class Result
		{
			public List<string> Items { get; set; }
		}

		[DepProp]
		public string RegEx { get { return UIHelper<RevRegExDialog>.GetPropValue<string>(this); } set { UIHelper<RevRegExDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string RegExDesc { get { return UIHelper<RevRegExDialog>.GetPropValue<string>(this); } set { UIHelper<RevRegExDialog>.SetPropValue(this, value); } }
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
			result = new Result { Items = RevRegExVisitor.Parse(RegEx) };
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
			RegExDesc = "";
			NumResults = -1;

			var items = RevRegExVisitor.Parse(RegEx);
			RegExDesc = String.Join(", ", items.Select(item => String.Format("{0} ({1})", item, item.Length)));
			NumResults = 1;
			foreach (var item in items)
				NumResults *= item.Length;
		}

		public static Result Run(Window parent)
		{
			var dialog = new RevRegExDialog { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
