using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using NeoEdit.GUI.Controls;
using NeoEdit.TextEdit.Dialogs;

namespace NeoEdit.TextEdit
{
	public partial class TextEditor
	{
		internal enum TableType { None, TSV, CSV, Columns }
		[Flags]
		public enum AggregateType
		{
			None = 0,
			Distinct = 1,
			Concat = 2,
			Min = 4,
			Max = 8,
			Sum = 16,
			Average = 32,
			Count = 64,
		}

		void SetTableSelection()
		{
			if (Selections.Count > 1)
				throw new Exception("Must have one selection.");

			if ((Selections.Count == 0) || (!Selections[0].HasSelection))
				Selections.Replace(new Range(BeginOffset(), EndOffset()));
		}

		internal static List<List<string>> GetTableStrings(string table, TableType tableType, int count = int.MaxValue)
		{
			List<List<string>> data;
			switch (tableType)
			{
				case TableType.TSV: data = table.SplitTCSV('\t').Take(count).ToList(); break;
				case TableType.CSV: data = table.SplitTCSV(',').Take(count).ToList(); break;
				case TableType.Columns: data = table.SplitByLine().Select(line => line.Split('│').Select(item => item.TrimEnd()).ToList()).ToList(); break;
				default: throw new ArgumentException("Invalid input type");
			}

			var numColumns = data.Max(row => row.Count);
			data = data.Select(row => row.Expand(numColumns, "").ToList()).ToList();
			return data;
		}

		internal static List<List<object>> GetTableObjects(List<List<string>> tableStrings, List<Type> types)
		{
			var parsers = new Dictionary<Type, Func<string, object>>
			{
				{ typeof(long), str => long.Parse(str) },
				{ typeof(double), str => double.Parse(str) },
				{ typeof(DateTime), str => DateTime.Parse(str) },
				{ typeof(string), str => str },
			};
			return tableStrings.Select(items => items.Select((item, index) => parsers[types[index]](item)).ToList()).ToList();
		}

		internal static List<List<object>> AggregateByColumn(List<List<object>> data, List<int> groupByColumn, List<Tuple<int, AggregateType>> aggregateTypes)
		{
			List<List<List<object>>> groupedData;
			if (groupByColumn.Any())
			{
				var groupMap = new Dictionary<string, List<List<object>>> { { "", data } };
				foreach (var column in groupByColumn)
					groupMap = groupMap.SelectMany(pair => pair.Value.GroupBy(items => pair.Key + "," + (items[column] ?? "").ToString())).ToDictionary(group => group.Key, group => group.ToList());
				groupedData = groupMap.Values.ToList();
			}
			else
				groupedData = data.Select(item => new List<List<object>> { item }).ToList();

			data = groupedData.Select(item => new List<object>()).ToList();

			foreach (var tuple in aggregateTypes)
				for (var ctr = 0; ctr < groupedData.Count; ++ctr)
					data[ctr].Add(GetAggregateValue(tuple.Item2, groupedData[ctr].Select(item => item[tuple.Item1]).ToList()));

			return data;
		}

		static object GetAggregateValue(AggregateType aggType, List<object> values)
		{
			switch (aggType)
			{
				case AggregateType.None: return values.Distinct().Single();
				case AggregateType.Distinct: return String.Join(", ", values.Distinct().OrderBy(a => a));
				case AggregateType.Concat: return String.Join(", ", values.OrderBy(a => a));
				case AggregateType.Min: return values.Min();
				case AggregateType.Max: return values.Max();
				case AggregateType.Sum: return values.Select(value => Convert.ToDouble(value)).Sum();
				case AggregateType.Count: return values.Count;
				case AggregateType.Average: return values.Select(value => Convert.ToDouble(value)).Sum() / values.Count;
				default: throw new Exception("Invalid table type");
			}
		}

