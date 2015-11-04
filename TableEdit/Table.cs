using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using NeoEdit.Common;

namespace NeoEdit.TableEdit
{
	public class Table
	{
		[Flags]
		public enum AggregateType
		{
			None = 0,
			Value = 1,
			Distinct = 2,
			Concat = 4,
			Min = 8,
			Max = 16,
			Sum = 32,
			Average = 64,
			Count = 128,
		}

		public enum JoinType { Inner, LeftOuter, RightOuter, FullOuter }

		public enum TableTypeEnum { None, TSV, CSV, Columns }

		List<List<object>> Rows { get; set; }
		public List<string> Headers { get; private set; }
		public TableTypeEnum TableType { get; set; }
		public bool HasHeaders { get; set; }

		public int NumRows => Rows.Count;
		public int NumColumns => Headers.Count;

		public Table()
		{
			Rows = new List<List<object>>();
			Headers = new List<string>();
			TableType = TableTypeEnum.TSV;
			HasHeaders = false;
		}

		Table(Table table)
		{
			Rows = table.Rows.Select(row => row.ToList()).ToList();
			Headers = table.Headers.ToList();
			TableType = table.TableType;
			HasHeaders = table.HasHeaders;
		}

		public Table(string input, TableTypeEnum tableType = TableTypeEnum.None, bool? hasHeaders = null)
		{
			if (tableType == TableTypeEnum.None)
				tableType = GuessTableType(input);
			ProcessInput(input, tableType);
			SetHeadersAndTypes(hasHeaders);
		}

		public Table(DbDataReader reader)
		{
			Headers = Enumerable.Range(0, reader.FieldCount).Select(column => reader.GetName(column)).ToList();
			HasHeaders = true;
			TableType = TableTypeEnum.TSV;

			Rows = new List<List<object>>();
			while (reader.Read())
				Rows.Add(Enumerable.Range(0, reader.FieldCount).Select(column => reader[column]).Select(value => value == DBNull.Value ? null : value).ToList());
		}

		public static TableTypeEnum GuessTableType(string table)
		{
			var endLine = table.IndexOfAny(new char[] { '\r', '\n' });
			if (endLine == -1)
				endLine = table.Length;
			var firstRow = table.Substring(0, endLine);
			if (String.IsNullOrWhiteSpace(firstRow))
				return TableTypeEnum.None;
			var tabCount = firstRow.Length - firstRow.Replace("\t", "").Length;
			var commaCount = firstRow.Length - firstRow.Replace(",", "").Length;
			var columnSepCount = firstRow.Length - firstRow.Replace("│", "").Length;
			if ((tabCount >= commaCount) && (tabCount >= columnSepCount))
				return TableTypeEnum.TSV;
			if (commaCount >= columnSepCount)
				return TableTypeEnum.CSV;
			return TableTypeEnum.Columns;
		}

		void ProcessInput(string table, TableTypeEnum tableType)
		{
			switch (tableType)
			{
				case TableTypeEnum.TSV: Rows = table.SplitTCSV('\t').ToList(); break;
				case TableTypeEnum.CSV: Rows = table.SplitTCSV(',').ToList(); break;
				case TableTypeEnum.Columns: Rows = table.SplitByLine().Select(line => line.Split('│').Select(item => item.TrimEnd() as object).ToList()).ToList(); break;
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
				for (var column = 0; column < row.Count; ++column)
				{
					var str = (string)row[column];
					if ((!String.IsNullOrEmpty(str)) && (str.Equals("NULL", StringComparison.OrdinalIgnoreCase)))
						row[column] = null;
				}
		}

		delegate bool TryParseDelegate<T>(string obj, out T result);
		static T? TypeParser<T>(string str, TryParseDelegate<T> tryParse) where T : struct
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
			[typeof(long)] = str => TypeParser<long>(str, long.TryParse),
			[typeof(double)] = str => TypeParser<double>(str, double.TryParse),
			[typeof(bool)] = str => TypeParser<bool>(str, bool.TryParse),
			[typeof(DateTime)] = str => TypeParser<DateTime>(str, DateTime.TryParse),
			[typeof(string)] = str => str,
		};

		public static object GetValue(string value)
		{
			if (value == null)
				return null;
			return parsers.Select(parser => parser.Value(value)).Where(str => str != null).First();
		}

		void SetHeadersAndTypes(bool? hasHeaders)
		{
			if ((!hasHeaders.HasValue) && ((!Rows.Any()) || (Rows[0].Any(item => item == null))))
				hasHeaders = false;
			HasHeaders = hasHeaders != false;

			Headers = Enumerable.Range(1, Rows.Any() ? Rows[0].Count : 0).Select(column => $"Column {column}").ToList();
			if (HasHeaders)
			{
				Headers = Headers.Select((header, column) => (string)this[0, column]).ToList();
				Rows.RemoveAt(0);
			}

			foreach (var row in Rows)
				for (var column = 0; column < Headers.Count; ++column)
					row[column] = GetValue(row[column] as string);
		}

