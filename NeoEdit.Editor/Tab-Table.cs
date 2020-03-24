using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NeoEdit.Common;
using NeoEdit.Common.Enums;
using NeoEdit.Common.Expressions;
using NeoEdit.Common.Models;

namespace NeoEdit.Editor
{
	partial class Tab
	{
		static Table joinTable;

		static string GetDBValue(string value) => value?.IsNumeric() != false ? value ?? "NULL" : $"'{value.Replace("'", "''")}'";

		Table GetTable(bool hasHeaders = true)
		{
			if (ContentType.IsTableType())
				return new Table(Text.GetString(), ContentType, hasHeaders);
			if (ContentType == ParserType.None)
				return new Table(Enumerable.Range(0, TextView.NumLines).AsParallel().AsOrdered().Select(line => Text.GetString(TextView.GetLine(line))).NonNullOrWhiteSpace().Select(str => new List<string> { str }).ToList(), false);
			throw new Exception("Invalid content type");
		}

		string GetTableText(Table table)
		{
			if (!ContentType.IsTableType())
				ContentType = ParserType.Columns;
			return table.ToString(TextView.DefaultEnding, ContentType);
		}

		NEVariables GetTableVariables(Table table)
		{
			var results = GetVariables();
			for (var column = 0; column < table.NumColumns; ++column)
			{
				var col = column; // If we don't copy this the value will be updated and invalid
				var header = table.GetHeader(column);
				var colData = default(List<string>);
				var colDataInitialize = new NEVariableInitializer(() => colData = Enumerable.Range(0, table.NumRows).Select(row => table[row, col]).ToList());
				results.Add(NEVariable.List(header, $"Column {header}", () => colData, colDataInitialize));
			}
			return results;
		}

		void SetText(Table table)
		{
			var output = GetTableText(table);
			Replace(new List<Range> { Range.FromIndex(0, Text.Length) }, new List<string> { output });
			Selections = new List<Range> { new Range() };
		}

		void Execute_Table_DetectType() => ContentType = Table.GuessTableType(Text.GetString());

		object Configure_Table_Convert() => state.TabsWindow.RunTableConvertDialog(ContentType);

		void Execute_Table_Convert()
		{
			var result = state.Configuration as TableConvertDialogResult;
			var table = GetTable();
			ContentType = result.TableType;
			SetText(table);
		}

		object Configure_Table_TextToTable()
		{
			if (Selections.Count != 1)
				throw new Exception("Must have one selection");
			if (!Selections[0].HasSelection)
				throw new Exception("Must have data selected");

			return state.TabsWindow.RunTableTextToTableDialog(GetSelectionStrings().Single());
		}

		void Execute_Table_TextToTable()
		{
			var result = state.Configuration as TableTextToTableDialogResult;
			if (Selections.Count != 1)
				throw new Exception("Must have one selection");
			if (!Selections[0].HasSelection)
				throw new Exception("Must have data selected");

			var columns = new List<Tuple<int, int>>();
			for (var ctr = 0; ctr < result.LineBreaks.Count - 1; ++ctr)
				columns.Add(Tuple.Create(result.LineBreaks[ctr], result.LineBreaks[ctr + 1]));
			var rows = GetSelectionStrings().Single().Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None).NonNullOrEmpty().Select(line => columns.Select(col => line.Substring(Math.Min(line.Length, col.Item1), Math.Min(line.Length, col.Item2) - Math.Min(line.Length, col.Item1)).Trim()).ToList()).ToList();
			OpenTable(new Table(rows));
		}

		void Execute_Table_LineSelectionsToTable()
		{
			if (!Selections.Any())
				return;

			var lineSets = Selections.AsParallel().AsOrdered().Select(range => new { start = TextView.GetPositionLine(range.Start), end = TextView.GetPositionLine(range.End) }).ToList();
			if (lineSets.Any(range => range.start != range.end))
				throw new Exception("Cannot have multi-line selections");

			var sels = GetSelectionStrings();
			var lines = lineSets.Select(range => range.start).ToList();
			var rows = Enumerable.Range(0, Selections.Count).GroupBy(index => lines[index]).Select(group => group.Select(index => sels[index]).ToList()).ToList();
			OpenTable(new Table(rows, false));
		}

		void Execute_Table_RegionSelectionsToTable_Region(int useRegion)
		{
			if (!Selections.Any())
				return;

			var sels = GetSelectionStrings();
			var regions = GetEnclosingRegions(useRegion);
			var rows = Enumerable.Range(0, Selections.Count).GroupBy(index => regions[index]).Select(group => group.Select(index => sels[index]).ToList()).ToList();
			OpenTable(new Table(rows, false));
		}

		object Configure_Table_EditTable() => state.TabsWindow.RunTableEditTableDialog(GetTable());

		void Execute_Table_EditTable()
		{
			var result = state.Configuration as TableEditTableDialogResult;
			SetText(GetTable().Aggregate(result.AggregateData).Sort(result.SortData));
		}

		void Execute_Table_AddHeaders() => SetText(GetTable(false));

		void Execute_Table_AddRow()
		{
			var table = GetTable();
			table.AddRow();
			SetText(table);
		}

		object Configure_Table_AddColumn()
		{
			var table = GetTable();
			return state.TabsWindow.RunTableAddColumnDialog(GetTableVariables(table), table.NumRows);
		}

