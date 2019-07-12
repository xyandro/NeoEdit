using System.Windows;
using NeoEdit.Program.Controls;

namespace NeoEdit.Program.Dialogs
{
	partial class WindowCustomGridDialog
	{
		public class Result
		{
			public int? Columns { get; set; }
			public int? Rows { get; set; }
		}

		[DepProp]
		public int? Columns { get { return UIHelper<WindowCustomGridDialog>.GetPropValue<int?>(this); } set { UIHelper<WindowCustomGridDialog>.SetPropValue(this, value); } }
		[DepProp]
		public int? Rows { get { return UIHelper<WindowCustomGridDialog>.GetPropValue<int?>(this); } set { UIHelper<WindowCustomGridDialog>.SetPropValue(this, value); } }

		static WindowCustomGridDialog() { UIHelper<WindowCustomGridDialog>.Register(); }

		WindowCustomGridDialog(int? columns, int? rows)
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
			var dialog = new WindowCustomGridDialog(columns, rows) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
