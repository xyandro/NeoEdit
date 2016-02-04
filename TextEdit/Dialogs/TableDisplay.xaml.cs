using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using NeoEdit.GUI.Controls;
using NeoEdit.GUI.Misc;

namespace NeoEdit.TextEdit.Dialogs
{
	partial class TableDisplay
	{
		[DepProp]
		public Table Table { get { return UIHelper<TableDisplay>.GetPropValue<Table>(this); } set { UIHelper<TableDisplay>.SetPropValue(this, value); } }
		[DepProp(BindsTwoWayByDefault = true)]
		public int SelectedColumn { get { return UIHelper<TableDisplay>.GetPropValue<int>(this); } set { UIHelper<TableDisplay>.SetPropValue(this, value); } }
		[DepProp]
		public bool Selectable { get { return UIHelper<TableDisplay>.GetPropValue<bool>(this); } set { UIHelper<TableDisplay>.SetPropValue(this, value); } }
		[DepProp]
		public ObservableCollection<int> Selected { get { return UIHelper<TableDisplay>.GetPropValue<ObservableCollection<int>>(this); } set { UIHelper<TableDisplay>.SetPropValue(this, value); } }

		static TableDisplay()
		{
			UIHelper<TableDisplay>.Register();
			UIHelper<TableDisplay>.AddCallback(a => a.Table, (obj, o, n) => obj.SetupTable());
			UIHelper<TableDisplay>.AddCallback(a => a.SelectedColumn, (obj, o, n) => obj.SetupSelection());
			UIHelper<TableDisplay>.AddObservableCallback(a => a.Selected, (obj, o, n) => obj.SetupSelection());
		}

		public TableDisplay()
		{
			InitializeComponent();
			Selected = new ObservableCollection<int>();
			Loaded += (s, e) => SetupSelection();
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);
			if ((Table == null) || (Table.NumColumns == 0))
				return;

			e.Handled = true;
			switch (e.Key)
			{
				case Key.Left: --SelectedColumn; break;
				case Key.Right: ++SelectedColumn; break;
				case Key.Home: SelectedColumn = 0; break;
				case Key.End: SelectedColumn = Table.NumColumns - 1; break;
				case Key.Up: case Key.Down: break;
				case Key.Space:
					if (Selectable)
					{
						if (Selected.Contains(SelectedColumn))
							Selected.Remove(SelectedColumn);
						else
							Selected.Add(SelectedColumn);
					}
					else
						e.Handled = false;
					break;
				default: e.Handled = false; break;
			}
		}

		protected override void OnMouseDown(MouseButtonEventArgs e)
		{
			base.OnMouseDown(e);
			Focus();
		}

		void GridMouseDown(object sender, MouseButtonEventArgs e)
		{
			var x = e.GetPosition(tableGrid).X;
			for (var column = 0; column < tableGrid.ColumnDefinitions.Count; ++column)
			{
				var width = tableGrid.ColumnDefinitions[column].ActualWidth;
				if (x < width)
				{
					SelectedColumn = column;
					return;
				}
				x -= width;
			}
		}

		void SetupTable()
		{
			tableGrid.RowDefinitions.Clear();
			tableGrid.ColumnDefinitions.Clear();
			tableGrid.Children.Clear();

			if (Table == null)
				return;

			for (var column = 0; column < Table.NumColumns; ++column)
				tableGrid.ColumnDefinitions.Add(new ColumnDefinition());

			for (var row = 0; row <= Table.NumRows; ++row)
			{
				tableGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(Font.Size + 2) });
				for (var column = 0; column < Table.NumColumns; ++column)
				{
					var text = new TextBlock { Padding = new Thickness(5, 0, 5, 0) };
					if (row == 0)
					{
						text.Text = Table.GetHeader(column);
						text.Background = Brushes.DarkGray;
					}
					else
						text.Text = Table[row - 1, column];
					Grid.SetRow(text, row);
					Grid.SetColumn(text, column);
					tableGrid.Children.Add(text);
				}
			}

			SetupSelection();
		}

		void SetupSelection()
		{
			if ((Table == null) || (Table.NumColumns == 0))
				return;

			// Remove old highlighting
			tableGrid.Children.OfType<Rectangle>().ToList().ForEach(child => tableGrid.Children.Remove(child));

			var selectedColumn = Math.Max(0, Math.Min(SelectedColumn, Table.NumColumns - 1));
			if (selectedColumn != SelectedColumn)
			{
				SelectedColumn = selectedColumn;
				return;
			}

			var selection = new Rectangle { Fill = new LinearGradientBrush(Colors.LightBlue, Colors.AliceBlue, 0) };
			tableGrid.Children.Insert(0, selection);
			Grid.SetColumn(selection, SelectedColumn);
			Grid.SetRowSpan(selection, Table.NumRows + 1);

			foreach (var selected in Selected)
			{
				var rect = new Rectangle { Fill = new LinearGradientBrush(Colors.LightGreen, Colors.GreenYellow, 0) { Opacity = .7 } };
				tableGrid.Children.Insert(1, rect);
				Grid.SetColumn(rect, selected);
				Grid.SetRowSpan(rect, Table.NumRows + 1);
			}

			var columnLeft = tableGrid.ColumnDefinitions.Take(SelectedColumn).Sum(columnDef => columnDef.ActualWidth);
			var columnRight = columnLeft + tableGrid.ColumnDefinitions[SelectedColumn].ActualWidth;
			scroller.ScrollToHorizontalOffset(Math.Min(Math.Max(scroller.HorizontalOffset, columnRight - scroller.ViewportWidth), columnLeft));

		}
	}
}
