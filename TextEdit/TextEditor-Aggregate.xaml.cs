using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using NeoEdit.GUI.Controls;
using NeoEdit.TextEdit.Dialogs;

namespace NeoEdit.TextEdit
{
	public partial class TextEditor
	{
		internal enum AggregateInputType { None, TSV, CSV, Piped }
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

		List<string> GetAggregateLines(bool preview = false)
		{
			var result = Selections.Select(range => GetString(range)).SplitByLine();
			if (preview)
				result = result.Take(1000);
			return result.ToList();
		}

		internal static List<List<string>> GetAggregateStrings(List<string> input, AggregateInputType inputType)
		{
			List<List<string>> data;
			switch (inputType)
			{
				case AggregateInputType.TSV: data = input.Select(item => item.SplitTCSV('\t').ToList()).ToList(); break;
				case AggregateInputType.CSV: data = input.Select(item => item.SplitTCSV(',').ToList()).ToList(); break;
				case AggregateInputType.Piped: data = input.Select(item => item.Split('|').Select(str => str.Trim()).ToList()).ToList(); break;
				default: throw new Exception("Invalid aggregation type");
			}

			if (data.Count == 0)
				return new List<List<string>>();

			var numColumns = data.Max(row => row.Count);
			data = data.Select(row => row.Expand(numColumns, "").ToList()).ToList();
			var used = Enumerable.Range(0, numColumns).Select(index => data.Any(items => !String.IsNullOrEmpty(items[index]))).ToList();
			data = data.Select(items => items.Where((item, index) => used[index]).ToList()).ToList();
			return data;
		}

		internal static List<List<object>> GetAggregateObjects(List<List<string>> strFields, List<Type> types)
		{
			var parsers = new Dictionary<Type, Func<string, object>>
			{
				{ typeof(long), str => long.Parse(str) },
				{ typeof(double), str => double.Parse(str) },
				{ typeof(DateTime), str => DateTime.Parse(str) },
				{ typeof(string), str => str },
			};
			return strFields.Select(items => items.Select((item, index) => parsers[types[index]](item)).ToList()).ToList();
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
				default: throw new Exception("Invalid aggregation type");
			}
		}

		static internal List<List<object>> SortAggregateData(List<List<object>> data, List<Tuple<int, bool>> sortDescs)
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

		internal AggregateDialog.Result Command_Edit_Aggregate_Dialog()
		{
			return AggregateDialog.Run(UIHelper.FindParent<Window>(this), GetAggregateLines(true));
		}

		string ToTCSV(object obj, char split)
		{
			var str = (obj ?? "").ToString();
			if (str.IndexOfAny(new char[] { split, '"' }) != -1)
				str = String.Format("\"{0}\"", str.Replace("\"", "\"\""));
			return str;
		}

		internal void Command_Edit_Aggregate(AggregateDialog.Result result)
		{
			var lines = GetAggregateLines();
			var strings = GetAggregateStrings(lines, result.InputType);
			if (result.HasHeaders)
				strings = strings.Skip(1).ToList();
			var data = GetAggregateObjects(strings, result.Types);
			data = AggregateByColumn(data, result.GroupByColumns, result.AggregateColumns);
			data = SortAggregateData(data, result.SortColumns);
			data.Insert(0, result.ColumnHeaders.Select(str => str as object).ToList());

			List<string> outputs;
			switch (result.InputType)
			{
				case AggregateInputType.TSV: outputs = data.Select(items => String.Join("\t", items.Select(item => ToTCSV(item, '\t')))).ToList(); break;
				case AggregateInputType.CSV: outputs = data.Select(items => String.Join(",", items.Select(item => ToTCSV(item, ',')))).ToList(); break;
				case AggregateInputType.Piped: outputs = LinesToTable(data.Select(items => items.Select(item => item.ToString()).ToList()).ToList()); break;
				default: throw new ArgumentException("Invalid output type");
			}

			var location = Selections.Any() ? Selections[0].Start : BeginOffset();
			location = Data.GetOffset(Data.GetOffsetLine(location), 0);
			var sb = new StringBuilder();
			var newSels = new List<Range>();
			foreach (var output in outputs)
			{
				newSels.Add(Range.FromIndex(location + sb.Length, output.Length));
				sb.Append(output);
				sb.Append(Data.DefaultEnding);
			}
			sb.Append(Data.DefaultEnding);
			Replace(new List<Range> { new Range(location) }, new List<string> { sb.ToString() });
			Selections.Replace(newSels);
		}
	}
}
