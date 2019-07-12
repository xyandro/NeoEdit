using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using NeoEdit.Program;
using NeoEdit.Program.Controls;

namespace NeoEdit.Program.Dialogs
{
	partial class TableDatabaseGenerateInsertsDialog
	{
		public class Result
		{
			public List<int> Columns { get; set; }
			public int BatchSize { get; set; }
			public string TableName { get; set; }
		}

		[DepProp]
		public Table Table { get { return UIHelper<TableDatabaseGenerateInsertsDialog>.GetPropValue<Table>(this); } set { UIHelper<TableDatabaseGenerateInsertsDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string TableName { get { return UIHelper<TableDatabaseGenerateInsertsDialog>.GetPropValue<string>(this); } set { UIHelper<TableDatabaseGenerateInsertsDialog>.SetPropValue(this, value); } }
		[DepProp]
		public int BatchSize { get { return UIHelper<TableDatabaseGenerateInsertsDialog>.GetPropValue<int>(this); } set { UIHelper<TableDatabaseGenerateInsertsDialog>.SetPropValue(this, value); } }

		static TableDatabaseGenerateInsertsDialog() { UIHelper<TableDatabaseGenerateInsertsDialog>.Register(); }

		TableDatabaseGenerateInsertsDialog(Table table, string tableName)
		{
			InitializeComponent();
			Table = table;
			Enumerable.Range(0, Table.NumColumns).ToList().ForEach(column => this.table.Selected.Add(column));
			TableName = tableName;
			BatchSize = 500;
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrWhiteSpace(TableName))
				throw new Exception("Table name must be set");
			if (BatchSize < 1)
				throw new Exception("Invalid batch size");
			if (table.Selected.Count == 0)
				throw new Exception("Please select the columns to use");

			result = new Result
			{
				Columns = table.Selected.ToList(),
				BatchSize = BatchSize,
				TableName = TableName,
			};
			DialogResult = true;
		}

		static public Result Run(Window parent, Table table, string tableName)
		{
			var dialog = new TableDatabaseGenerateInsertsDialog(table, tableName) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
