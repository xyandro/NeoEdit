using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using NeoEdit.GUI.Controls;

namespace NeoEdit.TextEdit.Dialogs
{
	internal partial class GenerateUpdatesDialog
	{
		internal class Result
		{
			public List<int> Update { get; set; }
			public List<int> Where { get; set; }
			public string TableName { get; set; }
		}

		[DepProp]
		public Table Table { get { return UIHelper<GenerateUpdatesDialog>.GetPropValue<Table>(this); } set { UIHelper<GenerateUpdatesDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string TableName { get { return UIHelper<GenerateUpdatesDialog>.GetPropValue<string>(this); } set { UIHelper<GenerateUpdatesDialog>.SetPropValue(this, value); } }

		static GenerateUpdatesDialog() { UIHelper<GenerateUpdatesDialog>.Register(); }

		GenerateUpdatesDialog(Table table, string tableName)
		{
			InitializeComponent();
			Table = table;
			var id = Enumerable.Range(0, Table.NumColumns).FirstOrDefault(column => table.GetHeader(column).ToLowerInvariant().Contains("id"));
			Enumerable.Range(0, Table.NumColumns).Where(column => column != id).ToList().ForEach(column => update.Selected.Add(column));
			where.Selected.Add(id);
			TableName = tableName;
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrWhiteSpace(TableName))
				throw new Exception("Table name must be set");
			if (update.Selected.Count == 0)
				throw new Exception("Please select the columns to update");
			if (where.Selected.Count == 0)
				throw new Exception("Please select the columns to limit the update");

			result = new Result
			{
				Update = update.Selected.ToList(),
				Where = where.Selected.ToList(),
				TableName = TableName,
			};
			DialogResult = true;
		}

		static public Result Run(Window parent, Table table, string tableName)
		{
			var dialog = new GenerateUpdatesDialog(table, tableName) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
