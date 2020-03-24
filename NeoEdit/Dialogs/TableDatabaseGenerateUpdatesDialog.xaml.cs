using System;
using System.Linq;
using System.Windows;
using NeoEdit.Common;
using NeoEdit.Common.Models;
using NeoEdit.Program.Controls;

namespace NeoEdit.Program.Dialogs
{
	partial class TableDatabaseGenerateUpdatesDialog
	{
		[DepProp]
		public Table Table { get { return UIHelper<TableDatabaseGenerateUpdatesDialog>.GetPropValue<Table>(this); } set { UIHelper<TableDatabaseGenerateUpdatesDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string TableName { get { return UIHelper<TableDatabaseGenerateUpdatesDialog>.GetPropValue<string>(this); } set { UIHelper<TableDatabaseGenerateUpdatesDialog>.SetPropValue(this, value); } }

		static TableDatabaseGenerateUpdatesDialog() { UIHelper<TableDatabaseGenerateUpdatesDialog>.Register(); }

		TableDatabaseGenerateUpdatesDialog(Table table, string tableName)
		{
			InitializeComponent();
			Table = table;
			var id = Enumerable.Range(0, Table.NumColumns).FirstOrDefault(column => table.GetHeader(column).ToLowerInvariant().Contains("id"));
			Enumerable.Range(0, Table.NumColumns).Where(column => column != id).ToList().ForEach(column => update.Selected.Add(column));
			where.Selected.Add(id);
			TableName = tableName;
		}

		TableDatabaseGenerateUpdatesDialogResult result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrWhiteSpace(TableName))
				throw new Exception("Table name must be set");
			if (update.Selected.Count == 0)
				throw new Exception("Please select the columns to update");
			if (where.Selected.Count == 0)
				throw new Exception("Please select the columns to limit the update");

			result = new TableDatabaseGenerateUpdatesDialogResult
			{
				Update = update.Selected.ToList(),
				Where = where.Selected.ToList(),
				TableName = TableName,
			};
			DialogResult = true;
		}

		public static TableDatabaseGenerateUpdatesDialogResult Run(Window parent, Table table, string tableName)
		{
			var dialog = new TableDatabaseGenerateUpdatesDialog(table, tableName) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
