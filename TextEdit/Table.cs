using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using NeoEdit.Common;
using NeoEdit.TextEdit.Content;

namespace NeoEdit.TextEdit
{
	public class Table
	{
		const string NULL = "<NULL>";

		public enum AggregateType
		{
			None,
			Group,
			All,
			Distinct,
			Min,
			Max,
			Sum,
			Average,
			Count,
			CountNonNull,
		}

		public enum JoinType { Inner, LeftOuter, RightOuter, FullOuter }

		List<List<string>> Rows { get; set; }
		List<string> Headers { get; set; }

		public int NumRows => Rows.Count;
		public int NumColumns => Headers.Count;

		public string GetHeader(int index) => Headers[index];

		public Table()
		{
			Rows = new List<List<string>>();
			Headers = new List<string>();
		}

		public Table(string input, Parser.ParserType tableType = Parser.ParserType.None, bool hasHeaders = true) : this(GetInputRows(input, tableType), hasHeaders) { }

		public Table(List<List<string>> rows, bool hasHeaders = true)
		{
			Rows = rows.Select(row => row.Select(value => value ?? NULL).ToList()).ToList();
			EqualizeColumns();

			if ((Rows.Any()) && (hasHeaders))
			{
				Headers = Rows[0];
				Rows.RemoveAt(0);
			}
			else
				Headers = Rows[0].Select((value, index) => $"Column {index + 1}").ToList();
		}

		public Table(DbDataReader reader)
		{
			Headers = Enumerable.Range(0, reader.FieldCount).Select(column => reader.GetName(column)).ToList();
			Rows = new List<List<string>>();
			while (reader.Read())
				Rows.Add(Enumerable.Range(0, reader.FieldCount).Select(column => reader[column]).Select(value => value == DBNull.Value ? NULL : value.ToString()).ToList());
		}

		public static Parser.ParserType GuessTableType(string input)
		{
			var endLine = input.IndexOfAny(new char[] { '\r', '\n' });
			if (endLine == -1)
				endLine = input.Length;
			var firstRow = input.Substring(0, endLine);
			if (String.IsNullOrWhiteSpace(firstRow))
				return Parser.ParserType.None;
			var tabCount = firstRow.Length - firstRow.Replace("\t", "").Length;
			var commaCount = firstRow.Length - firstRow.Replace(",", "").Length;
			var columnSepCount = firstRow.Length - firstRow.Replace("│", "").Length;
			if ((tabCount >= commaCount) && (tabCount >= columnSepCount))
				return Parser.ParserType.TSV;
			if (commaCount >= columnSepCount)
				return Parser.ParserType.CSV;
			return Parser.ParserType.Columns;
		}

		static IEnumerable<string> SplitByDoublePipe(string item)
		{
			var pos = 0;
			while (pos < item.Length)
			{
				var startIndex = item.IndexOf('║', pos);
				if (startIndex == -1)
					break;
				var endIndex = item.IndexOf('║', startIndex + 1);
				if (endIndex == -1)
					endIndex = item.Length;
				yield return item.Substring(startIndex + 1, endIndex - startIndex - 1);
				pos = endIndex + 1;
			}
		}

		static List<List<string>> GetInputRows(string input, Parser.ParserType tableType)
		{
			if (tableType == Parser.ParserType.None)
				tableType = GuessTableType(input);

			switch (tableType)
			{
				case Parser.ParserType.TSV: return input.SplitTCSV('\t').Select(split => split.ToList()).ToList();
				case Parser.ParserType.CSV: return input.SplitTCSV(',').Select(split => split.ToList()).ToList();
				case Parser.ParserType.Columns: return SplitByDoublePipe(input).Select(line => line.Split('│').Select(item => item.Trim()).ToList()).ToList();
				default: throw new ArgumentException("Invalid input type");
			}
		}

		void EqualizeColumns()
		{
			var numColumns = Rows.Max(row => row.Count);
			foreach (var row in Rows)
				while (row.Count < numColumns)
					row.Add(NULL);
		}

