using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using NeoEdit;
using NeoEdit.Controls;
using NeoEdit.Parsing;

namespace NeoEdit.Dialogs
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
		[DepProp]
		public int YScrollValue { get { return UIHelper<TableDisplay>.GetPropValue<int>(this); } set { UIHelper<TableDisplay>.SetPropValue(this, value); } }

		static readonly double RowHeight;
		static TableDisplay()
		{
			UIHelper<TableDisplay>.Register();
			UIHelper<TableDisplay>.AddCallback(a => a.Table, (obj, o, n) => obj.SetupTable());
			UIHelper<TableDisplay>.AddCallback(a => a.SelectedColumn, (obj, o, n) => obj.SetupSelection());
			UIHelper<TableDisplay>.AddObservableCallback(a => a.Selected, (obj, o, n) => obj.SetupSelection());
			UIHelper<TableDisplay>.AddCallback(a => a.YScrollValue, (obj, o, n) => obj.SetupTable());
			RowHeight = CalcRowHeight();
		}

		static double CalcRowHeight()
		{
			var text = new TextBlock { Text = "THE QUICK BROWN FOX JUMPED OVER THE LAZY DOGS the quick brown fox jumped over the lazy dogs" };
			text.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
			return text.DesiredSize.Height;
		}

		public TableDisplay()
		{
			InitializeComponent();
			Selected = new ObservableCollection<int>();
			Loaded += (s, e) => SetupSelection();
			xScroller.ScrollChanged += (s, e) => { if (e.ViewportHeightChange != 0) SetupTable(); };
		}

		bool controlDown => (Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.None;

		protected override void OnPreviewKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);
			if ((Table == null) || (Table.NumColumns == 0))
				return;

			e.Handled = true;
			switch (e.Key)
			{
				case Key.A:
					if ((!controlDown) || (!Selectable))
						e.Handled = false;
					else
						for (var ctr = 0; ctr < Table.NumColumns; ++ctr)
							if (!Selected.Contains(ctr))
								Selected.Add(ctr);
					break;
				case Key.N:
					if ((!controlDown) || (!Selectable))
						e.Handled = false;
					else
						Selected.Clear();
					break;
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

			if ((Table == null) || (xScroller.ViewportHeight == 0))
				return;

			var viewportRowsFloor = (int)Math.Floor(xScroller.ViewportHeight / RowHeight);
			var viewportRowsCeiling = (int)Math.Ceiling(xScroller.ViewportHeight / RowHeight);

			yScroller.Minimum = 0;
			yScroller.Maximum = Math.Max(0, Table.NumRows - viewportRowsCeiling + 2);
			yScroller.SmallChange = 1;
			yScroller.LargeChange = yScroller.ViewportSize = Math.Max(0, viewportRowsFloor - 2);

			for (var column = 0; column < Table.NumColumns; ++column)
				tableGrid.ColumnDefinitions.Add(new ColumnDefinition());

			var rows = Math.Min(viewportRowsCeiling, Table.NumRows - YScrollValue + 1);
			for (var row = 0; row < rows; ++row)
			{
				tableGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(RowHeight) });
				for (var column = 0; column < Table.NumColumns; ++column)
				{
					var text = new TextBlock { Padding = new Thickness(5, 0, 5, 0) };
					if (row == 0)
					{
						text.Text = Table.GetHeader(column)?.Replace("\r", "").Replace("\n", "").Trim() ?? "";
						text.Background = Brushes.DarkGray;
					}
					else
						text.Text = Table[row - 1 + YScrollValue, column]?.Replace("\r", "").Replace("\n", "").Trim() ?? "";
					Grid.SetRow(text, row);
					Grid.SetColumn(text, column);
					tableGrid.Children.Add(text);
				}
			}

			tableGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

			SetupSelection();
		}

		void SetupSelection()
		{
			if ((Table == null) || (Table.NumColumns == 0) || (xScroller.ViewportHeight == 0))
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
			Grid.SetRowSpan(selection, tableGrid.RowDefinitions.Count);

			foreach (var selected in Selected)
			{
				var rect = new Rectangle { Fill = new LinearGradientBrush(Colors.LightGreen, Colors.GreenYellow, 0) { Opacity = .7 } };
				tableGrid.Children.Insert(1, rect);
				Grid.SetColumn(rect, selected);
				Grid.SetRowSpan(rect, tableGrid.RowDefinitions.Count);
			}

			var columnLeft = tableGrid.ColumnDefinitions.Take(SelectedColumn).Sum(columnDef => columnDef.ActualWidth);
			var columnRight = columnLeft + tableGrid.ColumnDefinitions[SelectedColumn].ActualWidth;
			xScroller.ScrollToHorizontalOffset(Math.Min(Math.Max(xScroller.HorizontalOffset, columnRight - xScroller.ViewportWidth), columnLeft));
		}
	}
}
