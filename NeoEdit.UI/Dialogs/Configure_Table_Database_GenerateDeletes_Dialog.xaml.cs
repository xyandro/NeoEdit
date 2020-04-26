using System;
using System.Linq;
using System.Windows;
using NeoEdit.Common;
using NeoEdit.Common.Configuration;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Configure_Table_Database_GenerateDeletes_Dialog
	{
		[DepProp]
		public Table Table { get { return UIHelper<Configure_Table_Database_GenerateDeletes_Dialog>.GetPropValue<Table>(this); } set { UIHelper<Configure_Table_Database_GenerateDeletes_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public string TableName { get { return UIHelper<Configure_Table_Database_GenerateDeletes_Dialog>.GetPropValue<string>(this); } set { UIHelper<Configure_Table_Database_GenerateDeletes_Dialog>.SetPropValue(this, value); } }

		static Configure_Table_Database_GenerateDeletes_Dialog() { UIHelper<Configure_Table_Database_GenerateDeletes_Dialog>.Register(); }

		Configure_Table_Database_GenerateDeletes_Dialog(Table table, string tableName)
		{
			InitializeComponent();
			Table = table;
			var id = Enumerable.Range(0, Table.NumColumns).FirstOrDefault(column => table.GetHeader(column).ToLowerInvariant().Contains("id"));
			where.Selected.Add(id);
			TableName = tableName;
		}

		Configuration_Table_Database_GenerateDeletes result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrWhiteSpace(TableName))
				throw new Exception("Table name must be set");
			if (where.Selected.Count == 0)
				throw new Exception("Please select the columns to limit the update");

			result = new Configuration_Table_Database_GenerateDeletes
			{
				Where = where.Selected.ToList(),
				TableName = TableName,
			};
			DialogResult = true;
		}

		public static Configuration_Table_Database_GenerateDeletes Run(Window parent, Table table, string tableName)
		{
			var dialog = new Configure_Table_Database_GenerateDeletes_Dialog(table, tableName) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
