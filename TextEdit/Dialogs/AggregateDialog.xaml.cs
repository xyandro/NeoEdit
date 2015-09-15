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
	partial class AggregateDialog
	{
		class ColumnInfo
		{
			public string Name { get; set; }
			public Type Type { get; set; }
			public int? GroupOrder { get; set; }

			public ColumnInfo(string name, Type type)
			{
				Name = name;
				Type = type;
			}

			public override string ToString() { return Name; }
		}

		class DisplayColumn
		{
			public ColumnInfo ColumnInfo { get; set; }
			public TextEditor.AggregateType AggregateType { get; set; }
			public int? SortOrder { get; set; }
			public bool SortAscending { get; set; }

			public DisplayColumn(ColumnInfo columnInfo, TextEditor.AggregateType aggregateType)
			{
				ColumnInfo = columnInfo;
				AggregateType = aggregateType;
			}

			public override string ToString() { return String.Format("{0}: {1}", ColumnInfo.Name, AggregateType); }
		}

		internal class Result
		{
			public TextEditor.TableType TableType { get; set; }
			public bool HasHeaders { get; set; }
			public List<Type> Types { get; set; }
			public List<int> GroupByColumns { get; set; }
			public List<Tuple<int, TextEditor.AggregateType>> AggregateColumns { get; set; }
			public List<Tuple<int, bool>> SortColumns { get; set; }
			public List<string> ColumnHeaders { get; set; }
		}

		[DepProp]
		public TextEditor.TableType TableType { get { return UIHelper<AggregateDialog>.GetPropValue<TextEditor.TableType>(this); } set { UIHelper<AggregateDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool HasHeaders { get { return UIHelper<AggregateDialog>.GetPropValue<bool>(this); } set { UIHelper<AggregateDialog>.SetPropValue(this, value); } }
		[DepProp]
		public List<List<object>> Data { get { return UIHelper<AggregateDialog>.GetPropValue<List<List<object>>>(this); } set { UIHelper<AggregateDialog>.SetPropValue(this, value); } }

		static AggregateDialog()
		{
			UIHelper<AggregateDialog>.Register();
			UIHelper<AggregateDialog>.AddCallback(a => a.TableType, (obj, o, n) => obj.InputUpdated(true));
			UIHelper<AggregateDialog>.AddCallback(a => a.HasHeaders, (obj, o, n) => obj.InputUpdated(false));
		}

		readonly string table;
		List<List<object>> rawData;
		List<ColumnInfo> columnInfos = new List<ColumnInfo>();
		List<DisplayColumn> displayColumns = new List<DisplayColumn>();

		AggregateDialog(string table)
		{
			this.table = table;
			InitializeComponent();

			TableType = TextEditor.DetectTableType(table);
			if (TableType == TextEditor.TableType.None)
				TableType = TextEditor.TableType.TSV;
		}

		delegate bool TryParseDelegate<T>(string str, out T result);
		bool ColumnIsType<T>(List<List<string>> data, int column, TryParseDelegate<T> parse)
		{
			foreach (var items in data)
			{
				T obj;
				if (!parse(items[column], out obj))
					return false;
			}
			return true;
		}

		Type GetColumnType(List<List<string>> data, int column)
		{
			if (ColumnIsType<long>(data, column, long.TryParse))
				return typeof(long);
			if (ColumnIsType<double>(data, column, double.TryParse))
				return typeof(double);
			if (ColumnIsType<DateTime>(data, column, DateTime.TryParse))
				return typeof(DateTime);
			return typeof(string);
		}

		bool inInputUpdated = false;
		void InputUpdated(bool calcHasHeaders)
		{
			if (inInputUpdated)
				return;

			Data = new List<List<object>>();
			columnInfos = new List<ColumnInfo>();

			var input = TextEditor.GetTableStrings(table, TableType, 10000);
			if (!input.Any())
				return;

			var numColumns = input.First().Count;

			if (calcHasHeaders)
			{
				var headers = Enumerable.Take(input, 1).ToList();
				var rows = Enumerable.Skip(input, 1).ToList();
				var headerTypes = Enumerable.Range(0, numColumns).Select(column => GetColumnType(headers, column)).ToList();
				var dataTypes = Enumerable.Range(0, numColumns).Select(column => GetColumnType(rows, column)).ToList();
				inInputUpdated = true;
				HasHeaders = (headerTypes.All(type => type == typeof(string))) && (Enumerable.Range(0, numColumns).Any(column => headerTypes[column] != dataTypes[column]));
				inInputUpdated = false;
			}

			var columnNames = Enumerable.Range(0, numColumns).Select(index => String.Format("Column {0}", index + 1)).ToList();
			if (HasHeaders)
			{
				columnNames = input[0].Select(obj => (obj ?? "").ToString()).ToList();
				input.RemoveAt(0);
			}

			var columnTypes = Enumerable.Range(0, numColumns).Select(column => GetColumnType(input, column)).ToList();

			columnInfos = columnNames.Select((column, index) => new ColumnInfo(column, columnTypes[index])).ToList();
			rawData = TextEditor.GetTableObjects(input, columnInfos.Select(column => column.Type).ToList());
			displayColumns = columnInfos.Select(col => new DisplayColumn(col, TextEditor.AggregateType.None)).ToList();

			AggregateData();
		}

		void AggregateData()
		{
			var groupByIndexes = columnInfos.Where(column => column.GroupOrder.HasValue).OrderBy(column => column.GroupOrder.Value).Select(column => columnInfos.IndexOf(column)).ToList();
			var aggregateTypes = displayColumns.Select(column => Tuple.Create(columnInfos.IndexOf(column.ColumnInfo), column.AggregateType)).ToList();
			Data = TextEditor.AggregateByColumn(rawData, groupByIndexes, aggregateTypes);
			SortData();
		}

		void SortData()
		{
			var sortOrder = displayColumns.Where(column => column.SortOrder.HasValue).OrderBy(column => column.SortOrder.Value).Select(column => Tuple.Create(displayColumns.IndexOf(column), column.SortAscending)).ToList();
			Data = TextEditor.SortAggregateData(Data, sortOrder);
			SetupLayout();
		}

		static Geometry _arrowUp = Geometry.Parse("M 5,5 15,5 10,0 5,5");
		static Geometry _arrowDown = Geometry.Parse("M 5,0 10,5 15,0 5,0");
		static Geometry _aggregate = Geometry.Parse("M 5,0 5,10 0,5 10,5");

		class AggregateColumn : DataGridTextColumn
		{
			public DisplayColumn DisplayColumn { get; set; }
		}
		void SetupLayout()
		{
			var currentColumnName = "";
			var currentColumnIndex = 0;
			var currentType = TextEditor.AggregateType.None;
			if ((dataGrid.CurrentCell != null) && (dataGrid.CurrentCell.IsValid))
			{
				var column = dataGrid.CurrentCell.Column as AggregateColumn;
				currentColumnName = column.DisplayColumn.ColumnInfo.Name;
				currentColumnIndex = dataGrid.Columns.IndexOf(column);
				currentType = column.DisplayColumn.AggregateType;
			}

			dataGrid.Columns.Clear();
			var grouped = columnInfos.Any(column => column.GroupOrder.HasValue);
			foreach (var displayColumn in displayColumns)
			{
				var columnHeader = new StackPanel { Orientation = Orientation.Vertical, HorizontalAlignment = HorizontalAlignment.Center };

				var columnName = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };

				if (displayColumn.ColumnInfo.GroupOrder.HasValue)
					columnName.Children.Add(new Path { Fill = Brushes.Black, StrokeThickness = 1, Data = _aggregate, VerticalAlignment = VerticalAlignment.Center });

				columnName.Children.Add(new Label { Content = displayColumn.ColumnInfo.Name + (grouped ? " (" + displayColumn.AggregateType + ")" : "") });

				columnHeader.Children.Add(columnName);
				columnHeader.Children.Add(new Label { Content = displayColumn.ColumnInfo.Type.Name, HorizontalAlignment = HorizontalAlignment.Center });

				if (displayColumn.SortOrder.HasValue)
				{
					var fill = displayColumn.SortOrder == 0 ? Brushes.DarkGray : Brushes.Transparent;
					var geometry = displayColumn.SortAscending ? _arrowUp : _arrowDown;
					columnName.Children.Add(new Path { Fill = fill, Stroke = Brushes.Gray, StrokeThickness = 1, Data = geometry, VerticalAlignment = VerticalAlignment.Center });
				}

				var column = new AggregateColumn { Header = columnHeader, Binding = new Binding(String.Format("[{0}]", displayColumns.IndexOf(displayColumn))), DisplayColumn = displayColumn };

				if (displayColumn.ColumnInfo.Type == typeof(long))
				{
					var rightAlignCellStyle = new Style(typeof(TextBlock));
					rightAlignCellStyle.Setters.Add(new Setter(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Right));
					column.ElementStyle = rightAlignCellStyle;
				}

				dataGrid.Columns.Add(column);
			}

			var timer = new DispatcherTimer();
			timer.Tick += (s, e) =>
			{
				timer.Stop();

				var selectColumn = displayColumns.FirstOrDefault(column => (column.ColumnInfo.Name == currentColumnName) && ((currentType == TextEditor.AggregateType.None) || (column.AggregateType == currentType)));
				if ((selectColumn == null) && (displayColumns.Count != 0))
					selectColumn = displayColumns[Math.Max(0, Math.Min(currentColumnIndex, displayColumns.Count - 1))];

				if ((selectColumn != null) && (dataGrid.Items.Count > 0))
				{
					dataGrid.SelectedCells.Clear();
					dataGrid.CurrentCell = new DataGridCellInfo(dataGrid.Items[0], dataGrid.Columns.Cast<AggregateColumn>().Single(col => col.DisplayColumn == selectColumn));
					dataGrid.SelectedCells.Add(dataGrid.CurrentCell);
					(dataGrid.CurrentCell.Column.GetCellContent(dataGrid.CurrentCell.Item).Parent as IInputElement).Focus();
				}
			};
			timer.Start();
		}

		void ColumnReorderedEvent(object sender, DataGridColumnEventArgs e)
		{
			displayColumns = dataGrid.Columns.Cast<AggregateColumn>().OrderBy(col => col.DisplayIndex).Select(col => col.DisplayColumn).ToList();
			AggregateData();
		}

		Dictionary<Type, List<TextEditor.AggregateType>> DefaultAggregateTypesByType = new Dictionary<Type, List<TextEditor.AggregateType>>
		{
			{  typeof(long), new List<TextEditor.AggregateType> { TextEditor.AggregateType.Sum } },
			{  typeof(double), new List<TextEditor.AggregateType> { TextEditor.AggregateType.Sum } },
			{  typeof(DateTime), new List<TextEditor.AggregateType> { TextEditor.AggregateType.Min, TextEditor.AggregateType.Max } },
			{  typeof(string), new List<TextEditor.AggregateType> { TextEditor.AggregateType.Distinct } },
		};
		void SetGroupOrder(ColumnInfo columnInfo, bool grouped)
		{
			var cols = displayColumns.Where(col => col.ColumnInfo == columnInfo).ToList();
			var index = Math.Max(0, displayColumns.IndexOf(cols.FirstOrDefault()));
			cols.ForEach(col => displayColumns.Remove(col));

			var add = new List<DisplayColumn>();
			if (grouped)
			{
				columnInfo.GroupOrder = (columnInfos.Max(col => col.GroupOrder) ?? -1) + 1;
				add.Add(new DisplayColumn(columnInfo, TextEditor.AggregateType.Distinct));
				if (displayColumns.Count == 0)
					add.Add(new DisplayColumn(columnInfo, TextEditor.AggregateType.Count));
			}
			else
			{
				columnInfo.GroupOrder = null;
				add.AddRange(DefaultAggregateTypesByType[columnInfo.Type].Select(aggType => new DisplayColumn(columnInfo, aggType)));
			}
			displayColumns.InsertRange(index, add);
			displayColumns = displayColumns.OrderBy(col => col.ColumnInfo.GroupOrder ?? int.MaxValue).ToList();
		}

		void GroupByColumn(ColumnInfo column)
		{
			var contains = column.GroupOrder.HasValue;
			if (!ShiftDown)
				columnInfos.Where(col => col.GroupOrder.HasValue).ToList().ForEach(col => SetGroupOrder(col, false));

			SetGroupOrder(column, !contains);

			var grouped = columnInfos.Any(col => col.GroupOrder.HasValue);

			if (grouped)
				displayColumns = displayColumns.SelectMany(displayColumn => displayColumn.AggregateType == TextEditor.AggregateType.None ? DefaultAggregateTypesByType[displayColumn.ColumnInfo.Type].Select(aggType => new DisplayColumn(displayColumn.ColumnInfo, aggType)) : new List<DisplayColumn> { displayColumn }).ToList();
			else
				displayColumns = displayColumns.Select(displayColumn => displayColumn.AggregateType != TextEditor.AggregateType.None ? new DisplayColumn(displayColumn.ColumnInfo, TextEditor.AggregateType.None) : displayColumn).GroupBy(col => col.ColumnInfo.Name + col.AggregateType).Select(group => group.First()).ToList();

			AggregateData();
		}

		void SortByColumn(DisplayColumn column)
		{
			if (!ShiftDown)
			{
				if (!column.SortOrder.HasValue)
					column.SortAscending = false;
				displayColumns.ForEach(col => col.SortOrder = null);
				column.SortOrder = 0;
			}

			if (column.SortOrder == null)
			{
				column.SortOrder = (displayColumns.Max(col => col.SortOrder) ?? -1) + 1;
				column.SortAscending = true;
			}
			else
				column.SortAscending = !column.SortAscending;

			SortData();
		}

		bool ControlDown { get { return (Keyboard.Modifiers & ModifierKeys.Control) != 0; } }
		bool ShiftDown { get { return (Keyboard.Modifiers & ModifierKeys.Shift) != 0; } }
		void ColumnHeaderClick(object sender, RoutedEventArgs e)
		{
			var column = ((e.OriginalSource as DataGridColumnHeader).Column as AggregateColumn).DisplayColumn;

			if (ControlDown)
				GroupByColumn(column.ColumnInfo);
			else
				SortByColumn(column);
		}

		void ToggleAggregateType(DisplayColumn displayColumn, TextEditor.AggregateType type)
		{
			if (displayColumns.Any(col => (col.ColumnInfo == displayColumn.ColumnInfo) && (col.AggregateType == type)))
				return;

			if (ShiftDown)
				displayColumns.Insert(displayColumns.IndexOf(displayColumn) + 1, new DisplayColumn(displayColumn.ColumnInfo, type));
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
				case Key.G: GroupByColumn(column.ColumnInfo); break;
				case Key.Space: SortByColumn(column); break;
				case Key.Enter: OkClick(null, null); break;
				case Key.D: ToggleAggregateType(column, TextEditor.AggregateType.Distinct); break;
				case Key.O: ToggleAggregateType(column, TextEditor.AggregateType.Concat); break;
				case Key.N: ToggleAggregateType(column, TextEditor.AggregateType.Min); break;
				case Key.X: ToggleAggregateType(column, TextEditor.AggregateType.Max); break;
				case Key.S: ToggleAggregateType(column, TextEditor.AggregateType.Sum); break;
				case Key.A: ToggleAggregateType(column, TextEditor.AggregateType.Average); break;
				case Key.C: ToggleAggregateType(column, TextEditor.AggregateType.Count); break;
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
			InputUpdated(true);
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			var multiple = displayColumns.GroupBy(column => column.ColumnInfo).ToDictionary(group => group.Key, group => group.Count() > 1);
			result = new Result
			{
				TableType = TableType,
				HasHeaders = HasHeaders,
				Types = columnInfos.Select(col => col.Type).ToList(),
				GroupByColumns = columnInfos.Where(column => column.GroupOrder.HasValue).OrderBy(column => column.GroupOrder.Value).Select(column => columnInfos.IndexOf(column)).ToList(),
				AggregateColumns = displayColumns.Select(column => Tuple.Create(columnInfos.IndexOf(column.ColumnInfo), column.AggregateType)).ToList(),
				SortColumns = displayColumns.Where(column => column.SortOrder.HasValue).OrderBy(column => column.SortOrder.Value).Select(column => Tuple.Create(displayColumns.IndexOf(column), column.SortAscending)).ToList(),
				ColumnHeaders = displayColumns.Select(column => column.ColumnInfo.Name + (multiple[column.ColumnInfo] ? " (" + column.AggregateType + ")" : "")).ToList(),
			};
			DialogResult = true;
		}

		public static Result Run(Window parent, string table)
		{
			var dialog = new AggregateDialog(table) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
