using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NeoEdit;
using NeoEdit.Content;
using NeoEdit.Dialogs;
using NeoEdit.Expressions;
using NeoEdit.Transform;

namespace NeoEdit
{
	partial class TextEditor
	{
		static Table joinTable;

		string GetDBValue(string value) => value?.IsNumeric() != false ? value ?? "NULL" : $"'{value.Replace("'", "''")}'";

		Table GetTable(ITextEditor te, bool hasHeaders = true)
		{
			if (te.ContentType.IsTableType())
				return new Table(AllText, te.ContentType, hasHeaders);
			if (te.ContentType == Parser.ParserType.None)
				return new Table(Enumerable.Range(0, te.Data.NumLines).AsParallel().AsOrdered().Select(line => te.Data.GetLine(line)).NonNullOrWhiteSpace().Select(str => new List<string> { str }).ToList(), false);
			throw new Exception("Invalid content type");
		}

		string GetTableText(ITextEditor te, Table table)
		{
			if (!te.ContentType.IsTableType())
				te.ContentType = Parser.ParserType.Columns;
			return table.ToString(te.Data.DefaultEnding, te.ContentType);
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

		public void OpenTable(ITextEditor te, Table table, string name = null)
		{
			var contentType = te.ContentType.IsTableType() ? te.ContentType : Parser.ParserType.Columns;
			var textEditor = new TextEditor(bytes: Coder.StringToBytes(table.ToString("\r\n", contentType), Coder.CodePage.UTF8), codePage: Coder.CodePage.UTF8, modified: false);
			textEditor.ContentType = contentType;
			textEditor.DisplayName = name;
			te.TabsParent.Add(textEditor);
		}

		void SetText(ITextEditor te, Table table)
		{
			var output = GetTableText(te, table);
			te.Replace(new List<Range> { te.FullRange }, new List<string> { output });
			te.SetSelections(new List<Range> { BeginRange });
		}

		void Command_Table_DetectType(ITextEditor te) => te.ContentType = Table.GuessTableType(AllText);

		TableConvertDialog.Result Command_Table_Convert_Dialog(ITextEditor te) => TableConvertDialog.Run(te.TabsParent, te.ContentType);

		void Command_Table_Convert(ITextEditor te, TableConvertDialog.Result result)
		{
			var table = GetTable(te);
			te.ContentType = result.TableType;
			SetText(te, table);
		}

		TableTextToTableDialog.Result Command_Table_TextToTable_Dialog(ITextEditor te)
		{
			if (te.Selections.Count != 1)
				throw new Exception("Must have one selection");
			if (!te.Selections[0].HasSelection)
				throw new Exception("Must have data selected");

			return TableTextToTableDialog.Run(te.TabsParent, te.GetSelectionStrings().Single());
		}

		void Command_Table_TextToTable(ITextEditor te, TableTextToTableDialog.Result result)
		{
			if (te.Selections.Count != 1)
				throw new Exception("Must have one selection");
			if (!te.Selections[0].HasSelection)
				throw new Exception("Must have data selected");

			var columns = new List<Tuple<int, int>>();
			for (var ctr = 0; ctr < result.LineBreaks.Count - 1; ++ctr)
				columns.Add(Tuple.Create(result.LineBreaks[ctr], result.LineBreaks[ctr + 1]));
			var rows = te.GetSelectionStrings().Single().Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None).NonNullOrEmpty().Select(line => columns.Select(col => line.Substring(Math.Min(line.Length, col.Item1), Math.Min(line.Length, col.Item2) - Math.Min(line.Length, col.Item1)).Trim()).ToList()).ToList();
			te.OpenTable(te, new Table(rows));
		}

		void Command_Table_LineSelectionsToTable(ITextEditor te)
		{
			if (!te.Selections.Any())
				return;

			var lineSets = te.Selections.AsParallel().AsOrdered().Select(range => new { start = te.Data.GetOffsetLine(range.Start), end = te.Data.GetOffsetLine(range.End) }).ToList();
			if (lineSets.Any(range => range.start != range.end))
				throw new Exception("Cannot have multi-line selections");

			var sels = te.GetSelectionStrings();
			var lines = lineSets.Select(range => range.start).ToList();
			var rows = Enumerable.Range(0, te.Selections.Count).GroupBy(index => lines[index]).Select(group => group.Select(index => sels[index]).ToList()).ToList();
			te.OpenTable(te, new Table(rows, false));
		}

		void Command_Table_RegionSelectionsToTable_Region(ITextEditor te, int useRegion)
		{
			if (!te.Selections.Any())
				return;

			var sels = te.GetSelectionStrings();
			var regions = GetEnclosingRegions(te, useRegion);
			var rows = Enumerable.Range(0, te.Selections.Count).GroupBy(index => regions[index]).Select(group => group.Select(index => sels[index]).ToList()).ToList();
			te.OpenTable(te, new Table(rows, false));
		}

		TableEditTableDialog.Result Command_Table_EditTable_Dialog(ITextEditor te) => TableEditTableDialog.Run(te.TabsParent, GetTable(te));

		void Command_Table_EditTable(ITextEditor te, TableEditTableDialog.Result result) => SetText(te, GetTable(te).Aggregate(result.AggregateData).Sort(result.SortData));

		void Command_Table_AddHeaders(ITextEditor te) => SetText(te, GetTable(te, false));

		void Command_Table_AddRow(ITextEditor te)
		{
			var table = GetTable(te);
			table.AddRow();
			SetText(te, table);
		}

		TableAddColumnDialog.Result Command_Table_AddColumn_Dialog(ITextEditor te)
		{
			var table = GetTable(te);
			return TableAddColumnDialog.Run(te.TabsParent, GetTableVariables(table), table.NumRows);
		}

