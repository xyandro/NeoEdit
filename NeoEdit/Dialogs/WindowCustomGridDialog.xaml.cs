using System;
using System.Windows;
using NeoEdit.Common.Models;
using NeoEdit.Program.Controls;

namespace NeoEdit.Program.Dialogs
{
	partial class WindowCustomGridDialog
	{
		[DepProp]
		public int? Columns { get { return UIHelper<WindowCustomGridDialog>.GetPropValue<int?>(this); } set { UIHelper<WindowCustomGridDialog>.SetPropValue(this, value); } }
		[DepProp]
		public int? Rows { get { return UIHelper<WindowCustomGridDialog>.GetPropValue<int?>(this); } set { UIHelper<WindowCustomGridDialog>.SetPropValue(this, value); } }
		[DepProp]
		public int? MaxColumns { get { return UIHelper<WindowCustomGridDialog>.GetPropValue<int?>(this); } set { UIHelper<WindowCustomGridDialog>.SetPropValue(this, value); } }
		[DepProp]
		public int? MaxRows { get { return UIHelper<WindowCustomGridDialog>.GetPropValue<int?>(this); } set { UIHelper<WindowCustomGridDialog>.SetPropValue(this, value); } }

		static WindowCustomGridDialog() { UIHelper<WindowCustomGridDialog>.Register(); }

		WindowCustomGridDialog(int? columns, int? rows, int? maxColumns, int? maxRows)
		{
			InitializeComponent();
			Columns = columns;
			Rows = rows;
			MaxColumns = maxColumns;
			MaxRows = maxRows;
		}

		WindowCustomGridDialogResult result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if ((Columns < 1) || (Rows < 1) || (MaxColumns < 1) || (MaxRows < 1))
				return;
			result = new WindowCustomGridDialogResult { Columns = Columns, Rows = Rows, MaxColumns = MaxColumns, MaxRows = MaxRows };
			DialogResult = true;
		}

		public static WindowCustomGridDialogResult Run(Window parent, int? columns, int? rows, int? maxColumns, int? maxRows)
		{
			var dialog = new WindowCustomGridDialog(columns, rows, maxColumns, maxRows) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
