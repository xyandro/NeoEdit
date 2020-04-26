using System;
using System.Collections.Generic;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Enums;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class TableConvertDialog
	{
		[DepProp]
		public ParserType TableType { get { return UIHelper<TableConvertDialog>.GetPropValue<ParserType>(this); } set { UIHelper<TableConvertDialog>.SetPropValue(this, value); } }

		public List<ParserType> TableTypes { get; } = new List<ParserType> { ParserType.TSV, ParserType.CSV, ParserType.Columns, ParserType.ExactColumns };

		static TableConvertDialog() { UIHelper<TableConvertDialog>.Register(); }

		TableConvertDialog(ParserType tableType)
		{
			InitializeComponent();
			TableType = tableType;
		}

		Configuration_Table_Convert result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Configuration_Table_Convert { TableType = TableType };
			DialogResult = true;
		}

		public static Configuration_Table_Convert Run(Window parent, ParserType tableType)
		{
			var dialog = new TableConvertDialog(tableType) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
