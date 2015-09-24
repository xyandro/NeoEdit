using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using NeoEdit.Common.Parsing;
using NeoEdit.GUI.Controls;

namespace NeoEdit.Tables
{
	public class Table : DependencyObject
	{
		public class Header : DependencyObject
		{
			[DepProp]
			public string Name { get { return UIHelper<Header>.GetPropValue<string>(this); } set { UIHelper<Header>.SetPropValue(this, value); } }
			[DepProp]
			public Type Type { get { return UIHelper<Header>.GetPropValue<Type>(this); } set { UIHelper<Header>.SetPropValue(this, value); } }
		}

		public enum TableTypeEnum { None, TSV, CSV, Columns }

		[DepProp]
		public ObservableCollection<ObservableCollection<object>> Rows { get { return UIHelper<Table>.GetPropValue<ObservableCollection<ObservableCollection<object>>>(this); } set { UIHelper<Table>.SetPropValue(this, value); } }
		[DepProp]
		public ObservableCollection<Header> Headers { get { return UIHelper<Table>.GetPropValue<ObservableCollection<Header>>(this); } set { UIHelper<Table>.SetPropValue(this, value); } }
		[DepProp]
		public TableTypeEnum TableType { get { return UIHelper<Table>.GetPropValue<TableTypeEnum>(this); } set { UIHelper<Table>.SetPropValue(this, value); } }
		[DepProp]
		public bool HasHeaders { get { return UIHelper<Table>.GetPropValue<bool>(this); } set { UIHelper<Table>.SetPropValue(this, value); } }

		public Table()
		{
			Rows = new ObservableCollection<ObservableCollection<object>>();
			Headers = new ObservableCollection<Header>();
			TableType = TableTypeEnum.TSV;
			HasHeaders = false;
		}

		Table(Table table)
		{
			Rows = table.Rows;
			Headers = table.Headers;
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

		void ProcessInput(string table, TableTypeEnum tableType, int count = int.MaxValue)
		{
			switch (tableType)
			{
				case TableTypeEnum.TSV: Rows = new ObservableCollection<ObservableCollection<object>>(table.SplitTCSV('\t').Take(count)); break;
				case TableTypeEnum.CSV: Rows = new ObservableCollection<ObservableCollection<object>>(table.SplitTCSV(',').Take(count)); break;
				case TableTypeEnum.Columns: Rows = new ObservableCollection<ObservableCollection<object>>(table.SplitByLine().Select(line => new ObservableCollection<object>(line.Split('│').Select(item => item.TrimEnd() as object)))); break;
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
			Headers = new ObservableCollection<Header>();

			if ((!hasHeaders.HasValue) && ((!Rows.Any()) || (Rows[0].Any(item => item == null))))
				hasHeaders = false;

			var count = Rows.Any() ? Rows[0].Count : 0;
			for (var column = 0; column < count; ++column)
			{
				Headers.Add(new Header { Name = String.Format("Column {0}", column + 1) });
				var match = parsers.First(pair => Rows.Skip(hasHeaders == false ? 0 : 1).All(row => (row[column] == null) || (pair.Value((string)row[column]) != null)));
				if ((!hasHeaders.HasValue) && (match.Value((string)Rows[0][column]) == null))
					hasHeaders = true;
				Headers[column].Type = match.Key;
			}

			HasHeaders = hasHeaders == true;
			if (HasHeaders)
			{
				for (var column = 0; column < Headers.Count; ++column)
					Headers[column].Name = (string)Rows[0][column];
				Rows.RemoveAt(0);
			}

			foreach (var row in Rows)
				for (var column = 0; column < Headers.Count; ++column)
					row[column] = parsers[Headers[column].Type]((string)row[column]);
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

		static ObservableCollection<object> SplitTCSV(string source, char splitChar, ref int pos)
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
			return new ObservableCollection<object>(result);
		}

		static internal IEnumerable<ObservableCollection<object>> SplitTCSV(this string source, char splitChar)
		{
			var pos = 0;
			while (pos < source.Length)
				yield return SplitTCSV(source, splitChar, ref pos);
			yield break;
		}
	}
}
