using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NeoEdit.Common;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Models;
using NeoEdit.Common.Transform;
using NeoEdit.Editor.PreExecution;
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

		static PreExecutionStop PreExecute_Window_New_NewWindow(EditorExecuteState state)
		{
			new NEFiles(true);
			return PreExecutionStop.Stop;
		}

		static PreExecutionStop PreExecute_Window_New_FromFileSelections(EditorExecuteState state)
		{
			var newFileDatas = state.NEFiles.ActiveFiles.AsTaskRunner().Select(neFile => (DisplayName: neFile.GetDisplayName(), Selections: neFile.GetSelectionStrings(), neFile.ContentType)).ToList();
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

				var neFile = new NEFile(displayName: newFileData.DisplayName, bytes: Coder.StringToBytes(sb.ToString(), Coder.CodePage.UTF8), codePage: Coder.CodePage.UTF8, contentType: newFileData.ContentType, modified: false);
				neFile.BeginTransaction(state);
				neFile.Selections = selections;
				neFile.Commit();

				newFiles.Add(neFile);
			}

			var neFiles = new NEFiles();
			neFiles.BeginTransaction(state);
			newFiles.ForEach(neFile => neFiles.AddFile(neFile));
			neFiles.SetLayout(state.NEFiles.WindowLayout);
			neFiles.Commit();

			return PreExecutionStop.Stop;
		}

		static PreExecutionStop PreExecute_Window_New_FromSelections(EditorExecuteState state)
		{
			var newFiles = state.NEFiles.ActiveFiles.AsTaskRunner().SelectMany(neFile => neFile.Selections.AsTaskRunner().Select(range => neFile.Text.GetString(range)).Select(str => new NEFile(bytes: Coder.StringToBytes(str, Coder.CodePage.UTF8), codePage: Coder.CodePage.UTF8, contentType: neFile.ContentType, modified: false)).ToList()).ToList();
			newFiles.ForEach((neFile, index) =>
			{
				neFile.BeginTransaction(state);
				neFile.DisplayName = $"Selection {index + 1}";
				neFile.Commit();
			});

			var neFiles = new NEFiles();
			neFiles.BeginTransaction(state);
			newFiles.ForEach(neFile => neFiles.AddFile(neFile));
			neFiles.Commit();

			return PreExecutionStop.Stop;
		}

		static PreExecutionStop PreExecute_Window_New_FromClipboards(EditorExecuteState state)
		{
			var neFiles = new NEFiles();
			neFiles.BeginTransaction(state);
			NEFiles.AddFilesFromClipboards(neFiles);
			neFiles.Commit();

			return PreExecutionStop.Stop;
		}

		static PreExecutionStop PreExecute_Window_New_FromClipboardSelections(EditorExecuteState state)
		{
			var neFiles = new NEFiles();
			neFiles.BeginTransaction(state);
			NEFiles.AddFilesFromClipboardSelections(neFiles);
			neFiles.Commit();

			return PreExecutionStop.Stop;
		}

		static PreExecutionStop PreExecute_Window_New_FromActiveFiles(EditorExecuteState state)
		{
			var active = state.NEFiles.ActiveFiles.ToList();
			active.ForEach(neFile => state.NEFiles.RemoveFile(neFile));

			var neFiles = new NEFiles();
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

		static Configuration_Window_CustomGrid Configure_Window_CustomGrid(EditorExecuteState state) => state.NEFiles.FilesWindow.Configure_Window_CustomGrid(state.NEFiles.WindowLayout);

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

		static PreExecutionStop PreExecute_Window_ActiveFiles(EditorExecuteState state)
		{
			var data = new WindowActiveFilesDialogData();
			void RecalculateData()
			{
				data.AllFiles = state.NEFiles.AllFiles.Select(neFile => neFile.NEFileLabel).ToList();
				data.ActiveIndexes = state.NEFiles.ActiveFiles.Select(neFile => state.NEFiles.AllFiles.IndexOf(neFile)).ToList();
				data.FocusedIndex = state.NEFiles.AllFiles.IndexOf(state.NEFiles.Focused);
			}
			RecalculateData();
			data.SetActiveIndexes = list =>
			{
				state.NEFiles.ClearAllActive();
				list.Select(index => state.NEFiles.AllFiles[index]).ForEach(neFile => state.NEFiles.SetActive(neFile));
				RecalculateData();
				state.NEFiles.RenderFilesWindow();
			};
			data.CloseFiles = list =>
			{
				var neFiles = list.Select(index => state.NEFiles.AllFiles[index]).ToList();
				neFiles.ForEach(neFile => neFile.VerifyCanClose());
				neFiles.ForEach(neFile => state.NEFiles.RemoveFile(neFile));
				RecalculateData();
				state.NEFiles.RenderFilesWindow();
			};
			data.DoMoves = moves =>
			{
				moves.ForEach(((int oldIndex, int newIndex) move) => state.NEFiles.MoveFile(state.NEFiles.AllFiles[move.oldIndex], move.newIndex));
				RecalculateData();
				state.NEFiles.RenderFilesWindow();
			};

			state.NEFiles.FilesWindow.RunWindowActiveFilesDialog(data);

			return PreExecutionStop.Stop;
		}

		void Execute_Window_FileIndex(bool activeOnly)
		{
			ReplaceSelections((NEFiles.GetFileIndex(this, activeOnly) + 1).ToString());
		}

		static PreExecutionStop PreExecute_Window_Font_Size(EditorExecuteState state)
		{
			state.NEFiles.FilesWindow.RunWindowFontSizeDialog();
			return PreExecutionStop.Stop;
		}

		static PreExecutionStop PreExecute_Window_Font_ShowSpecial(EditorExecuteState state)
		{
			Font.ShowSpecialChars = state.MultiStatus != true;
			return PreExecutionStop.Stop;
		}

		void Execute_Window_ViewBinary() => ViewBinary = state.MultiStatus != true;

		static Configuration_Window_ViewBinaryCodePages Configure_Window_ViewBinaryCodePages(EditorExecuteState state) => state.NEFiles.FilesWindow.Configure_Window_ViewBinaryCodePages(state.NEFiles.Focused.ViewBinaryCodePages);

		void Execute_Window_ViewBinaryCodePages() => ViewBinaryCodePages = (state.Configuration as Configuration_Window_ViewBinaryCodePages).CodePages;

		static PreExecutionStop PreExecute_Window_Select_AllFiles(EditorExecuteState state)
		{
			state.NEFiles.AllFiles.ForEach(neFile => state.NEFiles.SetActive(neFile));
			state.NEFiles.Focused = state.NEFiles.AllFiles.FirstOrDefault();

			return PreExecutionStop.Stop;
		}

		static PreExecutionStop PreExecute_Window_Select_NoFiles(EditorExecuteState state)
		{
			state.NEFiles.ClearAllActive();
			return PreExecutionStop.Stop;
		}

		static PreExecutionStop PreExecute_Window_Select_FilesWithWithoutSelections(EditorExecuteState state, bool hasSelections)
		{
			state.NEFiles.ActiveFiles.ForEach(neFile => state.NEFiles.SetActive(neFile, neFile.Selections.Any() == hasSelections));
			return PreExecutionStop.Stop;
		}

		static PreExecutionStop PreExecute_Window_Select_ModifiedUnmodifiedFiles(EditorExecuteState state, bool modified)
		{
			state.NEFiles.ActiveFiles.ForEach(neFile => state.NEFiles.SetActive(neFile, neFile.IsModified == modified));
			return PreExecutionStop.Stop;
		}

		static PreExecutionStop PreExecute_Window_Select_InactiveFiles(EditorExecuteState state)
		{
			state.NEFiles.AllFiles.ForEach(neFile => state.NEFiles.SetActive(neFile, !Enumerable.Contains<NEFile>(state.NEFiles.ActiveFiles, neFile)));
			return PreExecutionStop.Stop;
		}

		static PreExecutionStop PreExecute_Window_Close_FilesWithWithoutSelections(EditorExecuteState state, bool hasSelections)
		{
			foreach (var neFile in state.NEFiles.ActiveFiles.Where(neFile => neFile.Selections.Any() == hasSelections))
			{
				neFile.VerifyCanClose();
				state.NEFiles.RemoveFile(neFile);
			}

			return PreExecutionStop.Stop;
		}

		static PreExecutionStop PreExecute_Window_Close_ModifiedUnmodifiedFiles(EditorExecuteState state, bool modified)
		{
			foreach (var neFile in state.NEFiles.ActiveFiles.Where(neFile => neFile.IsModified == modified))
			{
				neFile.VerifyCanClose();
				state.NEFiles.RemoveFile(neFile);
			}

			return PreExecutionStop.Stop;
		}

		static PreExecutionStop PreExecute_Window_Close_ActiveInactiveFiles(EditorExecuteState state, bool active)
		{
			foreach (var neFile in (active ? state.NEFiles.ActiveFiles : state.NEFiles.AllFiles.Except(state.NEFiles.ActiveFiles)))
			{
				neFile.VerifyCanClose();
				state.NEFiles.RemoveFile(neFile);
			}

			return PreExecutionStop.Stop;
		}

		static PreExecutionStop PreExecute_Window_WordList(EditorExecuteState state)
		{
			byte[] data;
			var streamName = typeof(NEFiles).Assembly.GetManifestResourceNames().Where(name => name.EndsWith(".Words.txt.gz")).Single();
			using (var stream = typeof(NEFiles).Assembly.GetManifestResourceStream(streamName))
			using (var ms = new MemoryStream())
			{
				stream.CopyTo(ms);
				data = ms.ToArray();
			}

			data = Compressor.Decompress(data, Compressor.Type.GZip);
			data = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(data));
			state.NEFiles.AddFile(new NEFile(displayName: "Word List", bytes: data, modified: false));

			return PreExecutionStop.Stop;
		}
	}
}
