using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NeoEdit.Common;
using NeoEdit.Common.Expressions;
using NeoEdit.Common.Transform;
using NeoEdit.GUI.Dialogs;
using NeoEdit.TextEdit.Content;
using NeoEdit.TextEdit.Dialogs;

namespace NeoEdit.TextEdit
{
	partial class TextEditor
	{
		static Table joinTable;

		string GetDBValue(string value) => value?.IsNumeric() != false ? value ?? "NULL" : $"'{value.Replace("'", "''")}'";

		Table GetTable(bool hasHeaders = true)
		{
			if (ContentType.IsTableType())
				return new Table(AllText, ContentType, hasHeaders);
			if (ContentType == Parser.ParserType.None)
				return new Table(Enumerable.Range(0, Data.NumLines).AsParallel().AsOrdered().Select(line => Data.GetLine(line)).NonNullOrWhiteSpace().Select(str => new List<string> { str }).ToList(), false);
			throw new Exception("Invalid content type");
		}

		string GetTableText(Table table)
		{
			if (!ContentType.IsTableType())
				ContentType = Parser.ParserType.Columns;
			return table.ToString(Data.DefaultEnding, ContentType);
		}

		NEVariables GetTableVariables(Table table)
		{
			var results = GetVariables();
			for (var column = 0; column < table.NumColumns; ++column)
			{
				var col = column; // If we don't copy this the value will be updated and invalid
				var header = table.GetHeader(column);
				var colData = default(List<string>);
				var colDataInitialize = new NEVariableListInitializer(() => colData = Enumerable.Range(0, table.NumRows).Select(row => table[row, col]).ToList());
				results.Add(NEVariable.List(header, $"Column {header}", () => colData, colDataInitialize));
			}
			return results;
		}

		void OpenTable(Table table, string name = null)
		{
			var contentType = ContentType.IsTableType() ? ContentType : Parser.ParserType.Columns;
			var textEditor = new TextEditor(bytes: Coder.StringToBytes(table.ToString("\r\n", contentType), Coder.CodePage.UTF8), codePage: Coder.CodePage.UTF8, modified: false);
			textEditor.ContentType = contentType;
			textEditor.DisplayName = name;
			TabsParent.CreateTab(textEditor);
		}

		void SetText(Table table)
		{
			var output = GetTableText(table);
			Replace(new List<Range> { FullRange }, new List<string> { output });
			Selections.Replace(BeginRange);
		}

		void Command_Table_Type_Detect() => ContentType = Table.GuessTableType(AllText);

		ChooseTableTypeDialog.Result Command_Table_Convert_Dialog() => ChooseTableTypeDialog.Run(WindowParent, ContentType);

		void Command_Table_Convert(ChooseTableTypeDialog.Result result)
		{
			var table = GetTable();
			ContentType = result.TableType;
			SetText(table);
		}

		TextToTableDialog.Result Command_Table_TextToTable_Dialog()
		{
			if (Selections.Count != 1)
				throw new Exception("Must have one selection");
			if (!Selections[0].HasSelection)
				throw new Exception("Must have data selected");

			return TextToTableDialog.Run(WindowParent, GetSelectionStrings().Single());
		}

