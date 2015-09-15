using System;
using System.Collections.Generic;
using System.Linq;

namespace NeoEdit.TextEdit
{
	class Table
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

		public int NumRows { get { return Rows.Count; } }
		public int NumColumns { get { return Rows.Any() ? Rows[0].Count : 0; } }
		public List<List<object>> Rows { get; private set; }
		public List<string> Headers { get; private set; }
		public List<AggregateType> AggregateTypes { get; private set; }
		public List<Type> Types { get; private set; }
		public bool HasHeaders { get; private set; }
		public TableType OriginalTableType { get; private set; }

		public Table()
		{
			Rows = new List<List<object>>();
			Headers = new List<string>();
			AggregateTypes = new List<AggregateType>();
			Types = new List<Type>();
			HasHeaders = false;
			OriginalTableType = TableType.TSV;
		}

		Table(Table table)
		{
			Rows = table.Rows;
			Headers = table.Headers;
			AggregateTypes = table.AggregateTypes;
			Types = table.Types;
			HasHeaders = table.HasHeaders;
			OriginalTableType = TableType.TSV;
		}

		public Table(string input, TableType tableType = TableType.None, bool? hasHeaders = null)
		{
			if (tableType == TableType.None)
				tableType = GuessTableType(input);
			OriginalTableType = tableType;
			ProcessInput(input, tableType);
			SetTypesAndHeaders(hasHeaders);
		}

		public static TableType GuessTableType(string table)
		{
			var endLine = table.IndexOfAny(new char[] { '\r', '\n' });
			if (endLine == -1)
				endLine = table.Length;
			var firstRow = table.Substring(0, endLine);
			if (String.IsNullOrWhiteSpace(firstRow))
				return TableType.None;
			var tabCount = firstRow.Length - firstRow.Replace("\t", "").Length;
			var commaCount = firstRow.Length - firstRow.Replace(",", "").Length;
			var columnSepCount = firstRow.Length - firstRow.Replace("│", "").Length;
			if ((tabCount >= commaCount) && (tabCount >= columnSepCount))
				return TableType.TSV;
			if (commaCount >= columnSepCount)
				return TableType.CSV;
			return TableType.Columns;
		}

		void ProcessInput(string table, TableType tableType, int count = int.MaxValue)
		{
			switch (tableType)
			{
				case TableType.TSV: Rows = table.SplitTCSV('\t').Take(count).ToList(); break;
				case TableType.CSV: Rows = table.SplitTCSV(',').Take(count).ToList(); break;
				case TableType.Columns: Rows = table.SplitByLine().Select(line => line.Split('│').Select(item => item.TrimEnd() as object).ToList()).ToList(); break;
				default: throw new ArgumentException("Invalid input type");
			}

			EqualizeColumns();
			SetNulls();
		}

		void EqualizeColumns()
		{
			var numColumns = Rows.Max(row => row.Count);
			foreach (var row in Rows)
				while (row.Count < numColumns)
					row.Add(null);
		}

		void SetNulls()
		{
			foreach (var row in Rows)
				for (var column = 0; column < NumColumns; ++column)
				{
					var str = (string)row[column];
					if ((!String.IsNullOrEmpty(str)) && (str.Equals("NULL", StringComparison.OrdinalIgnoreCase)))
						row[column] = null;
				}
		}

		delegate bool TryParseDelegate<T>(string obj, out T result);
		static object ParseValue<T>(string str, TryParseDelegate<T> tryParse)
		{
			if (str == null)
				return null;

			T result;
			if (!tryParse(str, out result))
				return null;
			return result;
		}

		static Dictionary<Type, Func<string, object>> parsers = new Dictionary<Type, Func<string, object>>
		{
			{  typeof(long), str => ParseValue<long>(str, long.TryParse) },
			{  typeof(double), str => ParseValue<double>(str, double.TryParse) },
			{  typeof(DateTime), str => ParseValue<DateTime>(str, DateTime.TryParse) },
			{  typeof(string), str => str },
		};
		void SetTypesAndHeaders(bool? hasHeaders)
		{
			Types = new List<Type>();
			AggregateTypes = new List<AggregateType>();

			if ((!hasHeaders.HasValue) && ((!Rows.Any()) || (Rows[0].Any(item => item == null))))
				hasHeaders = false;

			for (var column = 0; column < NumColumns; ++column)
			{
				var match = parsers.First(pair => Rows.Skip(hasHeaders == false ? 0 : 1).All(row => (row[column] == null) || (pair.Value((string)row[column]) != null)));
				if ((!hasHeaders.HasValue) && (match.Value((string)Rows[0][column]) == null))
					hasHeaders = true;
				Types.Add(match.Key);
				AggregateTypes.Add(AggregateType.None);
			}

			HasHeaders = hasHeaders == true;
			if (HasHeaders)
			{
				Headers = Rows[0].Select(item => (string)item).ToList();
				Rows.RemoveAt(0);
			}
			else
			{
				Headers = Enumerable.Range(1, NumColumns).Select(column => String.Format("Column {0}", column)).ToList();
			}

			foreach (var row in Rows)
				for (var column = 0; column < NumColumns; ++column)
					row[column] = parsers[Types[column]]((string)row[column]);
		}

		object GetAggregateValue(AggregateType aggType, List<object> values)
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

