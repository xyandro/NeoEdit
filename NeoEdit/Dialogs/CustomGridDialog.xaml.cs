using System.Windows;
using NeoEdit.Controls;

namespace NeoEdit.Dialogs
{
	partial class CustomGridDialog
	{
		public class Result
		{
			public int? Columns { get; set; }
			public int? Rows { get; set; }
		}

		[DepProp]
		public int? Columns { get { return UIHelper<CustomGridDialog>.GetPropValue<int?>(this); } set { UIHelper<CustomGridDialog>.SetPropValue(this, value); } }
		[DepProp]
		public int? Rows { get { return UIHelper<CustomGridDialog>.GetPropValue<int?>(this); } set { UIHelper<CustomGridDialog>.SetPropValue(this, value); } }

		static CustomGridDialog() { UIHelper<CustomGridDialog>.Register(); }

		CustomGridDialog(int? columns, int? rows)
		{
			InitializeComponent();
			Columns = columns;
			Rows = rows;
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if ((Columns < 1) || (Rows < 1))
				return;
			result = new Result { Columns = Columns, Rows = Rows };
			DialogResult = true;
		}

		public static Result Run(Window parent, int? columns, int? rows)
		{
			var dialog = new CustomGridDialog(columns, rows) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
