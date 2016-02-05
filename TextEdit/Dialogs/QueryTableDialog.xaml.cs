using System;
using System.Collections.Generic;
using System.Windows;
using NeoEdit.GUI.Controls;

namespace NeoEdit.TextEdit.Dialogs
{
	partial class QueryTableDialog
	{
		[DepProp]
		public string Table { get { return UIHelper<QueryTableDialog>.GetPropValue<string>(this); } set { UIHelper<QueryTableDialog>.SetPropValue(this, value); } }

		public List<string> Tables { get; }

		static QueryTableDialog() { UIHelper<QueryTableDialog>.Register(); }

		QueryTableDialog(List<string> tables)
		{
			Tables = tables;
			InitializeComponent();
		}

		string result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrWhiteSpace(Table))
				throw new Exception("No table selected.");
			result = Table;
			DialogResult = true;
		}

		public static string Run(Window parent, List<string> tables)
		{
			var dialog = new QueryTableDialog(tables) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
