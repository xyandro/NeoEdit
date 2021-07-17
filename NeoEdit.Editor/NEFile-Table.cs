using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NeoEdit.Common;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Enums;
using NeoEdit.Common.Expressions;
using NeoEdit.TaskRunning;

namespace NeoEdit.Editor
{
	partial class NEFile
	{
		static Table joinTable;

		static string GetDBValue(string value) => value?.IsNumeric() != false ? value ?? "NULL" : $"'{value.Replace("'", "''")}'";

		Table GetTable(bool hasHeaders = true)
		{
			if (ContentType.IsTableType())
				return new Table(Text.GetString(), ContentType, hasHeaders);
			if (ContentType == ParserType.None)
				return new Table(TaskRunner.Range(0, Text.NumLines).Select(line => Text.GetLine(line)).NonNullOrWhiteSpace().Select(str => new List<string> { str }).ToList(), false);
			throw new Exception("Invalid content type");
		}

		string GetTableText(Table table)
		{
			if (!ContentType.IsTableType())
				ContentType = ParserType.Columns;
			return table.ToString(Text.DefaultEnding, ContentType);
		}

		NEVariables GetTableVariables(Table table)
		{
			var results = GetVariables();
			for (var column = 0; column < table.NumColumns; ++column)
			{
				var col = column; // If we don't copy this the value will be updated and invalid
				var header = table.GetHeader(column);
				var colData = default(List<string>);
				var colDataInitialize = new RunOnceAction(() => colData = Enumerable.Range(0, table.NumRows).Select(row => table[row, col]).ToList());
				results.Add(NEVariable.List(header, $"Column {header}", () => { colDataInitialize.Invoke(); return colData; }));
			}
			return results;
		}

		void SetText(Table table)
		{
			var output = GetTableText(table);
			Replace(new List<NERange> { NERange.FromIndex(0, Text.Length) }, new List<string> { output });
			Selections = new List<NERange> { new NERange() };
		}

		static void Configure_Table_Select_RowsByExpression()
		{
			var table = state.NEWindow.Focused.GetTable();
			state.Configuration = state.NEWindow.neWindowUI.RunDialog_Configure_FileTable_Various_Various(state.NEWindow.Focused.GetTableVariables(table), table.NumRows);
		}

		void Execute_Table_Select_RowsByExpression()
		{
			var result = state.Configuration as Configuration_FileTable_Various_Various;
			var table = GetTable();
			var variables = GetTableVariables(table);
			var results = state.GetExpression(result.Expression).Evaluate<bool>(variables, table.NumRows);
			var lines = results.Indexes(res => res).Select(row => row + 1).ToList();
			Selections = lines.AsTaskRunner().Select(line => new NERange(Text.GetPosition(line, 0), Text.GetPosition(line, Text.GetLineLength(line)))).ToList();
		}

		static void Configure_Table_New_FromSelection()
		{
			if (state.NEWindow.Focused.Selections.Count != 1)
				throw new Exception("Must have one selection");
			if (!state.NEWindow.Focused.Selections[0].HasSelection)
				throw new Exception("Must have data selected");

			state.Configuration = state.NEWindow.neWindowUI.RunDialog_Configure_Table_New_FromSelection(state.NEWindow.Focused.GetSelectionStrings().Single());
		}

