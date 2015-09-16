using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using NeoEdit.GUI.Controls;

namespace NeoEdit.TextEdit.Dialogs
{
	partial class EditTableTab
	{
		public class JoinInfo
		{
			public int RightTable { get; internal set; }
			public int LeftColumn { get; internal set; }
			public int RightColumn { get; internal set; }
			public Table.JoinType JoinType { get; internal set; }
		}

		class DisplayColumn
		{
			public int InputColumn { get; set; }
			public Table.AggregateType AggregateType { get; set; }
			public int? SortOrder { get; set; }
			public bool SortAscending { get; set; }

			public DisplayColumn(int inputColumn, Table.AggregateType aggregateType)
			{
				InputColumn = inputColumn;
				AggregateType = aggregateType;
			}

			public override string ToString() { return String.Format("{0}: {1}", InputColumn, AggregateType); }
		}

		internal class Result
		{
			public Table.TableType InputTableType { get; internal set; }
			public Table.TableType OutputTableType { get; internal set; }
			public bool InputHasHeaders { get; internal set; }
			public bool OutputHasHeaders { get; internal set; }
			public List<JoinInfo> JoinInfos { get; internal set; }
			public List<int> GroupByColumns { get; internal set; }
			public List<Tuple<int, Table.AggregateType>> AggregateColumns { get; internal set; }
			public List<Tuple<int, bool>> SortColumns { get; internal set; }
		}

		[DepProp]
		public Table.TableType InputTableType { get { return UIHelper<EditTableTab>.GetPropValue<Table.TableType>(this); } set { UIHelper<EditTableTab>.SetPropValue(this, value); } }
		[DepProp]
		public Table.TableType OutputTableType { get { return UIHelper<EditTableTab>.GetPropValue<Table.TableType>(this); } set { UIHelper<EditTableTab>.SetPropValue(this, value); } }
		[DepProp]
		public bool InputHasHeaders { get { return UIHelper<EditTableTab>.GetPropValue<bool>(this); } set { UIHelper<EditTableTab>.SetPropValue(this, value); } }
		[DepProp]
		public bool OutputHasHeaders { get { return UIHelper<EditTableTab>.GetPropValue<bool>(this); } set { UIHelper<EditTableTab>.SetPropValue(this, value); } }
		[DepProp]
		public Table Table { get { return UIHelper<EditTableTab>.GetPropValue<Table>(this); } set { UIHelper<EditTableTab>.SetPropValue(this, value); } }

		static EditTableTab()
		{
			UIHelper<EditTableTab>.Register();
			UIHelper<EditTableTab>.AddCallback(a => a.InputTableType, (obj, o, n) => { obj.InputUpdated(true); obj.OutputTableType = obj.InputTableType; });
			UIHelper<EditTableTab>.AddCallback(a => a.InputHasHeaders, (obj, o, n) => obj.InputUpdated(false));
		}

		readonly EditTablesDialog parent;
		readonly string stringInput;
		readonly Table tableInput;
		Table inputTable, joinTable;
		List<JoinInfo> joinInfos = new List<JoinInfo>();
		List<int> groupByColumns = new List<int>();
		List<DisplayColumn> displayColumns = new List<DisplayColumn>();

		internal EditTableTab(EditTablesDialog parent, string tabName, string stringInput, Table tableInput)
		{
			this.parent = parent;
			Header = tabName;
			this.stringInput = stringInput;
			this.tableInput = tableInput;
			InitializeComponent();
			var tableTypes = Enum.GetValues(typeof(Table.TableType)).Cast<Table.TableType>().ToList();
			inputType.ItemsSource = tableTypes.Where(value => value != Table.TableType.None).ToList();
			outputType.ItemsSource = tableTypes;
			OutputHasHeaders = true;
			InputUpdated(true);
		}

