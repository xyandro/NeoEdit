using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NeoEdit.Common;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Transform;
using NeoEdit.TaskRunning;

namespace NeoEdit.Editor
{
	partial class NEFile
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

		static bool PreExecute_Window_New_NewWindow()
		{
			new NEWindow(true);
			return true;
		}

		static bool PreExecute_Window_New_FromSelections_AllSelections()
		{
			var newFiles = EditorExecuteState.CurrentState.NEWindow.ActiveFiles.AsTaskRunner().SelectMany(neFile => neFile.Selections.AsTaskRunner().Select(range => neFile.Text.GetString(range)).Select(str => new NEFile(bytes: Coder.StringToBytes(str, Coder.CodePage.UTF8), codePage: Coder.CodePage.UTF8, contentType: neFile.ContentType, modified: false)).ToList()).ToList();
			newFiles.ForEach((neFile, index) => neFile.DisplayName = $"Selection {index + 1}");

			var neWindow = new NEWindow();
			newFiles.ForEach(neFile => neWindow.AddNewFile(neFile));

			return true;
		}

		static bool PreExecute_Window_New_FromSelections_EachFile()
		{
			var newFileDatas = EditorExecuteState.CurrentState.NEWindow.ActiveFiles.AsTaskRunner().Select(neFile => (DisplayName: neFile.GetDisplayName(), Selections: neFile.GetSelectionStrings(), neFile.ContentType)).ToList();
			var newFiles = new List<NEFile>();
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

				newFiles.Add(new NEFile(displayName: newFileData.DisplayName, bytes: Coder.StringToBytes(sb.ToString(), Coder.CodePage.UTF8), codePage: Coder.CodePage.UTF8, contentType: newFileData.ContentType, modified: false) { Selections = selections });
			}

			var neWindow = new NEWindow();
			newFiles.ForEach(neFile => neWindow.AddNewFile(neFile));
			neWindow.SetLayout(EditorExecuteState.CurrentState.NEWindow.WindowLayout);

			return true;
		}

		static bool PreExecute_Window_New_SummarizeSelections_AllSelectionsEachFile_IgnoreMatchCase(bool caseSensitive, bool showAllFiles)
		{
			var selectionsByFile = EditorExecuteState.CurrentState.NEWindow.ActiveFiles.Select((neFile, index) => (DisplayName: neFile.GetSummaryName(index), Selections: neFile.GetSelectionStrings())).ToList();

			if (!showAllFiles)
				selectionsByFile = new List<(string DisplayName, IReadOnlyList<string> Selections)> { (DisplayName: "Summary", Selections: selectionsByFile.SelectMany(x => x.Selections).ToList()) };

			var comparer = caseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
			var summaryByFile = selectionsByFile.Select(tuple => (tuple.DisplayName, selections: tuple.Selections.GroupBy(x => x, comparer).Select(group => (str: group.Key, count: group.Count())).OrderByDescending(x => x.count).ToList())).ToList();

			var neWindow = new NEWindow(false);
			foreach (var neFile in summaryByFile)
				neWindow.AddNewFile(CreateSummaryFile(neFile.DisplayName, neFile.selections));
			neWindow.SetLayout(new WindowLayout(maxColumns: 4, maxRows: 4));

			return true;
		}

		static bool PreExecute_Window_New_FromClipboard_AllSelections()
		{
			AddFilesFromClipboardSelections(new NEWindow());
			return true;
		}

		static bool PreExecute_Window_New_FromClipboard_EachFile()
		{
			AddFilesFromClipboards(new NEWindow());
			return true;
		}

		static bool PreExecute_Window_New_FromActiveFiles()
		{
			var active = EditorExecuteState.CurrentState.NEWindow.ActiveFiles.ToList();
			active.ForEach(neFile => neFile.ClearFiles());

			var neWindow = new NEWindow();
			neWindow.SetLayout(EditorExecuteState.CurrentState.NEWindow.WindowLayout);
			active.ForEach(neFile => neWindow.AddNewFile(neFile));

			return true;
		}

		static bool PreExecute_Window_Full()
		{
			EditorExecuteState.CurrentState.NEWindow.SetLayout(new WindowLayout(1, 1));
			return true;
		}

		static bool PreExecute_Window_Grid()
		{
			EditorExecuteState.CurrentState.NEWindow.SetLayout(new WindowLayout(maxColumns: 4, maxRows: 4));
			return true;
		}

		static void Configure_Window_CustomGrid() => EditorExecuteState.CurrentState.Configuration = EditorExecuteState.CurrentState.NEWindowUI.RunDialog_Configure_Window_CustomGrid(EditorExecuteState.CurrentState.NEWindow.WindowLayout);

		static bool PreExecute_Window_CustomGrid()
		{
			EditorExecuteState.CurrentState.NEWindow.SetLayout((EditorExecuteState.CurrentState.Configuration as Configuration_Window_CustomGrid).WindowLayout);
			return true;
		}

		static bool PreExecute_Window_ActiveOnly()
		{
			EditorExecuteState.CurrentState.NEWindow.ActiveOnly = EditorExecuteState.CurrentState.MultiStatus != true;
			return true;
		}

		static bool PreExecute_Window_Font_Size()
		{
			EditorExecuteState.CurrentState.NEWindowUI.RunDialog_PreExecute_Window_Font_Size();
			return true;
		}

		static bool PreExecute_Window_Font_ShowSpecial()
		{
			Font.ShowSpecialChars = EditorExecuteState.CurrentState.MultiStatus != true;
			return true;
		}

		void Execute_Window_Binary() => ViewBinary = EditorExecuteState.CurrentState.MultiStatus != true;

		static void Configure_Window_BinaryCodePages() => EditorExecuteState.CurrentState.Configuration = EditorExecuteState.CurrentState.NEWindowUI.RunDialog_Configure_Window_BinaryCodePages(EditorExecuteState.CurrentState.NEWindow.Focused.ViewBinaryCodePages);

		void Execute_Window_BinaryCodePages() => ViewBinaryCodePages = (EditorExecuteState.CurrentState.Configuration as Configuration_Window_BinaryCodePages).CodePages;
	}
}
