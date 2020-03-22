using System;
using System.Collections.Generic;
using System.Windows;
using NeoEdit.Program;
using NeoEdit.Program.Controls;
using NeoEdit.Program.Models;

namespace NeoEdit.Program.Dialogs
{
	partial class TableConvertDialog
	{
		[DepProp]
		public ParserType TableType { get { return UIHelper<TableConvertDialog>.GetPropValue<ParserType>(this); } set { UIHelper<TableConvertDialog>.SetPropValue(this, value); } }

		public List<ParserType> TableTypes { get; } = new List<ParserType> { ParserType.TSV, ParserType.CSV, ParserType.Columns, ParserType.ExactColumns };

		static TableConvertDialog() { UIHelper<TableConvertDialog>.Register(); }

		TableConvertDialog(ParserType tableType)
		{
			InitializeComponent();
			TableType = tableType;
		}

		TableConvertDialogResult result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new TableConvertDialogResult { TableType = TableType };
			DialogResult = true;
		}

		static public TableConvertDialogResult Run(Window parent, ParserType tableType)
		{
			var dialog = new TableConvertDialog(tableType) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
