using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace NeoEdit.TableEdit
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
				dataGrid.Columns.Add(new TableColumn(column, columns[column]));
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
