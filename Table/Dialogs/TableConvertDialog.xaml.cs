using System.Collections.Generic;
using System.Windows;
using NeoEdit.Controls;

namespace NeoEdit.Dialogs
{
	partial class TableConvertDialog
	{
		public class Result
		{
			public ParserType TableType { get; set; }
		}

		[DepProp]
		public ParserType TableType { get { return UIHelper<TableConvertDialog>.GetPropValue<ParserType>(this); } set { UIHelper<TableConvertDialog>.SetPropValue(this, value); } }

		public List<ParserType> TableTypes { get; } = new List<ParserType> { ParserType.TSV, ParserType.CSV, ParserType.Columns, ParserType.ExactColumns };

		static TableConvertDialog() { UIHelper<TableConvertDialog>.Register(); }

		TableConvertDialog(ParserType tableType)
		{
			InitializeComponent();
			TableType = tableType;
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result { TableType = TableType };
			DialogResult = true;
		}

		static public Result Run(Window parent, ParserType tableType)
		{
			var dialog = new TableConvertDialog(tableType) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
