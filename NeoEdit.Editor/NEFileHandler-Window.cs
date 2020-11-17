using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NeoEdit.Common;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Transform;
using NeoEdit.Editor.PreExecution;
using NeoEdit.TaskRunning;

namespace NeoEdit.Editor
{
	partial class NEFileHandler
	{
		string GetDisplayName()
		{
			if (DisplayName != null)
				return DisplayName;
			if (FileName != null)
				return Path.GetFileName(FileName);
			return null;
		}

		string GetSummaryName(int index)
		{
			if (!string.IsNullOrWhiteSpace(DisplayName))
				return DisplayName;
			if (!string.IsNullOrWhiteSpace(FileName))
				return $"Summary for {Path.GetFileName(FileName)}";
			return $"Summary {index + 1}";
		}

		static PreExecutionStop PreExecute_Window_New_NewWindow(EditorExecuteState state)
		{
			new NEFilesHandler(true);
			return PreExecutionStop.Stop;
		}

		static PreExecutionStop PreExecute_Window_New_FromSelections_AllSelections(EditorExecuteState state)
		{
			var newFiles = state.NEFiles.ActiveFiles.AsTaskRunner().SelectMany(neFile => neFile.Selections.AsTaskRunner().Select(range => neFile.Text.GetString(range)).Select(str => new NEFileHandler(bytes: Coder.StringToBytes(str, Coder.CodePage.UTF8), codePage: Coder.CodePage.UTF8, contentType: neFile.ContentType, modified: false)).ToList()).ToList();
			newFiles.ForEach((neFile, index) =>
			{
				neFile.BeginTransaction(state);
				neFile.DisplayName = $"Selection {index + 1}";
				neFile.Commit();
			});

			var neFiles = new NEFilesHandler();
			neFiles.BeginTransaction(state);
			newFiles.ForEach(neFile => neFiles.AddFile(neFile));
			neFiles.Commit();

			return PreExecutionStop.Stop;
		}

		static PreExecutionStop PreExecute_Window_New_FromSelections_EachFile(EditorExecuteState state)
		{
			var newFileDatas = state.NEFiles.ActiveFiles.AsTaskRunner().Select(neFile => (DisplayName: neFile.GetDisplayName(), Selections: neFile.GetSelectionStrings(), neFile.ContentType)).ToList();
			var newFiles = new List<NEFileHandler>();
			foreach (var newFileData in newFileDatas)
			{
				var sb = new StringBuilder();
				var selections = new List<Range>();

				foreach (var str in newFileData.Selections)
				{
					selections.Add(Range.FromIndex(sb.Length, str.Length));
					sb.Append(str);
					if ((!str.EndsWith("\r")) && (!str.EndsWith("\n")))
						sb.Append("\r\n");
				}

				var neFile = new NEFileHandler(displayName: newFileData.DisplayName, bytes: Coder.StringToBytes(sb.ToString(), Coder.CodePage.UTF8), codePage: Coder.CodePage.UTF8, contentType: newFileData.ContentType, modified: false);
				neFile.BeginTransaction(state);
				neFile.Selections = selections;
				neFile.Commit();

				newFiles.Add(neFile);
			}

			var neFiles = new NEFilesHandler();
			neFiles.BeginTransaction(state);
			newFiles.ForEach(neFile => neFiles.AddFile(neFile));
			neFiles.SetLayout(state.NEFiles.WindowLayout);
			neFiles.Commit();

			return PreExecutionStop.Stop;
		}

