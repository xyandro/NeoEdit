using System;
using System.Linq;
using System.Windows;
using NeoEdit.Common;
using NeoEdit.Common.Configuration;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Table_Database_GenerateInserts_Dialog
	{
		[DepProp]
		public Table Table { get { return UIHelper<Table_Database_GenerateInserts_Dialog>.GetPropValue<Table>(this); } set { UIHelper<Table_Database_GenerateInserts_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public string TableName { get { return UIHelper<Table_Database_GenerateInserts_Dialog>.GetPropValue<string>(this); } set { UIHelper<Table_Database_GenerateInserts_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public int BatchSize { get { return UIHelper<Table_Database_GenerateInserts_Dialog>.GetPropValue<int>(this); } set { UIHelper<Table_Database_GenerateInserts_Dialog>.SetPropValue(this, value); } }

		static Table_Database_GenerateInserts_Dialog() { UIHelper<Table_Database_GenerateInserts_Dialog>.Register(); }

		Table_Database_GenerateInserts_Dialog(Table table, string tableName)
		{
			InitializeComponent();
			Table = table;
			Enumerable.Range(0, Table.NumColumns).ToList().ForEach(column => this.table.Selected.Add(column));
			TableName = tableName;
			BatchSize = 500;
		}

		Configuration_Table_Database_GenerateInserts result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrWhiteSpace(TableName))
				throw new Exception("Table name must be set");
			if (BatchSize < 1)
				throw new Exception("Invalid batch size");
			if (table.Selected.Count == 0)
				throw new Exception("Please select the columns to use");

			result = new Configuration_Table_Database_GenerateInserts
			{
				Columns = table.Selected.ToList(),
				BatchSize = BatchSize,
				TableName = TableName,
			};
			DialogResult = true;
		}

		public static Configuration_Table_Database_GenerateInserts Run(Window parent, Table table, string tableName)
		{
			var dialog = new Table_Database_GenerateInserts_Dialog(table, tableName) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
