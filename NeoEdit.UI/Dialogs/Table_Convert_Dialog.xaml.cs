using System;
using System.Collections.Generic;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Enums;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Table_Convert_Dialog
	{
		[DepProp]
		public ParserType TableType { get { return UIHelper<Table_Convert_Dialog>.GetPropValue<ParserType>(this); } set { UIHelper<Table_Convert_Dialog>.SetPropValue(this, value); } }

		public List<ParserType> TableTypes { get; } = new List<ParserType> { ParserType.TSV, ParserType.CSV, ParserType.Columns, ParserType.ExactColumns };

		static Table_Convert_Dialog() { UIHelper<Table_Convert_Dialog>.Register(); }

		Table_Convert_Dialog(ParserType tableType)
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
			var dialog = new Table_Convert_Dialog(tableType) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
