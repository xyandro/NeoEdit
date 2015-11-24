using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using NeoEdit.Common;
using NeoEdit.Common.Tables;
using NeoEdit.GUI.Controls;

namespace NeoEdit.TextEdit.Dialogs
{
	internal partial class AggregateTableDialog
	{
		internal class Result
		{
			public Table.TableTypeEnum InputType { get; set; }
			public bool InputHeaders { get; set; }
			public Table.TableTypeEnum OutputType { get; set; }
			public bool OutputHeaders { get; set; }
			public List<Table.AggregateData> AggregateData { get; set; }
			public List<Table.SortData> SortData { get; set; }
		}

		[DepProp]
		public Table.TableTypeEnum InputType { get { return UIHelper<AggregateTableDialog>.GetPropValue<Table.TableTypeEnum>(this); } set { UIHelper<AggregateTableDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool InputHeaders { get { return UIHelper<AggregateTableDialog>.GetPropValue<bool>(this); } set { UIHelper<AggregateTableDialog>.SetPropValue(this, value); } }
		[DepProp]
		public Table.TableTypeEnum OutputType { get { return UIHelper<AggregateTableDialog>.GetPropValue<Table.TableTypeEnum>(this); } set { UIHelper<AggregateTableDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool OutputHeaders { get { return UIHelper<AggregateTableDialog>.GetPropValue<bool>(this); } set { UIHelper<AggregateTableDialog>.SetPropValue(this, value); } }
		[DepProp]
		public Table Table { get { return UIHelper<AggregateTableDialog>.GetPropValue<Table>(this); } set { UIHelper<AggregateTableDialog>.SetPropValue(this, value); } }
		[DepProp]
		public int SelectedColumn { get { return UIHelper<AggregateTableDialog>.GetPropValue<int>(this); } set { UIHelper<AggregateTableDialog>.SetPropValue(this, value); } }

		public List<Table.TableTypeEnum> DataTypes { get; }
		public List<Table.AggregateData> AggregateData { get; set; }
		public List<Table.SortData> SortData { get; set; }

		static AggregateTableDialog()
		{
			UIHelper<AggregateTableDialog>.Register();
			UIHelper<AggregateTableDialog>.AddCallback(a => a.InputType, (obj, o, n) => { obj.OutputType = obj.InputType; obj.InitialSetup(); });
			UIHelper<AggregateTableDialog>.AddCallback(a => a.InputHeaders, (obj, o, n) => { obj.OutputHeaders = obj.InputHeaders; obj.InitialSetup(); });
		}

		readonly string example;
		AggregateTableDialog(string example)
		{
			this.example = example;
			DataTypes = Enum.GetValues(typeof(Table.TableTypeEnum)).Cast<Table.TableTypeEnum>().Where(a => a != Table.TableTypeEnum.None).ToList();
			InitializeComponent();

			InputType = OutputType = Table.GuessTableType(example);
			InputHeaders = OutputHeaders = true;
		}

		Table exampleTable;
		void InitialSetup()
		{
			exampleTable = new Table(example, InputType, InputHeaders);
			AggregateData = new List<Table.AggregateData>();
			for (var column = 0; column < exampleTable.NumColumns; ++column)
			{
				var aggregateData = new Table.AggregateData(column);
				var firstType = Enumerable.Range(0, exampleTable.NumRows).Select(row => exampleTable[row, column]).Where(value => value != null).Select(value => value.GetType()).FirstOrDefault();
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

		void SetupTable()
		{
			Table = exampleTable.Aggregate(AggregateData, false).Sort(SortData);
		}

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
			InitialSetup();
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result { InputType = InputType, InputHeaders = InputHeaders, OutputType = OutputType, OutputHeaders = OutputHeaders, AggregateData = AggregateData, SortData = SortData };
			DialogResult = true;
		}

		static public Result Run(Window parent, string example)
		{
			var dialog = new AggregateTableDialog(example) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