		static PreExecutionStop PreExecute_Window_New_SummarizeSelections_AllSelectionsEachFile_IgnoreMatchCase(EditorExecuteState state, bool caseSensitive, bool showAllFiles)
		{
			var selectionsByFile = state.NEFiles.ActiveFiles.Select((neFile, index) => (DisplayName: neFile.GetSummaryName(index), Selections: neFile.GetSelectionStrings())).ToList();

			if (!showAllFiles)
				selectionsByFile = new List<(string DisplayName, IReadOnlyList<string> Selections)> { (DisplayName: "Summary", Selections: selectionsByFile.SelectMany(x => x.Selections).ToList()) };

			var comparer = caseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
			var summaryByFile = selectionsByFile.Select(tuple => (tuple.DisplayName, selections: tuple.Selections.GroupBy(x => x, comparer).Select(group => (str: group.Key, count: group.Count())).OrderByDescending(x => x.count).ToList())).ToList();

			var neFiles = new NEFilesHandler(false);
			neFiles.BeginTransaction(state);
			foreach (var neFile in summaryByFile)
				neFiles.AddFile(CreateSummaryFile(neFile.DisplayName, neFile.selections));
			neFiles.SetLayout(new WindowLayout(maxColumns: 4, maxRows: 4));
			neFiles.Commit();

			return PreExecutionStop.Stop;
		}

		static PreExecutionStop PreExecute_Window_New_FromClipboard_AllSelections(EditorExecuteState state)
		{
			var neFiles = new NEFilesHandler();
			neFiles.BeginTransaction(state);
			NEFilesHandler.AddFilesFromClipboardSelections(neFiles);
			neFiles.Commit();

			return PreExecutionStop.Stop;
		}

		static PreExecutionStop PreExecute_Window_New_FromClipboard_EachFile(EditorExecuteState state)
		{
			var neFiles = new NEFilesHandler();
			neFiles.BeginTransaction(state);
			NEFilesHandler.AddFilesFromClipboards(neFiles);
			neFiles.Commit();

			return PreExecutionStop.Stop;
		}

		static PreExecutionStop PreExecute_Window_New_FromActiveFiles(EditorExecuteState state)
		{
			var active = state.NEFiles.ActiveFiles.ToList();
			active.ForEach(neFile => state.NEFiles.RemoveFile(neFile));

			var neFiles = new NEFilesHandler();
			neFiles.BeginTransaction(state);
			neFiles.SetLayout(state.NEFiles.WindowLayout);
			active.ForEach(neFile => neFiles.AddFile(neFile));
			neFiles.Commit();

			return PreExecutionStop.Stop;
		}

		static PreExecutionStop PreExecute_Window_Full(EditorExecuteState state)
		{
			state.NEFiles.SetLayout(new WindowLayout(1, 1));
			return PreExecutionStop.Stop;
		}

		static PreExecutionStop PreExecute_Window_Grid(EditorExecuteState state)
		{
			state.NEFiles.SetLayout(new WindowLayout(maxColumns: 4, maxRows: 4));
			return PreExecutionStop.Stop;
		}

		static Configuration_Window_CustomGrid Configure_Window_CustomGrid(EditorExecuteState state) => state.NEFiles.FilesWindow.RunDialog_Configure_Window_CustomGrid(state.NEFiles.WindowLayout);

		static PreExecutionStop PreExecute_Window_CustomGrid(EditorExecuteState state)
		{
			state.NEFiles.SetLayout((state.Configuration as Configuration_Window_CustomGrid).WindowLayout);
			return PreExecutionStop.Stop;
		}

		static PreExecutionStop PreExecute_Window_ActiveOnly(EditorExecuteState state)
		{
			state.NEFiles.ActiveOnly = state.MultiStatus != true;
			return PreExecutionStop.Stop;
		}

		static PreExecutionStop PreExecute_Window_Font_Size(EditorExecuteState state)
		{
			state.NEFiles.FilesWindow.RunDialog_PreExecute_Window_Font_Size();
			return PreExecutionStop.Stop;
		}

		static PreExecutionStop PreExecute_Window_Font_ShowSpecial(EditorExecuteState state)
		{
			Font.ShowSpecialChars = state.MultiStatus != true;
			return PreExecutionStop.Stop;
		}

		void Execute_Window_Binary() => ViewBinary = state.MultiStatus != true;

		static Configuration_Window_BinaryCodePages Configure_Window_BinaryCodePages(EditorExecuteState state) => state.NEFiles.FilesWindow.RunDialog_Configure_Window_BinaryCodePages(state.NEFiles.Focused.ViewBinaryCodePages);

		void Execute_Window_BinaryCodePages() => ViewBinaryCodePages = (state.Configuration as Configuration_Window_BinaryCodePages).CodePages;
	}
}
