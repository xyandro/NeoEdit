using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using NeoEdit.GUI.Controls;

namespace NeoEdit.TextEdit.Dialogs
{
	partial class QueryTableDialog
	{
		[DepProp]
		public string Table { get { return UIHelper<QueryTableDialog>.GetPropValue<string>(this); } set { UIHelper<QueryTableDialog>.SetPropValue(this, value); } }

		static QueryTableDialog() { UIHelper<QueryTableDialog>.Register(); }

		readonly HashSet<string> tables;
		QueryTableDialog(IEnumerable<string> tables)
		{
			this.tables = new HashSet<string>(tables);
			InitializeComponent();
			tablesList.AddSuggestions(tables.ToArray());
			tablesList.IsDropDownOpen = true;
		}

		string result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrWhiteSpace(Table))
				throw new Exception("No table selected.");
			if (!tables.Contains(Table))
				throw new Exception("Invalid table.");
			result = Table;
			tablesList.AddCurrentSuggestion();
			DialogResult = true;
		}

		public static string Run(Window parent, List<string> tables)
		{
			var dialog = new QueryTableDialog(tables) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
