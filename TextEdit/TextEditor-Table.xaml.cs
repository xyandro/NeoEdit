using System;
using System.Collections.Generic;
using System.Linq;
using NeoEdit.Common;
using NeoEdit.Common.Transform;
using NeoEdit.GUI.Controls;
using NeoEdit.TextEdit.Dialogs;

namespace NeoEdit.TextEdit
{
	public partial class TextEditor
	{
		void OpenTable(Table table)
		{
			var textEditor = new TextEditor(bytes: Coder.StringToBytes(table.ToString("\r\n", TextEdit.Content.Parser.ParserType.Columns), Coder.CodePage.UTF8), codePage: Coder.CodePage.UTF8);
			textEditor.ContentType = TextEdit.Content.Parser.ParserType.Columns;
			TabsParent.CreateTab(textEditor);
		}

		Table GetTable(bool hasHeaders = true) => new Table(AllText, ContentType, hasHeaders);

		void SetText(Table table)
		{
			var output = table.ToString(Data.DefaultEnding, ContentType);
			Replace(new List<Range> { FullRange }, new List<string> { output });
			Selections.Replace(FullRange);
		}

		internal void Command_Table_Type_Detect() => ContentType = Table.GuessTableType(AllText);

		internal ChooseTableTypeDialog.Result Command_Table_Convert_Dialog() => ChooseTableTypeDialog.Run(WindowParent, ContentType);

		internal void Command_Table_Convert(ChooseTableTypeDialog.Result result)
		{
			var table = GetTable();
			ContentType = result.TableType;
			SetText(table);
		}

		internal void Command_Table_AddHeaders() => SetText(GetTable(false));

		internal void Command_Table_LineSelectionsToTable()
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

		internal void Command_Table_RegionSelectionsToTable()
		{
			if (!Selections.Any())
				return;

			var sels = GetSelectionStrings();
			var regions = GetEnclosingRegions();
			var rows = Enumerable.Range(0, Selections.Count).GroupBy(index => regions[index]).Select(group => group.Select(index => sels[index]).ToList()).ToList();
			OpenTable(new Table(rows, false));
		}

		internal EditTableDialog.Result Command_Table_EditTable_Dialog() => EditTableDialog.Run(WindowParent, GetTable());

		internal void Command_Table_EditTable(EditTableDialog.Result result) => SetText(GetTable().Aggregate(result.AggregateData).Sort(result.SortData));

		static Table JoinTable;
		static List<int> JoinColumns;
		internal void Command_Table_SetJoinSource()
		{
			var table = GetTable();
			var result = ChooseTableColumnsDialog.Run(WindowParent, table);
			if (result == null)
				return;

			if (result.Columns.Count == 0)
				throw new Exception("No columns selected.");

			JoinTable = table;
			JoinColumns = result.Columns;
		}

		internal void Command_Table_Join()
		{
			if (JoinTable == null)
				throw new Exception("You must first set a join source.");

			var table = GetTable();
			var result = ChooseTableColumnsDialog.Run(WindowParent, table);
			if (result == null)
				return;
			if (JoinColumns.Count != result.Columns.Count)
				throw new Exception("Column counts must match.");

			var joinTypeResult = ChooseJoinTypeDialog.Run(WindowParent);
			if (joinTypeResult == null)
				return;

			SetText(Table.Join(table, JoinTable, result.Columns, JoinColumns, joinTypeResult.JoinType));
		}

		internal void Command_Table_Transpose() => SetText(GetTable().Transpose());

		internal void Command_Table_SetVariables()
		{
			var table = GetTable();
			for (var column = 0; column < table.NumColumns; ++column)
				variables[table.GetHeader(column)] = InterpretValues(Enumerable.Range(0, table.NumRows).Select(row => table[row, column]));
		}
	}
}