		void Execute_Table_AddColumn()
		{
			var result = state.Configuration as TableAddColumnDialogResult;
			var table = GetTable();
			var variables = GetTableVariables(table);
			var results = state.GetExpression(result.Expression).EvaluateList<string>(variables, table.NumRows);
			table.AddColumn(result.ColumnName, results);
			SetText(table);
		}

		object Configure_Table_Select_RowsByExpression()
		{
			var table = GetTable();
			return state.TabsWindow.RunGetExpressionDialog(GetTableVariables(table), table.NumRows);
		}

		void Execute_Table_Select_RowsByExpression()
		{
			var result = state.Configuration as GetExpressionDialogResult;
			var table = GetTable();
			var variables = GetTableVariables(table);
			var results = state.GetExpression(result.Expression).EvaluateList<bool>(variables, table.NumRows);
			var lines = results.Indexes(res => res).Select(row => row + 1).ToList();
			Selections = lines.AsParallel().AsOrdered().Select(line => new Range(TextView.GetPosition(line, TextView.GetLineLength(line)), TextView.GetPosition(line, 0))).ToList();
		}

		void Execute_Table_SetJoinSource() => joinTable = GetTable();

		object Configure_Table_Join()
		{
			if (joinTable == null)
				throw new Exception("You must first set a join source.");

			return state.TabsWindow.RunTableJoinDialog(GetTable(), joinTable);
		}

		void Execute_Table_Join()
		{
			var result = state.Configuration as TableJoinDialogResult;
			if (joinTable == null)
				throw new Exception("You must first set a join source.");

			SetText(Table.Join(GetTable(), joinTable, result.LeftColumns, result.RightColumns, result.JoinType));
		}

		void Execute_Table_Transpose() => SetText(GetTable().Transpose());

		object Configure_Table_Database_GenerateInserts() => state.TabsWindow.RunTableDatabaseGenerateInsertsDialog(GetTable(), FileName == null ? "<TABLE>" : Path.GetFileNameWithoutExtension(FileName));

		void Execute_Table_Database_GenerateInserts()
		{
			var result = state.Configuration as TableDatabaseGenerateInsertsDialogResult;
			var table = GetTable();
			var header = $"INSERT INTO {result.TableName} ({string.Join(", ", Enumerable.Range(0, table.NumColumns).Select(column => table.GetHeader(column)))}) VALUES{(result.BatchSize == 1 ? " " : TextView.DefaultEnding)}";
			var output = Enumerable.Range(0, table.NumRows).Batch(result.BatchSize).Select(batch => string.Join($",{TextView.DefaultEnding}", batch.Select(row => $"({string.Join(", ", result.Columns.Select(column => GetDBValue(table[row, column])))})"))).Select(val => $"{header}{val}{TextView.DefaultEnding}").ToList();
			Replace(new List<Range> { Range.FromIndex(0, Text.Length) }, new List<string> { string.Join("", output) });

			var index = 0;
			var sels = new List<Range>();
			foreach (var item in output)
			{
				sels.Add(Range.FromIndex(index, item.Length));
				index += item.Length;
			}
			Selections = sels;
		}

		object Configure_Table_Database_GenerateUpdates() => state.TabsWindow.RunTableDatabaseGenerateUpdatesDialog(GetTable(), FileName == null ? "<TABLE>" : Path.GetFileNameWithoutExtension(FileName));

		void Execute_Table_Database_GenerateUpdates()
		{
			var result = state.Configuration as TableDatabaseGenerateUpdatesDialogResult;
			var table = GetTable();

			var output = Enumerable.Range(0, table.NumRows).Select(row => $"UPDATE {result.TableName} SET {string.Join(", ", result.Update.Select(column => $"{table.GetHeader(column)} = {GetDBValue(table[row, column])}"))} WHERE {string.Join(" AND ", result.Where.Select(column => $"{table.GetHeader(column)} = {GetDBValue(table[row, column])}"))}{TextView.DefaultEnding}").ToList();
			Replace(new List<Range> { Range.FromIndex(0, Text.Length) }, new List<string> { string.Join("", output) });

			var index = 0;
			var sels = new List<Range>();
			foreach (var item in output)
			{
				sels.Add(Range.FromIndex(index, item.Length));
				index += item.Length;
			}
			Selections = sels;
		}

		object Configure_Table_Database_GenerateDeletes() => state.TabsWindow.RunTableDatabaseGenerateDeletesDialog(GetTable(), FileName == null ? "<TABLE>" : Path.GetFileNameWithoutExtension(FileName));

		void Execute_Table_Database_GenerateDeletes()
		{
			var result = state.Configuration as TableDatabaseGenerateDeletesDialogResult;
			var table = GetTable();

			var output = Enumerable.Range(0, table.NumRows).Select(row => $"DELETE FROM {result.TableName} WHERE {string.Join(" AND ", result.Where.Select(column => $"{table.GetHeader(column)} = {GetDBValue(table[row, column])}"))}{TextView.DefaultEnding}").ToList();
			Replace(new List<Range> { Range.FromIndex(0, Text.Length) }, new List<string> { string.Join("", output) });

			var index = 0;
			var sels = new List<Range>();
			foreach (var item in output)
			{
				sels.Add(Range.FromIndex(index, item.Length));
				index += item.Length;
			}
			Selections = sels;
		}
	}
}
