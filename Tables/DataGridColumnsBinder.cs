using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace NeoEdit.Tables
{
	public class DataGridColumnsBinder
	{
		public static readonly DependencyProperty ColumnsProperty = DependencyProperty.RegisterAttached("Columns", typeof(ObservableCollection<Table.Header>), typeof(DataGridColumnsBinder), new UIPropertyMetadata(null, ColumnsPropertyChanged));

		static void ColumnsPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
		{
			var dataGrid = sender as DataGrid;
			var columns = e.NewValue as ObservableCollection<Table.Header>;

			SetupColumns(dataGrid, columns);

			columns.CollectionChanged += (sender2, e2) => SetupColumns(dataGrid, columns);
		}

		static void SetupColumns(DataGrid dataGrid, ObservableCollection<Table.Header> columns)
		{
			dataGrid.Columns.Clear();
			if (columns == null)
				return;

			for (var column = 0; column < columns.Count; ++column)
			{
				var dataGridColumn = new DataGridTextColumn { Header = columns[column].Name, Binding = new Binding(String.Format("[{0}]", column)) };
				if (columns[column].Type == typeof(long))
				{
					var rightAlignCellStyle = new Style(typeof(TextBlock));
					rightAlignCellStyle.Setters.Add(new Setter(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Right));
					dataGridColumn.ElementStyle = rightAlignCellStyle;
				}
				dataGrid.Columns.Add(dataGridColumn);
			}
		}

		public static void SetColumns(DependencyObject element, ObservableCollection<Table.Header> value)
		{
			element.SetValue(ColumnsProperty, value);
		}

		public static ObservableCollection<Table.Header> GetColumns(DependencyObject element)
		{
			return (ObservableCollection<Table.Header>)element.GetValue(ColumnsProperty);
		}
	}
}
