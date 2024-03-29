﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using NeoEdit.Common;
using NeoEdit.Common.Configuration;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Table_Edit_Dialog
	{
		[DepProp]
		public Table Table { get { return UIHelper<Table_Edit_Dialog>.GetPropValue<Table>(this); } set { UIHelper<Table_Edit_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public int SelectedColumn { get { return UIHelper<Table_Edit_Dialog>.GetPropValue<int>(this); } set { UIHelper<Table_Edit_Dialog>.SetPropValue(this, value); } }

		public List<Table.AggregateData> AggregateData { get; set; }
		public List<Table.SortData> SortData { get; set; }

		static Table_Edit_Dialog() { UIHelper<Table_Edit_Dialog>.Register(); }

		readonly Table input;
		Table_Edit_Dialog(Table input)
		{
			this.input = input;
			Reset();
			InitializeComponent();
		}

		void Reset()
		{
			AggregateData = new List<Table.AggregateData>();
			for (var column = 0; column < input.NumColumns; ++column)
			{
				var aggregateData = new Table.AggregateData(column);
				var firstType = Enumerable.Range(0, input.NumRows).Select(row => input[row, column]).Where(value => value != null).Select(value => value.GetType()).FirstOrDefault();
				if (firstType == null)
					aggregateData.Aggregation = Table.AggregateType.All;
				else if (firstType.IsNumericType())
					aggregateData.Aggregation = Table.AggregateType.Sum;
				else if (firstType.IsDateType())
					aggregateData.Aggregation = Table.AggregateType.Min | Table.AggregateType.Max;
				else
					aggregateData.Aggregation = Table.AggregateType.All;
				AggregateData.Add(aggregateData);
			}
			SortData = new List<Table.SortData>();

			SetupTable();
		}

		void SetupTable() => Table = input.Aggregate(AggregateData).Sort(SortData);

		bool controlDown => Keyboard.Modifiers.HasFlag(ModifierKeys.Control);
		bool altDown => Keyboard.Modifiers.HasFlag(ModifierKeys.Alt);
		bool shiftDown => Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);

		void TablePreviewKeyDown(object sender, KeyEventArgs e)
		{
			e.Handled = true;
			var key = e.Key == Key.System ? e.SystemKey : e.Key;
			switch (key)
			{
				case Key.G: SetAggregation(Table.AggregateType.Group); break;
				case Key.A: SetAggregation(Table.AggregateType.All); break;
				case Key.D: SetAggregation(Table.AggregateType.Distinct); break;
				case Key.C: SetAggregation(Table.AggregateType.Count); break;
				case Key.O: SetAggregation(Table.AggregateType.CountNonNull); break;
				case Key.S: SetAggregation(Table.AggregateType.Sum); break;
				case Key.V: SetAggregation(Table.AggregateType.Average); break;
				case Key.N: SetAggregation(Table.AggregateType.Min); break;
				case Key.X: SetAggregation(Table.AggregateType.Max); break;
				case Key.Delete:
					if (Table.NumColumns != 0)
						AggregateData.RemoveAt(SelectedColumn);
					break;
				case Key.Space:
					{
						var current = SortData.FirstOrDefault(data => data.Column == SelectedColumn);
						if (!shiftDown)
							SortData = SortData.Where(data => data == current).ToList();
						if (current == null)
							SortData.Add(new Table.SortData(SelectedColumn));
						else
							current.Ascending = !current.Ascending;
					}
					break;
				case Key.Left:
					if ((controlDown) || (altDown))
					{
						if (SelectedColumn != 0)
						{
							AggregateData.Insert(SelectedColumn - 1, AggregateData[SelectedColumn]);
							AggregateData.RemoveAt(SelectedColumn + 1);
							--SelectedColumn;
						}
					}
					else
						e.Handled = false;
					break;
				case Key.Right:
					if ((controlDown) || (altDown))
					{
						if (SelectedColumn != Table.NumColumns - 1)
						{
							AggregateData.Insert(SelectedColumn + 2, AggregateData[SelectedColumn]);
							AggregateData.RemoveAt(SelectedColumn);
							++SelectedColumn;
						}
					}
					else
						e.Handled = false;
					break;
				default: e.Handled = false; break;
			}

			if (e.Handled)
				SetupTable();
		}

		void SetAggregation(Table.AggregateType type)
		{
			if (shiftDown)
			{
				AggregateData.Insert(SelectedColumn + 1, new Table.AggregateData(AggregateData[SelectedColumn].Column, type));
				++SelectedColumn;
			}
			else
				AggregateData[SelectedColumn].Aggregation = type;
		}

		void ResetClick(object sender, RoutedEventArgs e)
		{
			Reset();
		}

		Configuration_Table_Edit result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Configuration_Table_Edit { AggregateData = AggregateData, SortData = SortData };
			DialogResult = true;
		}

		public static Configuration_Table_Edit Run(Window parent, Table input)
		{
			var dialog = new Table_Edit_Dialog(input) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
