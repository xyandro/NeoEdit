﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using NeoEdit.Common;
using NeoEdit.Common.Tables;
using NeoEdit.GUI.Controls;

namespace NeoEdit.TableEdit.Dialogs
{
	internal partial class GroupDialog
	{
		internal class Result
		{
			public List<Table.AggregateData> AggregateData;
		}

		class ColumnData : DependencyObject
		{
			[DepProp]
			public string ColumnName { get { return UIHelper<ColumnData>.GetPropValue<string>(this); } set { UIHelper<ColumnData>.SetPropValue(this, value); } }
			[DepProp]
			public Table.AggregateType Aggregation { get { return UIHelper<ColumnData>.GetPropValue<Table.AggregateType>(this); } set { UIHelper<ColumnData>.SetPropValue(this, value); } }
			[DepProp]
			public bool Selected { get { return UIHelper<ColumnData>.GetPropValue<bool>(this); } set { UIHelper<ColumnData>.SetPropValue(this, value); } }

			static ColumnData() { UIHelper<ColumnData>.Register(); }

			public ColumnData(string header, Table table, int column, bool group)
			{
				ColumnName = header;
				var type = Enumerable.Range(0, table.NumRows).Select(row => table[row, column]).Where(value => value != null).Select(value => value.GetType()).FirstOrDefault();
				Aggregation = group ? Table.AggregateType.Group : type.IsNumericType() ? Table.AggregateType.Sum : type.IsDateType() ? Table.AggregateType.Min | Table.AggregateType.Max : Table.AggregateType.Distinct;
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
			Columns = table.Headers.Select((header, column) => new ColumnData(header, table, column, groupColumns.Contains(column))).ToObservableCollectionEx();
		}

		void ColumnsKeyDown(object sender, KeyEventArgs e)
		{
			e.Handled = true;
			switch (e.Key)
			{
				case Key.E: ToggleAggregateType(Table.AggregateType.None); break;
				case Key.G: ToggleAggregateType(Table.AggregateType.Group); break;
				case Key.A: ToggleAggregateType(Table.AggregateType.All); break;
				case Key.D: ToggleAggregateType(Table.AggregateType.Distinct); break;
				case Key.N: ToggleAggregateType(Table.AggregateType.Min); break;
				case Key.X: ToggleAggregateType(Table.AggregateType.Max); break;
				case Key.S: ToggleAggregateType(Table.AggregateType.Sum); break;
				case Key.V: ToggleAggregateType(Table.AggregateType.Average); break;
				case Key.C: ToggleAggregateType(Table.AggregateType.Count); break;
				default: e.Handled = false; break;
			}
		}

		bool shiftDown => Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);

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
				AggregateData = Columns.SelectMany((column, index) => aggregateTypes.Where(aggregateType => column.Aggregation.HasFlag(aggregateType)).Select(aggregateType => new Table.AggregateData(index, aggregateType))).OrderBy(tuple => !tuple.Aggregation.HasFlag(Table.AggregateType.Group)).ToList(),
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