		bool inInputUpdated = false;
		void InputUpdated(bool calcHasHeaders)
		{
			if (inInputUpdated)
				return;

			if (tableInput != null)
				inputTable = tableInput;
			else
				inputTable = new Table(stringInput, InputTableType, calcHasHeaders ? default(bool?) : InputHasHeaders);

			inInputUpdated = true;
			InputTableType = inputTable.OriginalTableType;
			InputHasHeaders = inputTable.HasHeaders;
			inInputUpdated = false;

			joinInfos = new List<JoinInfo>();
			groupByColumns = new List<int>();

			JoinData();
		}

		void JoinData()
		{
			joinTable = inputTable;
			foreach (var joinInfo in joinInfos)
				joinTable = Table.Join(joinTable, parent.GetTab(joinInfo.RightTable).inputTable, joinInfo.LeftColumn, joinInfo.RightColumn, joinInfo.JoinType);
			displayColumns = Enumerable.Range(0, joinTable.NumColumns).Select(column => new DisplayColumn(column, Table.AggregateType.None)).ToList();
			AggregateData();
		}

		void AggregateData()
		{
			var aggregateTypes = displayColumns.Select(column => Tuple.Create(column.InputColumn, column.AggregateType)).ToList();
			Table = joinTable.Aggregate(groupByColumns, aggregateTypes);
			SortData();
		}

		void SortData()
		{
			var sortOrder = displayColumns.Where(column => column.SortOrder.HasValue).OrderBy(column => column.SortOrder.Value).Select(column => Tuple.Create(displayColumns.IndexOf(column), column.SortAscending)).ToList();
			Table = Table.Sort(sortOrder);
			SetupLayout();
		}

		static Dictionary<Table.JoinType, Geometry> joinGeometries = new Dictionary<Table.JoinType, Geometry>
		{
			{ Table.JoinType.Inner, Geometry.Parse("M 0,5 0,15 5,10 0,5 M 5,9 10,9 10,11 5,11 5,9 M 15,5 15,15 10,10 15,5") },
			{ Table.JoinType.LeftOuter, Geometry.Parse("M 5,5 5,15 0,10 5,5 M 5,9 15,9 15,11 5,11 5,9") },
			{ Table.JoinType.RightOuter, Geometry.Parse("M 0,9 10,9 10,11 0,11 0,9 M 10,5 10,15 15,10 10,5") },
			{ Table.JoinType.FullOuter, Geometry.Parse("M 5,5 5,15 0,10 5,5 M 5,9 10,9 10,11 5,11 5,9 M 10,5 10,15 15,10 10,5") },
		};
		static Geometry aggregate = Geometry.Parse("M 5,0 5,10 0,5 10,5");
		static Dictionary<bool, Geometry> sortGeometries = new Dictionary<bool, Geometry>
		{
			{ true, Geometry.Parse("M 5,5 15,5 10,0 5,5") },
			{ false, Geometry.Parse("M 5,0 10,5 15,0 5,0") },
		};