		public List<int> GetSortOrder(List<int> columns)
		{
			var order = Enumerable.Range(0, Rows.Count).ToList();
			var ordering = order.OrderBy(rowIndex => 0);
			foreach (var column in columns)
				ordering = ordering.ThenBy(rowIndex => this[rowIndex, column]);
			if (ordering.InOrder())
			{
				ordering = order.OrderBy(rowIndex => 0);
				foreach (var column in columns)
					ordering = ordering.ThenByDescending(rowIndex => this[rowIndex, column]);
			}
			return ordering.ToList();
		}

		public int GetRowIndex(List<object> row) => Rows.IndexOf(row);

		public static string ToTCSV(string str, char split)
		{
			if (str.IndexOfAny(new char[] { split, '"' }) != -1)
				return $"\"{str.Replace("\"", "\"\"")}\"";
			return str;
		}

		public string ConvertToString(string ending, TableTypeEnum tableType = TableTypeEnum.TSV, bool hasHeaders = true)
		{
			var result = new List<List<string>>();
			if (hasHeaders)
				result.Add(Headers.ToList());
			result.AddRange(Rows.Select(row => row.Select(item => (item ?? "NULL").ToString()).ToList()));

			switch (tableType)
			{
				case TableTypeEnum.TSV: return String.Join("", result.Select(items => String.Join("\t", items.Select(item => ToTCSV(item, '\t'))) + ending));
				case TableTypeEnum.CSV: return String.Join("", result.Select(items => String.Join(",", items.Select(item => ToTCSV(item, ','))) + ending));
				case TableTypeEnum.Columns:
					{
						var columnWidths = Enumerable.Range(0, Headers.Count).Select(column => result.Max(line => line[column].Length)).ToList();
						var columns = Enumerable.Range(0, Headers.Count).Where(column => columnWidths[column] != 0).ToList();
						return String.Join("", result.AsParallel().AsOrdered().Select(line => String.Join("│", columns.Select(column => line[column] + new string(' ', columnWidths[column] - line[column].Length))) + ending));
					}
				default: throw new ArgumentException("Invalid output type");
			}
		}

		object GetAggregateValue(AggregateType aggType, List<object> values)
		{
			switch (aggType)
			{
				case AggregateType.Value: return values.Distinct().Single();
				case AggregateType.Distinct: return String.Join(", ", values.Distinct().OrderBy());
				case AggregateType.Concat: return String.Join(", ", values.OrderBy());
				case AggregateType.Min: return values.Min();
				case AggregateType.Max: return values.Max();
				case AggregateType.Sum: return values.Select(value => Convert.ToDouble(value)).Sum();
				case AggregateType.Count: return values.Count;
				case AggregateType.Average: return values.Select(value => Convert.ToDouble(value)).Sum() / values.Count;
				default: throw new Exception("Invalid table type");
			}
		}

		public Table Aggregate(List<int> groupByColumns, List<Tuple<int, AggregateType>> aggregateData)
		{
			var groupMap = Rows.GroupBy(row => new ItemSet<object>());
			foreach (var column in groupByColumns)
				groupMap = groupMap.SelectMany(group => group.GroupBy(items => group.Key.Concat(items[column]).ToItemSet()));
			var groupedRows = groupMap.Select(group => group.ToList()).ToList();

			var newHeaders = new List<string>();
			var newRows = groupedRows.Select(item => new List<object>()).ToList();

			foreach (var tuple in aggregateData)
			{
				for (var ctr = 0; ctr < groupedRows.Count; ++ctr)
					newRows[ctr].Add(GetAggregateValue(tuple.Item2, groupedRows[ctr].Select(item => item[tuple.Item1]).ToList()));

				newHeaders.Add($"{Headers[tuple.Item1]}{(tuple.Item2 == AggregateType.None ? "" : $" ({tuple.Item2})")}");
			}

			return new Table(this)
			{
				Headers = newHeaders,
				Rows = newRows,
				HasHeaders = HasHeaders,
				TableType = TableType,
			};
		}

		static public Table Join(Table leftTable, Table rightTable, List<int> leftColumns, List<int> rightColumns, JoinType joinType)
		{
			if ((leftColumns.Count != rightColumns.Count))
				throw new ArgumentException("Column counts must match to join");

			var left = leftTable.Rows.GroupBy(row => leftColumns.Select(column => row[column] ?? new object()).ToItemSet()).ToDictionary(group => group.Key, group => group.ToList());
			var right = rightTable.Rows.GroupBy(row => rightColumns.Select(column => row[column] ?? new object()).ToItemSet()).ToDictionary(group => group.Key, group => group.ToList());

			List<ItemSet<object>> keys;
			switch (joinType)
			{
				case JoinType.Inner: keys = left.Keys.Where(key => right.ContainsKey(key)).ToList(); break;
				case JoinType.LeftOuter: keys = left.Keys.ToList(); break;
				case JoinType.RightOuter: keys = right.Keys.ToList(); break;
				case JoinType.FullOuter: keys = left.Keys.Concat(right.Keys).Distinct().ToList(); break;
				default: throw new ArgumentException("Invalid join");
			}

			var emptyLeft = new List<List<object>> { Enumerable.Range(0, leftTable.NumColumns).Select(column => default(object)).ToList() };
			var emptyRight = new List<List<object>> { Enumerable.Range(0, rightTable.NumColumns).Select(column => default(object)).ToList() };
			return new Table(leftTable)
			{
				Headers = leftTable.Headers.Concat(rightTable.Headers).ToList(),
				Rows = keys.SelectMany(key => (left.ContainsKey(key) ? left[key] : emptyLeft).SelectMany(leftValue => (right.ContainsKey(key) ? right[key] : emptyRight).Select(rightValue => leftValue.Concat(rightValue).ToList()))).ToList(),
				HasHeaders = leftTable.HasHeaders,
				TableType = leftTable.TableType,
			};
		}

		public object this[Cell cell]
		{
			get { return Rows[cell.Row][cell.Column]; }
			set { Rows[cell.Row][cell.Column] = value; }
		}

		public object this[int row, int column]
		{
			get { return Rows[row][column]; }
			set { Rows[row][column] = value; }
		}

		public void ChangeCells(List<Cell> cells, List<object> values)
		{
			if (cells.Count != values.Count)
				throw new ArgumentException("Cells and values counts must match");

			for (var ctr = 0; ctr < cells.Count; ++ctr)
				this[cells[ctr]] = values[ctr];
		}

		public string GetString(Cell cell) => (Rows[cell.Row][cell.Column] ?? "<NULL>").ToString();
		public string GetString(int row, int column) => (Rows[row][column] ?? "<NULL>").ToString();
		public void Sort(List<int> sortOrder) => Rows = sortOrder.Select(index => Rows[index]).ToList();
		public List<List<object>> GetRowData(List<int> rows) => rows.Select(row => Rows[row].ToList()).ToList();
		public List<List<object>> GetColumnData(List<int> ranges) => ranges.Select(column => Rows.Select(row => row[column]).ToList()).ToList();
		public string GetTableData(ObservableCollectionEx<CellRange> ranges) => String.Join("\r\n", ranges.Select(range => GetTableData(range)));
		public string GetTableData(CellRange range) => String.Join("\r\n", Enumerable.Range(range.MinRow, range.NumRows).Select(row => String.Join("\t", Enumerable.Range(range.MinColumn, range.NumColumns).Select(column => (this[row, column] ?? "<NULL>").ToString()))));

		public void DeleteRows(List<int> rows)
		{
			var rowsHash = new HashSet<int>(rows);
			Rows = Rows.Where((row, index) => !rowsHash.Contains(index)).ToList();
		}

		public void InsertRows(List<int> rows, List<List<object>> insertData, bool selected)
		{
			if (rows.Count != insertData.Count)
				throw new ArgumentException("Rows and data counts must match");

			for (var row = 0; row < rows.Count; ++row)
				Rows.Insert(rows[row] + row, insertData[row]);
		}

		public void DeleteColumns(List<int> columns)
		{
			var columnsHash = new HashSet<int>(columns);
			Headers = new List<string>(Headers.Where((header, index) => !columnsHash.Contains(index)));
			Rows = Rows.Select(row => row.Where((item, index) => !columnsHash.Contains(index)).ToList()).ToList();
		}

		public void InsertColumns(List<int> columns, List<string> headers, List<List<object>> insertData, bool selected)
		{
			if ((columns.Count != insertData.Count) || (columns.Count != headers.Count))
				throw new ArgumentException("Columns, data, and headers counts must match");

			for (var column = 0; column < columns.Count; ++column)
			{
				Headers.Insert(columns[column] + column, headers[column]);
				for (var row = 0; row < Rows.Count; ++row)
					Rows[row].Insert(columns[column] + column, insertData[column][row]);
			}
		}

		public void RenameHeader(int column, string newName) => Headers[column] = newName;
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