		void Command_Table_AddColumn(ITextEditor te, TableAddColumnDialog.Result result)
		{
			var table = GetTable(te);
			var variables = GetTableVariables(table);
			var results = new NEExpression(result.Expression).EvaluateList<string>(variables, table.NumRows);
			table.AddColumn(result.ColumnName, results);
			SetText(te, table);
		}

		GetExpressionDialog.Result Command_Table_Select_RowsByExpression_Dialog(ITextEditor te)
		{
			var table = GetTable(te);
			return GetExpressionDialog.Run(te.TabsParent, GetTableVariables(table), table.NumRows);
		}

		void Command_Table_Select_RowsByExpression(ITextEditor te, GetExpressionDialog.Result result)
		{
			var table = GetTable(te);
			var variables = GetTableVariables(table);
			var results = new NEExpression(result.Expression).EvaluateList<bool>(variables, table.NumRows);
			var lines = results.Indexes(res => res).Select(row => row + 1).ToList();
			te.SetSelections(lines.AsParallel().AsOrdered().Select(line => new Range(te.Data.GetOffset(line, te.Data.GetLineLength(line)), te.Data.GetOffset(line, 0))).ToList());
		}

		void Command_Table_SetJoinSource(ITextEditor te) => joinTable = GetTable(te);

		TableJoinDialog.Result Command_Table_Join_Dialog(ITextEditor te)
		{
			if (joinTable == null)
				throw new Exception("You must first set a join source.");

			return TableJoinDialog.Run(te.TabsParent, GetTable(te), joinTable);
		}

		void Command_Table_Join(ITextEditor te, TableJoinDialog.Result result)
		{
			if (joinTable == null)
				throw new Exception("You must first set a join source.");

			SetText(te, Table.Join(GetTable(te), joinTable, result.LeftColumns, result.RightColumns, result.JoinType));
		}

		void Command_Table_Transpose(ITextEditor te) => SetText(te, GetTable(te).Transpose());

		TableDatabaseGenerateInsertsDialog.Result Command_Table_Database_GenerateInserts_Dialog(ITextEditor te) => TableDatabaseGenerateInsertsDialog.Run(te.TabsParent, GetTable(te), te.FileName == null ? "<TABLE>" : Path.GetFileNameWithoutExtension(te.FileName));

		void Command_Table_Database_GenerateInserts(ITextEditor te, TableDatabaseGenerateInsertsDialog.Result result)
		{
			var table = GetTable(te);
			var header = $"INSERT INTO {result.TableName} ({string.Join(", ", Enumerable.Range(0, table.NumColumns).Select(column => table.GetHeader(column)))}) VALUES{(result.BatchSize == 1 ? " " : te.Data.DefaultEnding)}";
			var output = Enumerable.Range(0, table.NumRows).Batch(result.BatchSize).Select(batch => string.Join($",{te.Data.DefaultEnding}", batch.Select(row => $"({string.Join(", ", result.Columns.Select(column => GetDBValue(table[row, column])))})"))).Select(val => $"{header}{val}{te.Data.DefaultEnding}").ToList();
			te.Replace(new List<Range> { te.FullRange }, new List<string> { string.Join("", output) });

			var offset = 0;
			var sels = new List<Range>();
			foreach (var item in output)
			{
				sels.Add(Range.FromIndex(offset, item.Length));
				offset += item.Length;
			}
			te.SetSelections(sels);
		}

		TableDatabaseGenerateUpdatesDialog.Result Command_Table_Database_GenerateUpdates_Dialog(ITextEditor te) => TableDatabaseGenerateUpdatesDialog.Run(te.TabsParent, GetTable(te), te.FileName == null ? "<TABLE>" : Path.GetFileNameWithoutExtension(te.FileName));

		void Command_Table_Database_GenerateUpdates(ITextEditor te, TableDatabaseGenerateUpdatesDialog.Result result)
		{
			var table = GetTable(te);

			var output = Enumerable.Range(0, table.NumRows).Select(row => $"UPDATE {result.TableName} SET {string.Join(", ", result.Update.Select(column => $"{table.GetHeader(column)} = {GetDBValue(table[row, column])}"))} WHERE {string.Join(" AND ", result.Where.Select(column => $"{table.GetHeader(column)} = {GetDBValue(table[row, column])}"))}{te.Data.DefaultEnding}").ToList();
			te.Replace(new List<Range> { te.FullRange }, new List<string> { string.Join("", output) });

			var offset = 0;
			var sels = new List<Range>();
			foreach (var item in output)
			{
				sels.Add(Range.FromIndex(offset, item.Length));
				offset += item.Length;
			}
			te.SetSelections(sels);
		}

		TableDatabaseGenerateDeletesDialog.Result Command_Table_Database_GenerateDeletes_Dialog(ITextEditor te) => TableDatabaseGenerateDeletesDialog.Run(te.TabsParent, GetTable(te), te.FileName == null ? "<TABLE>" : Path.GetFileNameWithoutExtension(te.FileName));

		void Command_Table_Database_GenerateDeletes(ITextEditor te, TableDatabaseGenerateDeletesDialog.Result result)
		{
			var table = GetTable(te);

			var output = Enumerable.Range(0, table.NumRows).Select(row => $"DELETE FROM {result.TableName} WHERE {string.Join(" AND ", result.Where.Select(column => $"{table.GetHeader(column)} = {GetDBValue(table[row, column])}"))}{te.Data.DefaultEnding}").ToList();
			te.Replace(new List<Range> { te.FullRange }, new List<string> { string.Join("", output) });

			var offset = 0;
			var sels = new List<Range>();
			foreach (var item in output)
			{
				sels.Add(Range.FromIndex(offset, item.Length));
				offset += item.Length;
			}
			te.SetSelections(sels);
		}
	}
}