		void Command_Table_TextToTable(TextToTableDialog.Result result)
		{
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

		void Command_Table_LineSelectionsToTable()
		{
			if (!Selections.Any())
				return;

			var lineSets = Selections.AsParallel().AsOrdered().Select(range => new { start = Data.GetOffsetLine(range.Start), end = Data.GetOffsetLine(range.End) }).ToList();
			if (lineSets.Any(range => range.start != range.end))
				throw new Exception("Cannot have multi-line selections");

			var sels = GetSelectionStrings();
			var lines = lineSets.Select(range => range.start).ToList();
			var rows = Enumerable.Range(0, Selections.Count).GroupBy(index => lines[index]).Select(group => group.Select(index => sels[index]).ToList()).ToList();
			OpenTable(new Table(rows, false));
		}

		void Command_Table_RegionSelectionsToTable()
		{
			if (!Selections.Any())
				return;

			var sels = GetSelectionStrings();
			var regions = GetEnclosingRegions();
			var rows = Enumerable.Range(0, Selections.Count).GroupBy(index => regions[index]).Select(group => group.Select(index => sels[index]).ToList()).ToList();
			OpenTable(new Table(rows, false));
		}

		EditTableDialog.Result Command_Table_EditTable_Dialog() => EditTableDialog.Run(WindowParent, GetTable());

		void Command_Table_EditTable(EditTableDialog.Result result) => SetText(GetTable().Aggregate(result.AggregateData).Sort(result.SortData));

		AddColumnDialog.Result Command_Table_AddColumn_Dialog()
		{
			var table = GetTable();
			return AddColumnDialog.Run(WindowParent, GetTableVariables(table), table.NumRows);
		}

		void Command_Table_AddHeaders() => SetText(GetTable(false));

		void Command_Table_AddRow()
		{
			var table = GetTable();
			table.AddRow();
			SetText(table);
		}

		void Command_Table_AddColumn(AddColumnDialog.Result result)
		{
			var table = GetTable();
			var variables = GetTableVariables(table);
			var results = new NEExpression(result.Expression).EvaluateRows<string>(variables, table.NumRows);
			table.AddColumn(result.ColumnName, results);
			SetText(table);
		}

		GetExpressionDialog.Result Command_Table_Select_RowsByExpression_Dialog()
		{
			var table = GetTable();
			return GetExpressionDialog.Run(WindowParent, GetTableVariables(table), table.NumRows);
		}

		void Command_Table_Select_RowsByExpression(GetExpressionDialog.Result result)
		{
			var table = GetTable();
			var variables = GetTableVariables(table);
			var results = new NEExpression(result.Expression).EvaluateRows<bool>(variables, table.NumRows);
			var lines = results.Indexes(res => res).Select(row => row + 1).ToList();
			Selections.Replace(lines.AsParallel().AsOrdered().Select(line => new Range(Data.GetOffset(line, Data.GetLineLength(line)), Data.GetOffset(line, 0))).ToList());
		}

		void Command_Table_SetJoinSource() => joinTable = GetTable();

		JoinDialog.Result Command_Table_Join_Dialog()
		{
			if (joinTable == null)
				throw new Exception("You must first set a join source.");

			return JoinDialog.Run(WindowParent, GetTable(), joinTable);
		}

		void Command_Table_Join(JoinDialog.Result result)
		{
			if (joinTable == null)
				throw new Exception("You must first set a join source.");

			SetText(Table.Join(GetTable(), joinTable, result.LeftColumns, result.RightColumns, result.JoinType));
		}

		void Command_Table_Transpose() => SetText(GetTable().Transpose());

		void Command_Table_SetVariables()
		{
			var table = GetTable();
			for (var column = 0; column < table.NumColumns; ++column)
				variables[table.GetHeader(column)] = Enumerable.Range(0, table.NumRows).Select(row => table[row, column]).ToList();
		}

		GenerateInsertsDialog.Result Command_Table_Database_GenerateInserts_Dialog() => GenerateInsertsDialog.Run(WindowParent, GetTable(), FileName == null ? "<TABLE>" : Path.GetFileNameWithoutExtension(FileName));

		void Command_Table_Database_GenerateInserts(GenerateInsertsDialog.Result result)
		{
			var table = GetTable();
			var header = $"INSERT INTO {result.TableName} ({string.Join(", ", Enumerable.Range(0, table.NumColumns).Select(column => table.GetHeader(column)))}) VALUES{(result.BatchSize == 1 ? " " : Data.DefaultEnding)}";
			var output = Enumerable.Range(0, table.NumRows).Batch(result.BatchSize).Select(batch => string.Join($",{Data.DefaultEnding}", batch.Select(row => $"({string.Join(", ", result.Columns.Select(column => GetDBValue(table[row, column])))})"))).Select(val => $"{header}{val}{Data.DefaultEnding}").ToList();
			Replace(new List<Range> { FullRange }, new List<string> { string.Join("", output) });

			var offset = 0;
			var sels = new List<Range>();
			foreach (var item in output)
			{
				sels.Add(Range.FromIndex(offset, item.Length));
				offset += item.Length;
			}
			Selections.Replace(sels);
		}

		GenerateUpdatesDialog.Result Command_Table_Database_GenerateUpdates_Dialog() => GenerateUpdatesDialog.Run(WindowParent, GetTable(), FileName == null ? "<TABLE>" : Path.GetFileNameWithoutExtension(FileName));

		void Command_Table_Database_GenerateUpdates(GenerateUpdatesDialog.Result result)
		{
			var table = GetTable();

			var output = Enumerable.Range(0, table.NumRows).Select(row => $"UPDATE {result.TableName} SET {string.Join(", ", result.Update.Select(column => $"{table.GetHeader(column)} = {GetDBValue(table[row, column])}"))} WHERE {string.Join(" AND ", result.Where.Select(column => $"{table.GetHeader(column)} = {GetDBValue(table[row, column])}"))}{Data.DefaultEnding}").ToList();
			Replace(new List<Range> { FullRange }, new List<string> { string.Join("", output) });

			var offset = 0;
			var sels = new List<Range>();
			foreach (var item in output)
			{
				sels.Add(Range.FromIndex(offset, item.Length));
				offset += item.Length;
			}
			Selections.Replace(sels);
		}

		GenerateDeletesDialog.Result Command_Table_Database_GenerateDeletes_Dialog() => GenerateDeletesDialog.Run(WindowParent, GetTable(), FileName == null ? "<TABLE>" : Path.GetFileNameWithoutExtension(FileName));

		void Command_Table_Database_GenerateDeletes(GenerateDeletesDialog.Result result)
		{
			var table = GetTable();

			var output = Enumerable.Range(0, table.NumRows).Select(row => $"DELETE FROM {result.TableName} WHERE {string.Join(" AND ", result.Where.Select(column => $"{table.GetHeader(column)} = {GetDBValue(table[row, column])}"))}{Data.DefaultEnding}").ToList();
			Replace(new List<Range> { FullRange }, new List<string> { string.Join("", output) });

			var offset = 0;
			var sels = new List<Range>();
			foreach (var item in output)
			{
				sels.Add(Range.FromIndex(offset, item.Length));
				offset += item.Length;
			}
			Selections.Replace(sels);
		}
	}
}
