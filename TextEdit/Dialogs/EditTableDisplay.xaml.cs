using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using NeoEdit.GUI.Controls;
using NeoEdit.GUI.Misc;

namespace NeoEdit.TextEdit.Dialogs
{
	partial class EditTableDisplay
	{
		[DepProp]
		public Table Table { get { return UIHelper<EditTableDisplay>.GetPropValue<Table>(this); } set { UIHelper<EditTableDisplay>.SetPropValue(this, value); } }
		[DepProp]
		public int SelectedColumn { get { return UIHelper<EditTableDisplay>.GetPropValue<int>(this); } set { UIHelper<EditTableDisplay>.SetPropValue(this, value); } }

		static EditTableDisplay()
		{
			UIHelper<EditTableDisplay>.Register();
			UIHelper<EditTableDisplay>.AddCallback(a => a.Table, (obj, o, n) => obj.SetupTable());
			UIHelper<EditTableDisplay>.AddCallback(a => a.SelectedColumn, (obj, o, n) => obj.SetupSelection());
		}

		readonly Rectangle selection = new Rectangle { Fill = new LinearGradientBrush(Colors.LightBlue, Colors.AliceBlue, 0) };
		public EditTableDisplay()
		{
			InitializeComponent();
			SelectedColumn = 0;
		}

		void SetupTable()
		{
			tableGrid.RowDefinitions.Clear();
			tableGrid.ColumnDefinitions.Clear();
			tableGrid.Children.Clear();

			if (Table == null)
				return;

			var rows = Table.NumRows;
			var columns = Table.NumColumns;

			for (var column = 0; column < columns; ++column)
				tableGrid.ColumnDefinitions.Add(new ColumnDefinition());

			tableGrid.Children.Add(selection);

			for (var row = 0; row <= rows; ++row)
			{
				tableGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(Font.Size + 2) });
				for (var column = 0; column < columns; ++column)
				{
					var text = new TextBlock();
					if (row == 0)
					{
						text.Text = Table.GetHeader(column);
						text.Background = Brushes.DarkGray;
					}
					else
						text.Text = Table[row - 1, column]?.ToString() ?? "";
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

			var selectedColumn = Math.Max(0, Math.Min(SelectedColumn, Table.NumColumns - 1));
			Grid.SetColumn(selection, selectedColumn);
			Grid.SetRowSpan(selection, Table.NumRows + 1);
		}
	}
}