		public static string ToTCSV(string str, char split)
		{
			if (str.IndexOfAny(new char[] { split, '"', '\r', '\n' }) != -1)
				return $"\"{str.Replace("\"", "\"\"")}\"";
			return str;
		}

		public override string ToString() => ToString("\r\n", Parser.ParserType.Columns);

		public string ToString(string ending, Parser.ParserType tableType = Parser.ParserType.Columns)
		{
			var result = new List<List<string>>();
			result.Add(Headers.ToList());
			result.AddRange(Rows.Select(row => row.Select(item => item.ToString()).ToList()));

			switch (tableType)
			{
				case Parser.ParserType.TSV: return String.Join("", result.Select(items => String.Join("\t", items.Select(item => ToTCSV(item, '\t'))) + ending));
				case Parser.ParserType.CSV: return String.Join("", result.Select(items => String.Join(",", items.Select(item => ToTCSV(item, ','))) + ending));
				case Parser.ParserType.Columns:
					{
						var newLineChars = new char[] { '\r', '\n' };
						var columnWidths = Enumerable.Range(0, Headers.Count).Select(column => result.Max(line => line[column].IndexOfAny(newLineChars) == -1 ? line[column].Length : 0)).ToList();
						return String.Join("", result.AsParallel().AsOrdered().Select(line => "║ " + String.Join(" │ ", Enumerable.Range(0, Headers.Count).Select(column => line[column] + new string(' ', Math.Max(columnWidths[column] - line[column].Length, 0)))) + " ║" + ending));
					}
				default: throw new ArgumentException("Invalid output type");
			}
		}

		string GetAggregateValue(AggregateType aggType, List<string> values)
		{
			switch (aggType)
			{
				case AggregateType.Group: return values.Distinct().Single();
				case AggregateType.All: return String.Join(", ", values);
				case AggregateType.Distinct: return String.Join(", ", values.Distinct().OrderBy());
				case AggregateType.Min: return values.Min();
				case AggregateType.Max: return values.Max();
				case AggregateType.Sum: return values.Select(value => Convert.ToDouble(value)).Sum().ToString();
				case AggregateType.Count: return values.Count.ToString();
				case AggregateType.CountNonNull: return values.Where(value => value != NULL).Count().ToString();
				case AggregateType.Average: return (values.Select(value => Convert.ToDouble(value)).Sum() / values.Count).ToString();
				default: throw new Exception("Invalid table type");
			}
		}

		public class AggregateData
		{
			public int Column { get; set; }
			public AggregateType Aggregation { get; set; }
			public AggregateData(int column, AggregateType aggregation = AggregateType.All)
			{
				Column = column;
				Aggregation = aggregation;
			}
		}

		public Table Aggregate(List<AggregateData> aggregateData)
		{
			var changeHeaders = true;
			var groupByColumns = aggregateData.Where(data => data.Aggregation == AggregateType.Group).Select(data => data.Column).ToList();
			List<List<List<string>>> groupedRows;
			if (!groupByColumns.Any())
			{
				groupedRows = Rows.Select(row => new List<List<string>> { row }).ToList();
				changeHeaders = false;
			}
			else
			{
				var groupMap = Rows.GroupBy(row => new ItemSet<object>());
				foreach (var column in groupByColumns)
					groupMap = groupMap.SelectMany(group => group.GroupBy(items => group.Key.Concat(items[column]).ToItemSet()));
				groupedRows = groupMap.Select(group => group.ToList()).ToList();
			}

			var newHeaders = new List<string>();
			var newRows = groupedRows.Select(item => new List<string>()).ToList();

			foreach (var data in aggregateData)
			{
				for (var ctr = 0; ctr < groupedRows.Count; ++ctr)
					newRows[ctr].Add(GetAggregateValue(data.Aggregation, groupedRows[ctr].Select(item => item[data.Column]).ToList()));

				newHeaders.Add($"{Headers[data.Column]}{((data.Aggregation == AggregateType.None) || (!changeHeaders) ? "" : $" ({data.Aggregation})")}");
			}

			return new Table()
			{
				Headers = newHeaders,
				Rows = newRows,
			};
		}

		static object JoinValue(string value) => value == NULL ? new object() : value;

		static public Table Join(Table leftTable, Table rightTable, List<int> leftColumns, List<int> rightColumns, JoinType joinType)
		{
			if ((leftColumns.Count != rightColumns.Count))
				throw new ArgumentException("Column counts must match to join");

			var left = leftTable.Rows.GroupBy(row => leftColumns.Select(column => JoinValue(row[column])).ToItemSet()).ToDictionary(group => group.Key, group => group.ToList());
			var right = rightTable.Rows.GroupBy(row => rightColumns.Select(column => JoinValue(row[column])).ToItemSet()).ToDictionary(group => group.Key, group => group.ToList());

			List<ItemSet<object>> keys;
			switch (joinType)
			{
				case JoinType.Inner: keys = left.Keys.Where(key => right.ContainsKey(key)).ToList(); break;
				case JoinType.LeftOuter: keys = left.Keys.ToList(); break;
				case JoinType.RightOuter: keys = right.Keys.ToList(); break;
				case JoinType.FullOuter: keys = left.Keys.Concat(right.Keys).Distinct().ToList(); break;
				default: throw new ArgumentException("Invalid join");
			}

			var emptyLeft = new List<List<string>> { Enumerable.Repeat(NULL, leftTable.NumColumns).ToList() };
			var emptyRight = new List<List<string>> { Enumerable.Repeat(NULL, rightTable.NumColumns).ToList() };
			return new Table()
			{
				Headers = leftTable.Headers.Concat(rightTable.Headers).ToList(),
				Rows = keys.SelectMany(key => (left.ContainsKey(key) ? left[key] : emptyLeft).SelectMany(leftValue => (right.ContainsKey(key) ? right[key] : emptyRight).Select(rightValue => leftValue.Concat(rightValue).ToList()))).ToList(),
			};
		}

		public class SortData
		{
			public int Column { get; set; }
			public bool Ascending { get; set; }
			public SortData(int column, bool ascending = true)
			{
				Column = column;
				Ascending = ascending;
			}
		}

		public Table Sort(List<SortData> sortData)
		{
			var order = Enumerable.Range(0, Rows.Count).ToList();
			var ordering = order.OrderBy(rowIndex => 0);
			foreach (var data in sortData)
				if (data.Ascending)
					ordering = ordering.ThenBy(rowIndex => this[rowIndex, data.Column]);
				else
					ordering = ordering.ThenByDescending(rowIndex => this[rowIndex, data.Column]);

			return new Table()
			{
				Headers = Headers,
				Rows = ordering.Select(index => Rows[index]).ToList(),
			};
		}

		public Table Transpose()
		{
			var result = new List<List<string>> { Headers };
			result.AddRange(Rows);
			return new Table(Enumerable.Range(0, NumColumns).Select(column => Enumerable.Range(0, result.Count).Select(row => result[row][column]).ToList()).ToList(), true);
		}

		public string this[int row, int column]
		{
			get { return Rows[row][column]; }
			set { Rows[row][column] = value; }
		}
	}
}
