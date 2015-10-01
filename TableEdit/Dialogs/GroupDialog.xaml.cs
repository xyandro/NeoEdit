using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using NeoEdit.Common;
using NeoEdit.GUI.Controls;

namespace NeoEdit.TableEdit.Dialogs
{
	internal partial class GroupDialog
	{
		internal class Result
		{
			public List<int> AggregateColumns;
			public List<Tuple<int, Table.AggregateType>> AggregateData;
		}

		class ColumnData : DependencyObject
		{
			[DepProp]
			public string ColumnName { get { return UIHelper<ColumnData>.GetPropValue<string>(this); } set { UIHelper<ColumnData>.SetPropValue(this, value); } }
			[DepProp]
			public bool Group { get { return UIHelper<ColumnData>.GetPropValue<bool>(this); } set { UIHelper<ColumnData>.SetPropValue(this, value); } }
			[DepProp]
			public Table.AggregateType Aggregation { get { return UIHelper<ColumnData>.GetPropValue<Table.AggregateType>(this); } set { UIHelper<ColumnData>.SetPropValue(this, value); } }
			[DepProp]
			public bool Selected { get { return UIHelper<ColumnData>.GetPropValue<bool>(this); } set { UIHelper<ColumnData>.SetPropValue(this, value); } }

			readonly Type type;

			static ColumnData()
			{
				UIHelper<ColumnData>.Register();
				UIHelper<ColumnData>.AddCallback(a => a.Group, (obj, o, n) => obj.SetAggregateType());
			}

			public ColumnData(Table.Header header, bool group)
			{
				ColumnName = header.Name;
				type = header.Type;
				Group = group;
				SetAggregateType();
			}

			void SetAggregateType()
			{
				Aggregation = Group ? Table.AggregateType.Value : type.IsNumericType() ? Table.AggregateType.Sum : type.IsDateType() ? Table.AggregateType.Min | Table.AggregateType.Max : Table.AggregateType.Distinct;
			}
		}

		[DepProp]
		ObservableCollectionEx<ColumnData> Columns { get { return UIHelper<GroupDialog>.GetPropValue<ObservableCollectionEx<ColumnData>>(this); } set { UIHelper<GroupDialog>.SetPropValue(this, value); } }

		static GroupDialog() { UIHelper<GroupDialog>.Register(); }

		readonly Table table;
		GroupDialog(Table table, List<int> groupColumns)
		{
			this.table = table;
			InitializeComponent();
			Columns = table.Headers.Select((header, index) => new ColumnData(header, groupColumns.Contains(index))).ToObservableCollectionEx();
		}

		void ColumnsKeyDown(object sender, KeyEventArgs e)
		{
			e.Handled = true;
			switch (e.Key)
			{
				case Key.G:
					var selected = Columns.Where(column => column.Selected).ToList();
					var all = !selected.All(column => column.Group);
					selected.ForEach(column => column.Group = all);
					break;
				case Key.E: ToggleAggregateType(Table.AggregateType.None); break;
				case Key.V: ToggleAggregateType(Table.AggregateType.Value); break;
				case Key.D: ToggleAggregateType(Table.AggregateType.Distinct); break;
				case Key.O: ToggleAggregateType(Table.AggregateType.Concat); break;
				case Key.N: ToggleAggregateType(Table.AggregateType.Min); break;
				case Key.X: ToggleAggregateType(Table.AggregateType.Max); break;
				case Key.S: ToggleAggregateType(Table.AggregateType.Sum); break;
				case Key.A: ToggleAggregateType(Table.AggregateType.Average); break;
				case Key.C: ToggleAggregateType(Table.AggregateType.Count); break;
				default: e.Handled = false; break;
			}
		}

		bool shiftDown { get { return Keyboard.Modifiers.HasFlag(ModifierKeys.Shift); } }

		void ToggleAggregateType(Table.AggregateType type)
		{
			var selected = Columns.Where(column => column.Selected).ToList();
			if (!shiftDown)
				selected.ForEach(column => column.Aggregation = Table.AggregateType.None);
			var all = !selected.All(column => column.Aggregation.HasFlag(type));
			if (all)
				selected.ForEach(column => column.Aggregation |= type);
			else
				selected.ForEach(column => column.Aggregation &= ~type);
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			var aggregateTypes = Enum.GetValues(typeof(Table.AggregateType)).Cast<Table.AggregateType>().Where(value => value != Table.AggregateType.None).ToList();
			result = new Result
			{
				AggregateColumns = Columns.Indexes(column => column.Group).ToList(),
				AggregateData = Columns.SelectMany((column, index) => aggregateTypes.Where(aggregateType => column.Aggregation.HasFlag(aggregateType)).Select(aggregateType => Tuple.Create(index, aggregateType))).OrderBy(tuple => !Columns[tuple.Item1].Group).ToList(),
			};
			DialogResult = true;
		}

		static public Result Run(Window parent, Table table, List<int> groupColumns)
		{
			var dialog = new GroupDialog(table, groupColumns) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
