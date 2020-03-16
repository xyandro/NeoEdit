using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using NeoEdit.Program.Transform;

namespace NeoEdit.Program
{
	public class Table
	{
		const string NULLSTR = "NULL";

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

		public enum JoinType
		{
			Inner,
			Left,
			Full,
			Right,
			Cross,
			LeftExc,
			RightExc,
			FullExc,
		}

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

		public Table(string input, ParserType tableType = ParserType.None, bool hasHeaders = true) : this(GetInputRows(input, tableType), hasHeaders) { }

		public Table(List<List<string>> rows, bool hasHeaders = true)
		{
			Rows = rows.Select(row => row.Select(value => value ?? NULLSTR).ToList()).ToList();
			EqualizeColumns();

			if ((Rows.Any()) && (hasHeaders))
			{
				Headers = Rows[0];
				Rows.RemoveAt(0);
			}
			else
				Headers = Rows[0].Select((value, index) => $"Column {index + 1}").ToList();
		}

		string GetDBValue(object value)
		{
			if ((value == null) || (value is DBNull))
				return NULLSTR;

			if (value is byte[] valueBytes)
				return $"'{Coder.BytesToString(valueBytes, Coder.CodePage.Hex)}'";

			if (value is string valueStr)
				return $"'{valueStr.Replace("'", "''")}'";

			return value.ToString();
		}

		public Table(DbDataReader reader)
		{
			Headers = Enumerable.Range(0, reader.FieldCount).Select(column => reader.GetName(column)).ToList();
			Rows = new List<List<string>>();
			while (reader.Read())
				Rows.Add(Enumerable.Range(0, reader.FieldCount).Select(column => GetDBValue(reader[column])).ToList());
		}

