using System;
using System.Collections.Generic;
using System.Linq;

namespace NeoEdit.Tables
{
	public class Table
	{
		public class Header
		{
			public string Name { get; set; }
			public Type Type { get; set; }
			public double Width { get; set; }

			public Header Copy()
			{
				return new Header
				{
					Name = Name,
					Type = Type,
					Width = Width,
				};
			}
		}

		public enum TableTypeEnum { None, TSV, CSV, Columns }

		List<List<object>> Rows { get; set; }
		List<List<bool>> Selected { get; set; }
		public List<Header> Headers { get; private set; }
		public TableTypeEnum TableType { get; set; }
		public bool HasHeaders { get; set; }

		public int NumRows { get { return Rows.Count; } }
		public int NumColumns { get { return Headers.Count; } }

		public Table()
		{
			Rows = new List<List<object>>();
			Selected = new List<List<bool>>();
			Headers = new List<Header>();
			TableType = TableTypeEnum.TSV;
			HasHeaders = false;
		}

		Table(Table table)
		{
			Rows = table.Rows.Select(row => row.ToList()).ToList();
			Selected = table.Selected.Select(row => row.ToList()).ToList();
			Headers = table.Headers.Select(header => header.Copy()).ToList();
			TableType = table.TableType;
			HasHeaders = table.HasHeaders;
		}

		public Table(string input, TableTypeEnum tableType = TableTypeEnum.None, bool? hasHeaders = null)
		{
			if (tableType == TableTypeEnum.None)
				tableType = GuessTableType(input);
			ProcessInput(input, tableType);
			SetHeadersAndTypes(hasHeaders);
			Selected = Rows.Select(row => row.Select(item => false).ToList()).ToList();
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
		void SetHeadersAndTypes(bool? hasHeaders)
		{
			Headers = new List<Header>();

			if ((!hasHeaders.HasValue) && ((!Rows.Any()) || (Rows[0].Any(item => item == null))))
				hasHeaders = false;

			var count = Rows.Any() ? Rows[0].Count : 0;
			for (var column = 0; column < count; ++column)
			{
				Headers.Add(new Header { Name = String.Format("Column {0}", column + 1) });
				var match = parsers.First(pair => Rows.Skip(hasHeaders == false ? 0 : 1).All(row => (row[column] == null) || (pair.Value((string)row[column]) != null)));
				if ((!hasHeaders.HasValue) && (match.Value((string)this[0, column]) == null))
					hasHeaders = true;
				Headers[column].Type = match.Key;
			}

			HasHeaders = hasHeaders == true;
			if (HasHeaders)
			{
				for (var column = 0; column < Headers.Count; ++column)
					Headers[column].Name = (string)this[0, column];
				Rows.RemoveAt(0);
			}

			foreach (var row in Rows)
				for (var column = 0; column < Headers.Count; ++column)
					row[column] = parsers[Headers[column].Type]((string)row[column]);
		}

		public List<int> GetSortOrder(List<int> columns)
		{
			var order = Enumerable.Range(0, Rows.Count).ToList();
			var ordering = order.OrderBy(rowIndex => 0);
			foreach (var column in columns)
				ordering = ordering.ThenBy(rowIndex => this[rowIndex, column]);
			return ordering.ToList();
		}

		public int GetRowIndex(List<object> row)
		{
			return Rows.IndexOf(row);
		}

		public static string ToTCSV(string str, char split)
		{
			if (str.IndexOfAny(new char[] { split, '"' }) != -1)
				return String.Format("\"{0}\"", str.Replace("\"", "\"\""));
			return str;
		}

		public string ConvertToString(string ending, TableTypeEnum tableType = TableTypeEnum.TSV, bool hasHeaders = true)
		{
			var result = new List<List<string>>();
			if (hasHeaders)
				result.Add(Headers.Select(header => header.Name).ToList());
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

		public object this[CellLocation cell]
		{
			get { return Rows[cell.Row][cell.Column]; }
			set { Rows[cell.Row][cell.Column] = value; }
		}

		public object this[int row, int column]
		{
			get { return Rows[row][column]; }
			set { Rows[row][column] = value; }
		}

		public bool GetSelected(CellLocation cell)
		{
			return Selected[cell.Row][cell.Column];
		}

		public void SetSelected(CellLocation cell, bool value)
		{
			Selected[cell.Row][cell.Column] = value;
		}

		public bool GetSelected(int row, int column)
		{
			return Selected[row][column];
		}

		public void SetSelected(int row, int column, bool value)
		{
			Selected[row][column] = value;
		}

		public void ChangeCells(CellRanges ranges, List<object> values)
		{
			var cells = ranges.GetCells(NumRows, NumColumns).ToList();
			if (cells.Count != values.Count)
				throw new ArgumentException("Cells and values counts must match");

			for (var ctr = 0; ctr < cells.Count; ++ctr)
				this[cells[ctr]] = values[ctr];
		}

		public void Sort(List<int> sortOrder)
		{
			Rows = sortOrder.Select(index => Rows[index]).ToList();
			Selected = sortOrder.Select(index => Selected[index]).ToList();
		}

		public List<List<object>> GetRowData(List<int> rows)
		{
			return rows.Select(row => Rows[row].ToList()).ToList();
		}

		public List<List<object>> GetColumnData(List<int> columns)
		{
			return columns.Select(column => Rows.Select(row => row[column]).ToList()).ToList();
		}

		public void DeleteRows(List<int> rows)
		{
			var rowsHash = new HashSet<int>(rows);
			Rows = Rows.Where((row, index) => !rowsHash.Contains(index)).ToList();
			Selected = Selected.Where((row, index) => !rowsHash.Contains(index)).ToList();
		}

		public void InsertRows(List<int> rows, List<List<object>> insertData, bool selected)
		{
			if (rows.Count != insertData.Count)
				throw new ArgumentException("Rows and data counts must match");

			for (var ctr = 0; ctr < rows.Count; ++ctr)
			{
				Rows.Insert(rows[ctr] + ctr, insertData[ctr]);
				Selected.Insert(rows[ctr] + ctr, Enumerable.Repeat(selected, NumColumns).ToList());
			}
		}

		public void DeleteColumns(List<int> columns)
		{
			var columnsHash = new HashSet<int>(columns);
			Headers = new List<Header>(Headers.Where((header, index) => !columnsHash.Contains(index)));
			Rows = Rows.Select(row => row.Where((item, index) => !columnsHash.Contains(index)).ToList()).ToList();
			Selected = Selected.Select(row => row.Where((item, index) => !columnsHash.Contains(index)).ToList()).ToList();
		}

		public void InsertColumns(List<int> columns, List<Table.Header> headers, List<List<object>> insertData, bool selected)
		{
			if ((columns.Count != insertData.Count) || (columns.Count != headers.Count))
				throw new ArgumentException("Columns, data, and headers counts must match");

			for (var column = 0; column < columns.Count; ++column)
			{
				Headers.Insert(columns[column] + column, headers[column]);
				for (var row = 0; row < Rows.Count; ++row)
				{
					Rows[row].Insert(columns[column] + column, insertData[column][row]);
					Selected[row].Insert(columns[column] + column, selected);
				}
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
