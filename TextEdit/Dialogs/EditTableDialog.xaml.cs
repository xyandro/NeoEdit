using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using NeoEdit.Common;
using NeoEdit.GUI.Controls;

namespace NeoEdit.TextEdit.Dialogs
{
	internal partial class EditTableDialog
	{
		internal class Result
		{
			public List<Table.AggregateData> AggregateData { get; set; }
			public List<Table.SortData> SortData { get; set; }
		}

		[DepProp]
		public Table Table { get { return UIHelper<EditTableDialog>.GetPropValue<Table>(this); } set { UIHelper<EditTableDialog>.SetPropValue(this, value); } }
		[DepProp]
		public int SelectedColumn { get { return UIHelper<EditTableDialog>.GetPropValue<int>(this); } set { UIHelper<EditTableDialog>.SetPropValue(this, value); } }

		public List<Table.AggregateData> AggregateData { get; set; }
		public List<Table.SortData> SortData { get; set; }

		static EditTableDialog() { UIHelper<EditTableDialog>.Register(); }

		readonly Table inputTable;
		EditTableDialog(string input, Table.TableType tableType, bool hasHeaders)
		{
			inputTable = new Table(input, tableType, hasHeaders);
			Reset();
			InitializeComponent();
		}

		void Reset()
		{
			AggregateData = new List<Table.AggregateData>();
			for (var column = 0; column < inputTable.NumColumns; ++column)
			{
				var aggregateData = new Table.AggregateData(column);
				var firstType = Enumerable.Range(0, inputTable.NumRows).Select(row => inputTable[row, column]).Where(value => value != null).Select(value => value.GetType()).FirstOrDefault();
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

		void SetupTable() => Table = inputTable.Aggregate(AggregateData).Sort(SortData);

		bool controlDown => (Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.None;
		bool altDown => (Keyboard.Modifiers & ModifierKeys.Alt) != ModifierKeys.None;
		bool shiftDown => (Keyboard.Modifiers & ModifierKeys.Shift) != ModifierKeys.None;

		protected override void OnPreviewKeyDown(KeyEventArgs e)
		{
			base.OnPreviewKeyDown(e);
			e.Handled = true;
			var key = e.Key == Key.System ? e.SystemKey : e.Key;
			switch (key)
			{
				case Key.G: SetAggregation(Table.AggregateType.Group); break;
				case Key.A: SetAggregation(Table.AggregateType.All); break;
				case Key.D: SetAggregation(Table.AggregateType.Distinct); break;
				case Key.C: SetAggregation(Table.AggregateType.Count); break;
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
				case Key.Home: SelectedColumn = 0; break;
				case Key.End: SelectedColumn = Table.NumColumns - 1; break;
				case Key.Up: case Key.Down: break;
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
						--SelectedColumn;
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
						++SelectedColumn;
					break;
				default: e.Handled = false; break;
			}

			if (e.Handled)
			{
				SetupTable();
				SelectedColumn = Math.Max(0, Math.Min(SelectedColumn, Table.NumColumns - 1));
			}
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

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result { AggregateData = AggregateData, SortData = SortData };
			DialogResult = true;
		}

		static public Result Run(Window parent, string input, Table.TableType tableType, bool hasHeaders)
		{
			var dialog = new EditTableDialog(input, tableType, hasHeaders) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
