using System;
using System.Linq;
using System.Windows;
using NeoEdit.Common;
using NeoEdit.Common.Configuration;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
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

		Configuration_Table_Database_GenerateUpdates result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrWhiteSpace(TableName))
				throw new Exception("Table name must be set");
			if (update.Selected.Count == 0)
				throw new Exception("Please select the columns to update");
			if (where.Selected.Count == 0)
				throw new Exception("Please select the columns to limit the update");

			result = new Configuration_Table_Database_GenerateUpdates
			{
				Update = update.Selected.ToList(),
				Where = where.Selected.ToList(),
				TableName = TableName,
			};
			DialogResult = true;
		}

		public static Configuration_Table_Database_GenerateUpdates Run(Window parent, Table table, string tableName)
		{
			var dialog = new TableDatabaseGenerateUpdatesDialog(table, tableName) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