		class AggregateColumn : DataGridTextColumn
		{
			public DisplayColumn DisplayColumn { get; set; }
		}
		void SetupLayout()
		{
			var currentColumnName = "";
			var currentColumnIndex = 0;
			var currentType = Table.AggregateType.None;
			if ((dataGrid.CurrentCell != null) && (dataGrid.CurrentCell.IsValid))
			{
				var column = dataGrid.CurrentCell.Column as AggregateColumn;
				currentColumnName = joinTable.Headers[column.DisplayColumn.InputColumn];
				currentColumnIndex = dataGrid.Columns.IndexOf(column);
				currentType = column.DisplayColumn.AggregateType;
			}

			dataGrid.Columns.Clear();
			for (var index = 0; index < displayColumns.Count; ++index)
			{
				var displayColumn = displayColumns[index];

				var columnHeader = new StackPanel { Orientation = Orientation.Vertical, HorizontalAlignment = HorizontalAlignment.Center };

				var columnName = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };

				if (groupByColumns.Contains(displayColumn.InputColumn))
					columnName.Children.Add(new Path { Fill = Brushes.Black, StrokeThickness = 1, Data = aggregate, VerticalAlignment = VerticalAlignment.Center });

				columnName.Children.Add(new Label { Content = Table.Headers[index].Replace("_", "__") });

				columnHeader.Children.Add(columnName);
				columnHeader.Children.Add(new Label { Content = Table.Types[index].Name, HorizontalAlignment = HorizontalAlignment.Center });

				foreach (var joinInfo in joinInfos.Where(joinInfo => joinInfo.LeftColumn == displayColumn.InputColumn))
					columnName.Children.Add(new Path { Fill = Brushes.Black, Stroke = Brushes.Gray, StrokeThickness = 1, Data = joinGeometries[joinInfo.JoinType], VerticalAlignment = VerticalAlignment.Center });

				if (displayColumn.SortOrder.HasValue)
				{
					var fill = displayColumn.SortOrder == 0 ? Brushes.DarkGray : Brushes.Transparent;
					columnName.Children.Add(new Path { Fill = fill, Stroke = Brushes.Gray, StrokeThickness = 1, Data = sortGeometries[displayColumn.SortAscending], VerticalAlignment = VerticalAlignment.Center });
				}

				var column = new AggregateColumn { Header = columnHeader, Binding = new Binding(String.Format("[{0}]", displayColumns.IndexOf(displayColumn))), DisplayColumn = displayColumn };

				if (Table.Types[index] == typeof(long))
				{
					var rightAlignCellStyle = new Style(typeof(TextBlock));
					rightAlignCellStyle.Setters.Add(new Setter(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Right));
					column.ElementStyle = rightAlignCellStyle;
				}

				dataGrid.Columns.Add(column);
			}

			var selectColumn = displayColumns.FirstOrDefault(column => (joinTable.Headers[column.InputColumn] == currentColumnName) && ((currentType == Table.AggregateType.None) || (column.AggregateType == currentType)));
			if ((selectColumn == null) && (displayColumns.Count != 0))
				selectColumn = displayColumns[Math.Max(0, Math.Min(currentColumnIndex, displayColumns.Count - 1))];

			if ((selectColumn != null) && (dataGrid.Items.Count > 0))
			{
				dataGrid.SelectedCells.Clear();
				dataGrid.CurrentCell = new DataGridCellInfo(dataGrid.Items[0], dataGrid.Columns.Cast<AggregateColumn>().Single(column => column.DisplayColumn == selectColumn));
				dataGrid.SelectedCells.Add(dataGrid.CurrentCell);
			}
		}

		internal void SetFocus()
		{
			var timer = new DispatcherTimer();
			timer.Tick += (s, e) =>
			{
				timer.Stop();

				if ((dataGrid.Items.Count == 0) || (dataGrid.Columns.Count == 0))
					return;

				if (!dataGrid.SelectedCells.Any(selected => (selected != null) && (selected.IsValid)))
					dataGrid.SelectedCells.Add(new DataGridCellInfo(dataGrid.Items.Cast<object>().FirstOrDefault(), dataGrid.Columns.FirstOrDefault()));

				var cell = dataGrid.SelectedCells.Last(selected => (selected != null) && (selected.IsValid));
				dataGrid.Focus();
				dataGrid.CurrentCell = cell;
			};
			timer.Start();
		}

		void ColumnReorderedEvent(object sender, DataGridColumnEventArgs e)
		{
			displayColumns = dataGrid.Columns.Cast<AggregateColumn>().OrderBy(column => column.DisplayIndex).Select(column => column.DisplayColumn).ToList();
			AggregateData();
		}