		void Execute_Table_New_FromSelection()
		{
			var result = state.Configuration as Configuration_Table_New_FromSelection;
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

		void Execute_Table_New_FromLineSelections()
		{
			if (!Selections.Any())
				return;

			var lineSets = Selections.AsTaskRunner().Select(range => new { start = Text.GetPositionLine(range.Start), end = Text.GetPositionLine(range.End) }).ToList();
			if (lineSets.Any(range => range.start != range.end))
				throw new Exception("Cannot have multi-line selections");

			var sels = GetSelectionStrings();
			var lines = lineSets.Select(range => range.start).ToList();
			var rows = Enumerable.Range(0, Selections.Count).GroupBy(index => lines[index]).Select(group => group.Select(index => sels[index]).ToList()).ToList();
			OpenTable(new Table(rows, false));
		}

		void Execute_Table_New_FromRegionSelections_Region(int useRegion)
		{
			if (!Selections.Any())
				return;

			var sels = GetSelectionStrings();
			var regions = GetEnclosingRegions(useRegion);
			var rows = Enumerable.Range(0, Selections.Count).GroupBy(index => regions[index]).Select(group => group.Select(index => sels[index]).ToList()).ToList();
			OpenTable(new Table(rows, false));
		}

		static void Configure_Table_Edit() => state.Configuration = state.NEWindow.neWindowUI.RunDialog_Configure_Table_Edit(state.NEWindow.Focused.GetTable());

		void Execute_Table_Edit()
		{
			var result = state.Configuration as Configuration_Table_Edit;
			SetText(GetTable().Aggregate(result.AggregateData).Sort(result.SortData));
		}

		void Execute_Table_DetectType() => ContentType = Table.GuessTableType(Text.GetString());

		static void Configure_Table_Convert() => state.Configuration = state.NEWindow.neWindowUI.RunDialog_Configure_Table_Convert(state.NEWindow.Focused.ContentType);

		void Execute_Table_Convert()
		{
			var result = state.Configuration as Configuration_Table_Convert;
			var table = GetTable();
			ContentType = result.TableType;
			SetText(table);
		}

		void Execute_Table_SetJoinSource() => joinTable = GetTable();

		static void Configure_Table_Join()
		{
			if (joinTable == null)
				throw new Exception("You must first set a join source.");

			state.Configuration = state.NEWindow.neWindowUI.RunDialog_Configure_Table_Join(state.NEWindow.Focused.GetTable(), joinTable);
		}

		void Execute_Table_Join()
		{
			var result = state.Configuration as Configuration_Table_Join;
			if (joinTable == null)
				throw new Exception("You must first set a join source.");

			SetText(Table.Join(GetTable(), joinTable, result.LeftColumns, result.RightColumns, result.JoinType));
		}

		void Execute_Table_Transpose() => SetText(GetTable().Transpose());

		static void Configure_Table_Database_GenerateInserts() => state.Configuration = state.NEWindow.neWindowUI.RunDialog_Configure_Table_Database_GenerateInserts(state.NEWindow.Focused.GetTable(), state.NEWindow.Focused.FileName == null ? "<TABLE>" : Path.GetFileNameWithoutExtension(state.NEWindow.Focused.FileName));

		void Execute_Table_Database_GenerateInserts()
		{
			var result = state.Configuration as Configuration_Table_Database_GenerateInserts;
			var table = GetTable();
			var header = $"INSERT INTO {result.TableName} ({string.Join(", ", Enumerable.Range(0, table.NumColumns).Select(column => table.GetHeader(column)))}) VALUES{(result.BatchSize == 1 ? " " : Text.DefaultEnding)}";
			var output = Enumerable.Range(0, table.NumRows).Batch(result.BatchSize).Select(batch => string.Join($",{Text.DefaultEnding}", batch.Select(row => $"({string.Join(", ", result.Columns.Select(column => GetDBValue(table[row, column])))})"))).Select(val => $"{header}{val}{Text.DefaultEnding}").ToList();
			Replace(new List<NERange> { NERange.FromIndex(0, Text.Length) }, new List<string> { string.Join("", output) });

			var index = 0;
			var sels = new List<NERange>();
			foreach (var item in output)
			{
				sels.Add(NERange.FromIndex(index, item.Length));
				index += item.Length;
			}
			Selections = sels;
		}

		static void Configure_Table_Database_GenerateUpdates() => state.Configuration = state.NEWindow.neWindowUI.RunDialog_Configure_Table_Database_GenerateUpdates(state.NEWindow.Focused.GetTable(), state.NEWindow.Focused.FileName == null ? "<TABLE>" : Path.GetFileNameWithoutExtension(state.NEWindow.Focused.FileName));

		void Execute_Table_Database_GenerateUpdates()
		{
			var result = state.Configuration as Configuration_Table_Database_GenerateUpdates;
			var table = GetTable();

			var output = Enumerable.Range(0, table.NumRows).Select(row => $"UPDATE {result.TableName} SET {string.Join(", ", result.Update.Select(column => $"{table.GetHeader(column)} = {GetDBValue(table[row, column])}"))} WHERE {string.Join(" AND ", result.Where.Select(column => $"{table.GetHeader(column)} = {GetDBValue(table[row, column])}"))}{Text.DefaultEnding}").ToList();
			Replace(new List<NERange> { NERange.FromIndex(0, Text.Length) }, new List<string> { string.Join("", output) });

			var index = 0;
			var sels = new List<NERange>();
			foreach (var item in output)
			{
				sels.Add(NERange.FromIndex(index, item.Length));
				index += item.Length;
			}
			Selections = sels;
		}

		static void Configure_Table_Database_GenerateDeletes() => state.Configuration = state.NEWindow.neWindowUI.RunDialog_Configure_Table_Database_GenerateDeletes(state.NEWindow.Focused.GetTable(), state.NEWindow.Focused.FileName == null ? "<TABLE>" : Path.GetFileNameWithoutExtension(state.NEWindow.Focused.FileName));

		void Execute_Table_Database_GenerateDeletes()
		{
			var result = state.Configuration as Configuration_Table_Database_GenerateDeletes;
			var table = GetTable();

			var output = Enumerable.Range(0, table.NumRows).Select(row => $"DELETE FROM {result.TableName} WHERE {string.Join(" AND ", result.Where.Select(column => $"{table.GetHeader(column)} = {GetDBValue(table[row, column])}"))}{Text.DefaultEnding}").ToList();
			Replace(new List<NERange> { NERange.FromIndex(0, Text.Length) }, new List<string> { string.Join("", output) });

			var index = 0;
			var sels = new List<NERange>();
			foreach (var item in output)
			{
				sels.Add(NERange.FromIndex(index, item.Length));
				index += item.Length;
			}
			Selections = sels;
		}
	}
}