		public Table Aggregate(List<int> groupByColumns, List<Tuple<int, AggregateType>> aggregateTypes)
		{
			List<List<List<object>>> groupedRows;
			if (groupByColumns.Any())
			{
				var groupMap = new Dictionary<string, List<List<object>>> { { "", Rows } };
				foreach (var column in groupByColumns)
					groupMap = groupMap.SelectMany(pair => pair.Value.GroupBy(items => pair.Key + "," + (items[column] ?? "").ToString())).ToDictionary(group => group.Key, group => group.ToList());
				groupedRows = groupMap.Values.ToList();
			}
			else
				groupedRows = Rows.Select(item => new List<List<object>> { item }).ToList();

			var newHeaders = new List<string>();
			var newAggregateTypes = new List<AggregateType>();
			var newTypes = new List<Type>();
			var newRows = groupedRows.Select(item => new List<object>()).ToList();

			foreach (var tuple in aggregateTypes)
			{
				for (var ctr = 0; ctr < groupedRows.Count; ++ctr)
					newRows[ctr].Add(GetAggregateValue(tuple.Item2, groupedRows[ctr].Select(item => item[tuple.Item1]).ToList()));

				newAggregateTypes.Add(tuple.Item2);
				newHeaders.Add(Headers[tuple.Item1] + (tuple.Item2 == AggregateType.None ? "" : " (" + tuple.Item2 + ")"));
				newTypes.Add(newRows.Select(row => row.Last()).Where(item => item != null).Select(item => item.GetType()).FirstOrDefault() ?? typeof(string));
			}

			return new Table(this)
			{
				Headers = newHeaders,
				AggregateTypes = newAggregateTypes,
				Rows = newRows,
				Types = newTypes,
			};
		}

		public Table Sort(List<Tuple<int, bool>> sortDescs)
		{
			if (!sortDescs.Any())
				return this;

			var sortedList = Rows.OrderBy(val => default(List<object>));
			foreach (var sortDesc in sortDescs)
			{
				if (sortDesc.Item2)
					sortedList = sortedList.ThenBy(items => items[sortDesc.Item1]);
				else
					sortedList = sortedList.ThenByDescending(items => items[sortDesc.Item1]);
			}
			return new Table(this) { Rows = sortedList.ToList() };
		}

		public static string ToTCSV(string str, char split)
		{
			if (str.IndexOfAny(new char[] { split, '"' }) != -1)
				return String.Format("\"{0}\"", str.Replace("\"", "\"\""));
			return str;
		}

		public string ToString(TableType tableType, bool hasHeaders, string ending)
		{
			var result = new List<List<string>>();
			if (hasHeaders)
				result.Add(Headers);
			result.AddRange(Rows.Select(row => row.Select(item => (item ?? "").ToString()).ToList()));

			switch (tableType)
			{
				case TableType.TSV: return String.Join("", result.Select(items => String.Join("\t", items.Select(item => ToTCSV(item, '\t'))) + ending));
				case TableType.CSV: return String.Join("", result.Select(items => String.Join(",", items.Select(item => ToTCSV(item, ','))) + ending));
				case TableType.Columns:
					{
						var columnWidths = Enumerable.Range(0, NumColumns).Select(column => result.Max(line => line[column].Length)).ToList();
						var columns = Enumerable.Range(0, NumColumns).Where(column => columnWidths[column] != 0).ToList();
						return String.Join("", result.AsParallel().AsOrdered().Select(line => String.Join("│", columns.Select(column => line[column] + new string(' ', columnWidths[column] - line[column].Length))) + ending));
					}
				default: throw new ArgumentException("Invalid output type");
			}
		}
	}

	static class TableExtensions
	{
		static internal IEnumerable<string> SplitByLine(this string item)
		{
			var lineBreakChars = new char[] { '\r', '\n' };
			var pos = 0;
			while (pos < item.Length)
			{
				var index = item.IndexOfAny(lineBreakChars, pos);
				if (index == -1)
					index = item.Length;
				yield return item.Substring(pos, index - pos);
				if ((index + 1 < item.Length) && (item[index] == '\r') && (item[index + 1] == '\n'))
					++index;
				pos = index + 1;
			}
		}

		static List<object> SplitTCSV(string source, char splitChar, ref int pos)
		{
			var findChars = new char[] { splitChar, '\r', '\n' };
			var result = new List<object>();
			while (true)
			{
				var item = "";
				if ((pos < source.Length) && (source[pos] == '"'))
				{
					var quoteIndex = pos + 1;
					while (true)
					{
						quoteIndex = source.IndexOf('"', quoteIndex);
						if (quoteIndex == -1)
						{
							item += source.Substring(pos + 1).Replace(@"""""", @"""");
							pos = source.Length;
							break;
						}

						if ((quoteIndex + 1 < source.Length) && (source[quoteIndex + 1] == '"'))
						{
							quoteIndex += 2;
							continue;
						}

						item += source.Substring(pos + 1, quoteIndex - pos - 1).Replace(@"""""", @"""");
						pos = quoteIndex + 1;
						break;
					}
				}

				var splitIndex = source.IndexOfAny(findChars, pos);
				var end = (splitIndex == -1) || (source[splitIndex] != splitChar);
				if (splitIndex == -1)
					splitIndex = source.Length;
				item += source.Substring(pos, splitIndex - pos);
				result.Add(item);

				pos = splitIndex;

				if (end)
				{
					if ((pos + 1 < source.Length) && (source[pos] == '\r') && (source[pos + 1] == '\n'))
						pos += 2;
					else if (pos < source.Length)
						++pos;
					break;
				}
				++pos;
			}
			return result;
		}

		static internal IEnumerable<List<object>> SplitTCSV(this string source, char splitChar)
		{
			var pos = 0;
			while (pos < source.Length)
				yield return SplitTCSV(source, splitChar, ref pos);
			yield break;
		}
	}
}