		Dictionary<Type, List<Table.AggregateType>> DefaultAggregateTypesByType = new Dictionary<Type, List<Table.AggregateType>>
		{
			{  typeof(long), new List<Table.AggregateType> { Table.AggregateType.Sum } },
			{  typeof(double), new List<Table.AggregateType> { Table.AggregateType.Sum } },
			{  typeof(DateTime), new List<Table.AggregateType> { Table.AggregateType.Min, Table.AggregateType.Max } },
			{  typeof(string), new List<Table.AggregateType> { Table.AggregateType.Distinct } },
		};
		void SetGroupOrder(int inputColumn, bool grouped)
		{
			displayColumns = displayColumns.Where(column => column.InputColumn != inputColumn).ToList();
			groupByColumns = groupByColumns.Where(column => column != inputColumn).ToList();

			var add = new List<DisplayColumn>();
			if (grouped)
			{
				groupByColumns.Add(inputColumn);
				add.Add(new DisplayColumn(inputColumn, Table.AggregateType.Distinct));
				if (displayColumns.Count == 0)
					add.Add(new DisplayColumn(inputColumn, Table.AggregateType.Count));
			}
			else
				add.AddRange(DefaultAggregateTypesByType[joinTable.Types[inputColumn]].Select(aggType => new DisplayColumn(inputColumn, aggType)));

			displayColumns.InsertRange(0, add);
			displayColumns = displayColumns.OrderBy(column => !groupByColumns.Contains(column.InputColumn)).ThenBy(column => groupByColumns.IndexOf(column.InputColumn)).ToList();
		}

		void JoinByColumn(int inputColumn)
		{
			if (ShiftDown)
			{
				joinInfos = joinInfos.Where(joinInfo => joinInfo.LeftColumn != inputColumn).ToList();
				JoinData();
				return;
			}

			int joinTable, joinColumn;
			if (!parent.GetJoin(out joinTable, out joinColumn))
			{
				var joinInfo = joinInfos.LastOrDefault(joinData => joinData.LeftColumn == inputColumn);
				if (joinInfo != null)
				{
					var joinTypes = Enum.GetValues(typeof(Table.JoinType)).Cast<Table.JoinType>().ToList();
					var index = joinTypes.IndexOf(joinInfo.JoinType) + 1;
					if (index >= joinTypes.Count)
						index = 0;
					joinInfo.JoinType = joinTypes[index];
					JoinData();
					return;
				}

				parent.SetJoin(this, inputColumn);
				return;
			}

			joinInfos.Add(new JoinInfo { RightTable = joinTable, LeftColumn = inputColumn, RightColumn = joinColumn, JoinType = Table.JoinType.Inner });
			parent.GetTab(joinTable).OutputTableType = Table.TableType.None;
			JoinData();
		}

		void GroupByColumn(int inputColumn)
		{
			var contains = groupByColumns.Contains(inputColumn);
			if (!ShiftDown)
				groupByColumns.ToList().ForEach(column => SetGroupOrder(column, false));

			SetGroupOrder(inputColumn, !contains);

			if (groupByColumns.Any())
				displayColumns = displayColumns.SelectMany(displayColumn => displayColumn.AggregateType == Table.AggregateType.None ? DefaultAggregateTypesByType[joinTable.Types[displayColumn.InputColumn]].Select(aggType => new DisplayColumn(displayColumn.InputColumn, aggType)) : new List<DisplayColumn> { displayColumn }).ToList();
			else
				displayColumns = displayColumns.Select(displayColumn => displayColumn.AggregateType != Table.AggregateType.None ? new DisplayColumn(displayColumn.InputColumn, Table.AggregateType.None) : displayColumn).GroupBy(column => column.InputColumn.ToString() + column.AggregateType).Select(group => group.First()).ToList();

			AggregateData();
		}

