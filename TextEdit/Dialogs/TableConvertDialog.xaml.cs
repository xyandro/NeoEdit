using System.Collections.Generic;
using System.Windows;
using NeoEdit.GUI.Controls;
using NeoEdit.TextEdit.Content;

namespace NeoEdit.TextEdit.Dialogs
{
	internal partial class TableConvertDialog
	{
		internal class Result
		{
			public Parser.ParserType TableType { get; set; }
		}

		[DepProp]
		public Parser.ParserType TableType { get { return UIHelper<TableConvertDialog>.GetPropValue<Parser.ParserType>(this); } set { UIHelper<TableConvertDialog>.SetPropValue(this, value); } }

		public List<Parser.ParserType> TableTypes { get; } = new List<Parser.ParserType> { Parser.ParserType.TSV, Parser.ParserType.CSV, Parser.ParserType.Columns, Parser.ParserType.ExactColumns };

		static TableConvertDialog() { UIHelper<TableConvertDialog>.Register(); }

		TableConvertDialog(Parser.ParserType tableType)
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

		static public Result Run(Window parent, Parser.ParserType tableType)
		{
			var dialog = new TableConvertDialog(tableType) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
