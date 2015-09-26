using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace NeoEdit.Tables
{
	class TableColumn : DataGridTextColumn
	{
		public int Column { get; private set; }
		public Table.Header TableHeader { get; private set; }

		public TableColumn(int column, Table.Header tableHeader)
		{
			Column = column;
			TableHeader = tableHeader;

			var header = new StackPanel();
			header.Children.Add(new Label { Content = tableHeader.Name, HorizontalAlignment = HorizontalAlignment.Center });
			header.Children.Add(new Label { Content = tableHeader.Type.Name, HorizontalAlignment = HorizontalAlignment.Center });
			Header = header;
			Binding = new Binding(String.Format("[{0}]", column));
			if (tableHeader.Type == typeof(long))
			{
				var rightAlignCellStyle = new Style(typeof(TextBlock));
				rightAlignCellStyle.Setters.Add(new Setter(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Right));
				ElementStyle = rightAlignCellStyle;
			}
		}

		public override string ToString()
		{
			return TableHeader.Name;
		}
	}
}
