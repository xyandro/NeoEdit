﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NeoEdit.Common;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Enums;
using NeoEdit.TaskRunning;

namespace NeoEdit.Editor
{
	partial class NEFile
	{
		static void AddFilesFromStrings(NEWindow neWindow, IReadOnlyList<(IReadOnlyList<string> strs, string name, ParserType contentType)> data) => data.AsTaskRunner().Select(x => CreateFileFromStrings(x.strs, x.name, x.contentType)).ForEach(neFile => neWindow.AddNewNEFile(neFile));

		string GetSummaryName(int index)
		{
			if (!string.IsNullOrWhiteSpace(DisplayName))
				return DisplayName;
			if (!string.IsNullOrWhiteSpace(FileName))
				return $"Summary for {Path.GetFileName(FileName)}";
			return $"Summary {index + 1}";
		}

		static void PreExecute__Window_New_New()
		{
			var neWindow = new NEWindow();
			neWindow.AddNewNEFile(new NEFile());
		}

		static void PreExecute__Window_New_FromSelections_All()
		{
			var contentType = state.NEWindow.ActiveFiles.GroupBy(neFile => neFile.ContentType).OrderByDescending(group => group.Count()).Select(group => group.Key).FirstOrDefault();
			var neWindow = new NEWindow();
			AddFilesFromStrings(neWindow, new List<(IReadOnlyList<string> strs, string name, ParserType contentType)> { (state.NEWindow.ActiveFiles.SelectMany(neFile => neFile.GetSelectionStrings()).ToList(), "Selections", contentType) });
			neWindow.WindowLayout = state.NEWindow.WindowLayout;
		}

		static void PreExecute__Window_New_FromSelections_Files()
		{
			var neWindow = new NEWindow();
			AddFilesFromStrings(neWindow, state.NEWindow.ActiveFiles.Select((neFile, index) => (neFile.GetSelectionStrings(), neFile.GetSelectionsName() ?? $"Selections {index + 1}", neFile.ContentType)).ToList());
			neWindow.WindowLayout = state.NEWindow.WindowLayout;
		}

		static void PreExecute__Window_New_FromSelections_Selections()
		{
			var index = 0;
			var neWindow = new NEWindow();
			AddFilesFromStrings(neWindow, state.NEWindow.ActiveFiles.SelectMany(neFile => neFile.GetSelectionStrings().Select(str => (new List<string> { str } as IReadOnlyList<string>, $"Selection {++index}", neFile.ContentType))).ToList());
			neWindow.WindowLayout = state.NEWindow.WindowLayout;
		}

		static void PreExecute__Window_New_SummarizeSelections_Files_IgnoreCase__Window_New_SummarizeSelections_Files_MatchCase__Window_New_SummarizeSelections_Selections_IgnoreCase__Window_New_SummarizeSelections_Selections_MatchCase(bool caseSensitive, bool showAllFiles)
		{
			var selectionsByFile = state.NEWindow.ActiveFiles.Select((neFile, index) => (DisplayName: neFile.GetSummaryName(index), Selections: neFile.GetSelectionStrings())).ToList();

			if (!showAllFiles)
				selectionsByFile = new List<(string DisplayName, IReadOnlyList<string> Selections)> { (DisplayName: "Summary", Selections: selectionsByFile.SelectMany(x => x.Selections).ToList()) };

			var comparer = caseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
			var summaryByFile = selectionsByFile.Select(tuple => (tuple.DisplayName, selections: tuple.Selections.GroupBy(x => x, comparer).Select(group => (str: group.Key, count: group.Count())).OrderByDescending(x => x.count).ToList())).ToList();

			var neWindow = new NEWindow();
			foreach (var neFile in summaryByFile)
				neWindow.AddNewNEFile(CreateSummaryFile(neFile.DisplayName, neFile.selections));
			neWindow.WindowLayout = new WindowLayout(maxColumns: 4, maxRows: 4);
		}

		static void PreExecute__Window_New_FromClipboard_All()
		{
			var neWindow = new NEWindow();
			AddFilesFromStrings(neWindow, new List<(IReadOnlyList<string> strs, string name, ParserType contentType)> { (NEClipboard.Current.Strings, "Clipboards", ParserType.None) });
			neWindow.WindowLayout = new WindowLayout(maxColumns: 4, maxRows: 4);
		}

		static void PreExecute__Window_New_FromClipboard_Files()
		{
			var neWindow = new NEWindow();
			AddFilesFromStrings(neWindow, NEClipboard.Current.Select((clipboard, index) => (clipboard, $"Clipboard {index + 1}", ParserType.None)).ToList());
			neWindow.WindowLayout = new WindowLayout(maxColumns: 4, maxRows: 4);
		}

		static void PreExecute__Window_New_FromClipboard_Selections()
		{
			var neWindow = new NEWindow();
			AddFilesFromStrings(neWindow, NEClipboard.Current.Strings.Select((str, index) => (new List<string> { str } as IReadOnlyList<string>, $"Clipboard {index + 1}", ParserType.None)).ToList());
			neWindow.WindowLayout = new WindowLayout(maxColumns: 4, maxRows: 4);
		}

		static void PreExecute__Window_New_FromFiles_Active()
		{
			var neWindow = new NEWindow();
			foreach (var neFile in state.NEWindow.ActiveFiles)
			{
				neFile.ClearNEFiles();
				neWindow.AddNewNEFile(neFile);
			}
			neWindow.WindowLayout = state.NEWindow.WindowLayout;
		}

		static void PreExecute__Window_New_FromFiles_CopiedCut()
		{
			var neWindow = new NEWindow();
			NEClipboard.Current.Strings.AsTaskRunner().Select(file => new NEFile(file)).ForEach(neFile => neWindow.AddNewNEFile(neFile));
			neWindow.WindowLayout = new WindowLayout(maxColumns: 4, maxRows: 4);
		}

		static void PreExecute__Window_Full() => state.NEWindow.WindowLayout = new WindowLayout(1, 1);

		static void PreExecute__Window_Grid() => state.NEWindow.WindowLayout = new WindowLayout(maxColumns: 4, maxRows: 4);

		static void Configure__Window_CustomGrid() => state.Configuration = state.NEWindow.neWindowUI.RunDialog_Configure_Window_CustomGrid(state.NEWindow.WindowLayout);

		static void PreExecute__Window_CustomGrid() => state.NEWindow.WindowLayout = (state.Configuration as Configuration_Window_CustomGrid).WindowLayout;

		static void PreExecute__Window_WorkMode() => state.NEWindow.WorkMode = state.MultiStatus != true;

		static void PreExecute__Window_Font_Size() => state.NEWindow.neWindowUI.RunDialog_PreExecute_Window_Font_Size();

		static void PreExecute__Window_Font_ShowSpecial() => Settings.ShowSpecialChars = state.MultiStatus != true;

		void Execute__Window_ViewBinary() => ViewBinary = state.MultiStatus != true;

		static void Configure__Window_BinaryCodePages() => state.Configuration = state.NEWindow.neWindowUI.RunDialog_Configure_Window_BinaryCodePages(state.NEWindow.Focused.ViewBinaryCodePages);

		void Execute__Window_BinaryCodePages() => ViewBinaryCodePages = (state.Configuration as Configuration_Window_BinaryCodePages).CodePages;
	}
}