		internal static List<List<object>> SortAggregateData(List<List<object>> data, List<Tuple<int, bool>> sortDescs)
		{
			if (!sortDescs.Any())
				return data;

			var sortedList = data.OrderBy(val => default(List<object>));
			foreach (var sortDesc in sortDescs)
			{
				if (sortDesc.Item2)
					sortedList = sortedList.ThenBy(items => items[sortDesc.Item1]);
				else
					sortedList = sortedList.ThenByDescending(items => items[sortDesc.Item1]);
			}
			return sortedList.ToList();
		}

		string ToTCSV(string str, char split)
		{
			if (str.IndexOfAny(new char[] { split, '"' }) != -1)
				return String.Format("\"{0}\"", str.Replace("\"", "\"\""));
			return str;
		}

		internal static TableType DetectTableType(string table)
		{
			var firstRow = table.Split('\r', '\n').FirstOrDefault() ?? table;
			if (String.IsNullOrWhiteSpace(firstRow))
				return TableType.None;
			var tabCount = firstRow.Length - firstRow.Replace("\t", "").Length;
			var commaCount = firstRow.Length - firstRow.Replace(",", "").Length;
			var columnSepCount = firstRow.Length - firstRow.Replace("│", "").Length;
			if ((tabCount >= commaCount) && (tabCount >= columnSepCount))
				return TextEditor.TableType.TSV;
			if (commaCount >= columnSepCount)
				return TextEditor.TableType.CSV;
			return TextEditor.TableType.Columns;
		}

		string GetOutput(List<List<string>> data, TableType tableType)
		{
			switch (tableType)
			{
				case TableType.TSV: return String.Join("", data.Select(items => String.Join("\t", items.Select(item => ToTCSV(item, '\t'))) + Data.DefaultEnding));
				case TableType.CSV: return String.Join("", data.Select(items => String.Join(",", items.Select(item => ToTCSV(item, ','))) + Data.DefaultEnding));
				case TableType.Columns:
					{
						var numColumns = data.Max(line => line.Count);
						foreach (var line in data)
							line.AddRange(Enumerable.Range(0, numColumns - line.Count).Select(a => ""));
						var columnWidths = Enumerable.Range(0, numColumns).Select(column => data.Max(line => line[column].Length)).ToList();
						var columns = Enumerable.Range(0, numColumns).Where(column => columnWidths[column] != 0).ToList();
						return String.Join("", data.AsParallel().AsOrdered().Select(line => String.Join("│", columns.Select(column => line[column] + new string(' ', columnWidths[column] - line[column].Length))) + Data.DefaultEnding));
					}
				default: throw new ArgumentException("Invalid output type");
			}
		}

		internal EditTableDialog.Result Command_Edit_Table_Edit_Dialog()
		{
			SetTableSelection();
			return EditTableDialog.Run(UIHelper.FindParent<Window>(this), GetString(Selections[0]));
		}

		internal void Command_Edit_Table_Edit(EditTableDialog.Result result)
		{
			SetTableSelection();
			var strings = GetTableStrings(GetString(Selections[0]), result.InputTableType);
			if (result.InputHasHeaders)
				strings = strings.Skip(1).ToList();
			var data = GetTableObjects(strings, result.Types);
			data = AggregateByColumn(data, result.GroupByColumns, result.AggregateColumns);
			data = SortAggregateData(data, result.SortColumns);
			strings = data.Select(items => items.Select(item => (item ?? "").ToString()).ToList()).ToList();
			if (result.OutputHasHeaders)
				strings.Insert(0, result.ColumnHeaders);
			var output = GetOutput(strings, result.OutputTableType);
			ReplaceSelections(output);
		}

		internal void Command_Edit_Table_RegionsSelectionsToTable()
		{
			if (!Selections.Any())
				return;

			var regions = GetEnclosingRegions();
			var lines = Enumerable.Range(0, Selections.Count).GroupBy(index => regions[index]).Select(group => String.Join("\t", group.Select(index => ToTCSV(GetString(Selections[index]), '\t')))).ToList();
			Selections.Replace(Regions);
			Regions.Clear();
			ReplaceSelections(lines);
		}
	}
}
