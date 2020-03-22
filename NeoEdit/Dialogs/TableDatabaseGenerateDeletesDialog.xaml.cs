using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using NeoEdit.Program;
using NeoEdit.Program.Controls;
using NeoEdit.Program.Models;

namespace NeoEdit.Program.Dialogs
{
	partial class TableDatabaseGenerateDeletesDialog
	{
		[DepProp]
		public Table Table { get { return UIHelper<TableDatabaseGenerateDeletesDialog>.GetPropValue<Table>(this); } set { UIHelper<TableDatabaseGenerateDeletesDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string TableName { get { return UIHelper<TableDatabaseGenerateDeletesDialog>.GetPropValue<string>(this); } set { UIHelper<TableDatabaseGenerateDeletesDialog>.SetPropValue(this, value); } }

		static TableDatabaseGenerateDeletesDialog() { UIHelper<TableDatabaseGenerateDeletesDialog>.Register(); }

		TableDatabaseGenerateDeletesDialog(Table table, string tableName)
		{
			InitializeComponent();
			Table = table;
			var id = Enumerable.Range(0, Table.NumColumns).FirstOrDefault(column => table.GetHeader(column).ToLowerInvariant().Contains("id"));
			where.Selected.Add(id);
			TableName = tableName;
		}

		TableDatabaseGenerateDeletesDialogResult result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrWhiteSpace(TableName))
				throw new Exception("Table name must be set");
			if (where.Selected.Count == 0)
				throw new Exception("Please select the columns to limit the update");

			result = new TableDatabaseGenerateDeletesDialogResult
			{
				Where = where.Selected.ToList(),
				TableName = TableName,
			};
			DialogResult = true;
		}

		static public TableDatabaseGenerateDeletesDialogResult Run(Window parent, Table table, string tableName)
		{
			var dialog = new TableDatabaseGenerateDeletesDialog(table, tableName) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