		public static ParserType GuessTableType(string input)
		{
			var endLine = input.IndexOfAny(new char[] { '\r', '\n' });
			if (endLine == -1)
				endLine = input.Length;
			var firstRow = input.Substring(0, endLine);
			if (string.IsNullOrWhiteSpace(firstRow))
				return ParserType.None;
			var tabCount = firstRow.Length - firstRow.Replace("\t", "").Length;
			var commaCount = firstRow.Length - firstRow.Replace(",", "").Length;
			var columnSepCount = firstRow.Length - firstRow.Replace("│", "").Replace("║", "").Length;
			if ((tabCount >= commaCount) && (tabCount >= columnSepCount))
				return ParserType.TSV;
			if (commaCount >= columnSepCount)
				return ParserType.CSV;
			return ParserType.Columns;
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

		static string TrimExactColumns(string item)
		{
			item = item.Trim();
			if ((item.Length >= 2) && (item.StartsWith(@"""")) && (item.EndsWith(@"""")))
				item = item.Substring(1, item.Length - 2).Replace(@"""""", @"""");
			return item;
		}

		static List<List<string>> GetInputRows(string input, ParserType tableType)
		{
			switch (tableType)
			{
				case ParserType.TSV: return input.SplitTCSV('\t').Select(split => split.ToList()).ToList();
				case ParserType.CSV: return input.SplitTCSV(',').Select(split => split.ToList()).ToList();
				case ParserType.Columns: return SplitByDoublePipe(input).Select(line => line.Split('│').Select(item => item.Trim()).ToList()).ToList();
				case ParserType.ExactColumns: return SplitByDoublePipe(input).Select(line => line.Split('│').Select(item => TrimExactColumns(item)).ToList()).ToList();
				default: throw new ArgumentException("Invalid input type");
			}
		}

		void EqualizeColumns()
		{
			var numColumns = Rows.Max(row => row.Count);
			foreach (var row in Rows)
				while (row.Count < numColumns)
					row.Add("");
		}

		public static string ToTCSV(string str, char split)
		{
			if (str.IndexOfAny(new char[] { split, '"', '\r', '\n' }) != -1)
				return $"\"{str.Replace("\"", "\"\"")}\"";
			return str;
		}

		public override string ToString() => ToString("\r\n", ParserType.Columns);

		public string ToString(string ending, ParserType tableType = ParserType.Columns)
		{
			var result = new List<List<string>>();
			result.Add(Headers.ToList());
			result.AddRange(Rows.Select(row => row.Select(item => item.ToString()).ToList()));

			switch (tableType)
			{
				case ParserType.TSV: return string.Join("", result.Select(items => string.Join("\t", items.Select(item => ToTCSV(item, '\t'))) + ending));
				case ParserType.CSV: return string.Join("", result.Select(items => string.Join(",", items.Select(item => ToTCSV(item, ','))) + ending));
				case ParserType.Columns:
					{
						var newLineChars = new char[] { '\r', '\n' };
						result = result.Select(row => row.Select(value => value.Trim()).ToList()).ToList();
						var columnWidths = Enumerable.Range(0, Headers.Count).Select(column => result.Max(line => line[column].IndexOfAny(newLineChars) == -1 ? line[column].Length : 0)).ToList();
						return string.Join("", result.AsParallel().AsOrdered().Select(line => "║ " + string.Join(" │ ", Enumerable.Range(0, Headers.Count).Select(column => line[column] + new string(' ', Math.Max(columnWidths[column] - line[column].Length, 0)))) + " ║" + ending));
					}
				case ParserType.ExactColumns:
					{
						var newLineChars = new char[] { '\r', '\n' };
						result = result.Select(row => row.Select(value => $@"""{value.Replace(@"""", @"""""")}""").ToList()).ToList();
						var columnWidths = Enumerable.Range(0, Headers.Count).Select(column => result.Max(line => line[column].IndexOfAny(newLineChars) == -1 ? line[column].Length : 0)).ToList();
						return string.Join("", result.AsParallel().AsOrdered().Select(line => "║ " + string.Join(" │ ", Enumerable.Range(0, Headers.Count).Select(column => line[column] + new string(' ', Math.Max(columnWidths[column] - line[column].Length, 0)))) + " ║" + ending));
					}
				default: throw new ArgumentException("Invalid output type");
			}
		}

		public void AddRow() => Rows.Add(Headers.Select(header => "").ToList());

		public void AddColumn(string columnName, List<string> results)
		{
			if (results.Count != NumRows)
				throw new ArgumentException("Invalid row count");
			Headers.Add(columnName);
			Enumerable.Range(0, NumRows).ForEach(row => Rows[row].Add(results[row] ?? NULLSTR));
		}

		string GetAggregateValue(AggregateType aggType, List<string> values)
		{
			switch (aggType)
			{
				case AggregateType.Group: return values.Distinct().Single();
				case AggregateType.All: return string.Join(", ", values);
				case AggregateType.Distinct: return string.Join(", ", values.Distinct().OrderBy());
				case AggregateType.Min: return values.Min();
				case AggregateType.Max: return values.Max();
				case AggregateType.Sum: return values.Select(value => Convert.ToDouble(value)).Sum().ToString();
				case AggregateType.Count: return values.Count.ToString();
				case AggregateType.CountNonNull: return values.Where(value => value != NULLSTR).Count().ToString();
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

			return new Table
			{
				Headers = newHeaders,
				Rows = newRows,
			};
		}

		static object JoinValue(string value) => value == NULLSTR ? new object() : value;

		static IEnumerable<string> CombinedValue(IEnumerable<string> left, IEnumerable<string> right, JoinType joinType)
		{
			if (joinType != JoinType.RightExc)
				foreach (var item in left)
					yield return item;
			if (joinType != JoinType.LeftExc)
				foreach (var item in right)
					yield return item;
		}

		static public Table Join(Table leftTable, Table rightTable, List<int> leftColumns, List<int> rightColumns, JoinType joinType)
		{
			if (joinType == JoinType.Cross)
			{
				leftColumns = new List<int>();
				rightColumns = new List<int>();
			}

			if ((leftColumns.Count != rightColumns.Count))
				throw new ArgumentException("Column counts must match to join");

			var left = leftTable.Rows.GroupBy(row => leftColumns.Select(column => JoinValue(row[column])).ToItemSet()).ToDictionary(group => group.Key, group => group.ToList());
			var right = rightTable.Rows.GroupBy(row => rightColumns.Select(column => JoinValue(row[column])).ToItemSet()).ToDictionary(group => group.Key, group => group.ToList());

			List<ItemSet<object>> keys;
			switch (joinType)
			{
				case JoinType.Inner: keys = left.Keys.Intersect(right.Keys).ToList(); break;
				case JoinType.Left: keys = left.Keys.ToList(); break;
				case JoinType.LeftExc: keys = left.Keys.Except(right.Keys).ToList(); break;
				case JoinType.Right: keys = right.Keys.ToList(); break;
				case JoinType.RightExc: keys = right.Keys.Except(left.Keys).ToList(); break;
				case JoinType.Full: keys = left.Keys.Concat(right.Keys).Distinct().ToList(); break;
				case JoinType.FullExc: keys = left.Keys.Concat(right.Keys).Distinct().Except(left.Keys).Except(right.Keys).ToList(); break;
				case JoinType.Cross: keys = new List<ItemSet<object>> { new ItemSet<object>() }; break;
				default: throw new ArgumentException("Invalid join");
			}

			var emptyLeft = new List<List<string>> { Enumerable.Repeat(NULLSTR, leftTable.NumColumns).ToList() };
			var emptyRight = new List<List<string>> { Enumerable.Repeat(NULLSTR, rightTable.NumColumns).ToList() };
			return new Table
			{
				Headers = CombinedValue(leftTable.Headers, rightTable.Headers, joinType).ToList(),
				Rows = keys.SelectMany(key => (left.ContainsKey(key) ? left[key] : emptyLeft).SelectMany(leftValue => (right.ContainsKey(key) ? right[key] : emptyRight).Select(rightValue => CombinedValue(leftValue, rightValue, joinType).ToList()))).ToList(),
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
			var comparer = Helpers.SmartComparer();
			var order = Enumerable.Range(0, Rows.Count).ToList();
			var ordering = order.OrderBy(rowIndex => 0);
			foreach (var data in sortData)
				if (data.Ascending)
					ordering = ordering.ThenBy(rowIndex => this[rowIndex, data.Column] ?? "", comparer);
				else
					ordering = ordering.ThenByDescending(rowIndex => this[rowIndex, data.Column] ?? "", comparer);

			return new Table
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
			get { return Rows[row][column] == NULLSTR ? null : Rows[row][column]; }
			set { Rows[row][column] = value ?? NULLSTR; }
		}
	}
}