		void SortByColumn(DisplayColumn displayColumn)
		{
			if (!ShiftDown)
			{
				if (!displayColumn.SortOrder.HasValue)
					displayColumn.SortAscending = false;
				displayColumns.ForEach(column => column.SortOrder = null);
				displayColumn.SortOrder = 0;
			}

			if (displayColumn.SortOrder == null)
			{
				displayColumn.SortOrder = (displayColumns.Max(column => column.SortOrder) ?? -1) + 1;
				displayColumn.SortAscending = true;
			}
			else
				displayColumn.SortAscending = !displayColumn.SortAscending;

			SortData();
		}

		bool ControlDown { get { return (Keyboard.Modifiers & ModifierKeys.Control) != 0; } }
		bool ShiftDown { get { return (Keyboard.Modifiers & ModifierKeys.Shift) != 0; } }
		void ColumnHeaderClick(object sender, RoutedEventArgs e)
		{
			var column = ((e.OriginalSource as DataGridColumnHeader).Column as AggregateColumn).DisplayColumn;

			if (ControlDown)
				GroupByColumn(column.InputColumn);
			else
				SortByColumn(column);
		}

		void ToggleAggregateType(DisplayColumn displayColumn, Table.AggregateType type)
		{
			if (displayColumns.Any(column => (column.InputColumn == displayColumn.InputColumn) && (column.AggregateType == type)))
				return;

			if (ShiftDown)
				displayColumns.Insert(displayColumns.IndexOf(displayColumn) + 1, new DisplayColumn(displayColumn.InputColumn, type));
			else
				displayColumn.AggregateType = type;
			AggregateData();
		}

		void DataGridKeyDown(object sender, KeyEventArgs e)
		{
			var column = (dataGrid.CurrentColumn as AggregateColumn).DisplayColumn;
			e.Handled = true;
			switch (e.Key)
			{
				case Key.Right:
				case Key.Left:
					if (ControlDown)
					{
						var ofs = e.Key == Key.Right ? 1 : -1;
						var index = displayColumns.IndexOf(column);
						var newIndex = index + ofs;
						if ((newIndex >= 0) && (newIndex < displayColumns.Count))
						{
							var tmp = displayColumns[index];
							displayColumns[index] = displayColumns[newIndex];
							displayColumns[newIndex] = tmp;
							AggregateData();
						}
					}
					else
						e.Handled = false;
					break;
				case Key.J: JoinByColumn(column.InputColumn); break;
				case Key.G: GroupByColumn(column.InputColumn); break;
				case Key.Space: SortByColumn(column); break;
				case Key.D: ToggleAggregateType(column, Table.AggregateType.Distinct); break;
				case Key.O: ToggleAggregateType(column, Table.AggregateType.Concat); break;
				case Key.N: ToggleAggregateType(column, Table.AggregateType.Min); break;
				case Key.X: ToggleAggregateType(column, Table.AggregateType.Max); break;
				case Key.S: ToggleAggregateType(column, Table.AggregateType.Sum); break;
				case Key.A: ToggleAggregateType(column, Table.AggregateType.Average); break;
				case Key.C: ToggleAggregateType(column, Table.AggregateType.Count); break;
				case Key.Delete:
					displayColumns.Remove(column);
					AggregateData();
					break;
				default:
					e.Handled = false;
					break;
			}
		}

		void ResetClick(object sender, RoutedEventArgs e)
		{
			InputTableType = Table.TableType.None; // Will call InputUpdated
		}

		internal Result GetResult()
		{
			return new Result
			{
				InputTableType = InputTableType,
				OutputTableType = OutputTableType,
				InputHasHeaders = InputHasHeaders,
				OutputHasHeaders = OutputHasHeaders,
				JoinInfos = joinInfos,
				GroupByColumns = groupByColumns,
				AggregateColumns = displayColumns.Select(column => Tuple.Create(column.InputColumn, column.AggregateType)).ToList(),
				SortColumns = displayColumns.Where(column => column.SortOrder.HasValue).OrderBy(column => column.SortOrder.Value).Select(column => Tuple.Create(displayColumns.IndexOf(column), column.SortAscending)).ToList(),
			};
		}

	}
}
