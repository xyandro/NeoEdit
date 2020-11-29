using System.Collections.Generic;
using System.IO;
using System.Linq;
using NeoEdit.Common;
using NeoEdit.Common.Enums;
using NeoEdit.Common.Transform;
using NeoEdit.Editor.PreExecution;
using NeoEdit.TaskRunning;

namespace NeoEdit.Editor
{
	partial class NEWindow
	{
		void AddFilesFromStrings(NEWindow neWindow, IReadOnlyList<(IReadOnlyList<string> strs, string name, ParserType contentType)> data)
		{
			data.AsTaskRunner().Select(x => NEFile.CreateFileFromStrings(x.strs, x.name, x.contentType)).ForEach(neFile => neWindow.AddNewNEFile(neFile));
		}

		void Execute_File_Select_All()
		{
			ActiveFiles = NEFiles;
			Focused = ActiveFiles.FirstOrDefault();
		}

		void Execute_File_Select_None() => ClearActiveFiles();

		void Execute_File_Select_WithWithoutSelections(bool hasSelections) => SetActiveFiles(ActiveFiles.Where(neFile => neFile.Selections.Any() == hasSelections));

		void Execute_File_Select_ModifiedUnmodified(bool modified) => SetActiveFiles(ActiveFiles.Where(neFile => neFile.IsModified == modified));

		void Execute_File_Select_Inactive() => SetActiveFiles(NEFiles.Except(ActiveFiles));

		void Execute_File_Select_Choose()
		{
			bool CanClose(IEnumerable<INEFile> files)
			{
				state.SavedAnswers.Clear();
				try { files.Cast<NEFile>().ForEach(file => file.VerifyCanClose()); }
				catch { return false; }
				finally
				{
					SetNeedsRender();
					RenderNEWindowUI();
				}
				return true;
			}

			void UpdateFiles(IEnumerable<INEFile> nextNEFiles, IEnumerable<INEFile> nextActiveFiles, INEFile nextFocused)
			{
				NEFileDatas = new OrderedHashSet<INEFileData>(nextNEFiles.Cast<NEFile>().Select(neFile => neFile.Data));
				SetActiveFiles(NEFiles.Intersect(nextActiveFiles.Cast<NEFile>()));
				if (!ActiveFiles.Contains(nextFocused))
					nextFocused = Focused;
				if (!ActiveFiles.Contains(nextFocused))
					nextFocused = ActiveFiles.FirstOrDefault();
				Focused = nextFocused as NEFile;
				SetNeedsRender();
				RenderNEWindowUI();
			};

			neWindowUI.RunDialog_Execute_File_Select_Choose(NEFiles, ActiveFiles, Focused, CanClose, UpdateFiles);
		}

		void Execute_File_New_New() => AddNewNEFile(new NEFile());

		void PreExecute_File_New_FromSelections_AllFilesSelections()
		{
			state.PreExecution = new PreExecution_File_New_FromSelections_AllFilesSelections
			{
				Selections = ActiveFiles.ToDictionary(x => x, x => default((IReadOnlyList<string>, string, Common.Enums.ParserType))),
			};
		}

		void PostExecute_File_New_FromSelections_All()
		{
			var preExecution = state.PreExecution as PreExecution_File_New_FromSelections_AllFilesSelections;
			var contentType = state.NEWindow.ActiveFiles.GroupBy(neFile => neFile.ContentType).OrderByDescending(group => group.Count()).Select(group => group.Key).FirstOrDefault();
			AddFilesFromStrings(state.NEWindow, new List<(IReadOnlyList<string> strs, string name, ParserType contentType)> { (preExecution.Selections.SelectMany(d => d.Value.selections).ToList(), "Selections", contentType) });
		}

		void PostExecute_File_New_FromSelections_Files()
		{
			var preExecution = state.PreExecution as PreExecution_File_New_FromSelections_AllFilesSelections;
			AddFilesFromStrings(state.NEWindow, preExecution.Selections.Values.Select((tuple, index) => (tuple.selections, tuple.name ?? $"Selections {index + 1}", tuple.contentType)).ToList());
		}

		void PostExecute_File_New_FromSelections_Selections()
		{
			var preExecution = state.PreExecution as PreExecution_File_New_FromSelections_AllFilesSelections;
			var index = 0;
			AddFilesFromStrings(state.NEWindow, preExecution.Selections.Values.SelectMany(tuple => tuple.selections.Select(str => (new List<string> { str } as IReadOnlyList<string>, $"Selection {++index}", tuple.contentType))).ToList());
		}

		void Execute_File_New_FromClipboard_All()
		{
			AddFilesFromStrings(state.NEWindow, new List<(IReadOnlyList<string> strs, string name, ParserType contentType)> { (NEClipboard.Current.Strings, "Clipboards", ParserType.None) });
		}

		void Execute_File_New_FromClipboard_Files()
		{
			AddFilesFromStrings(state.NEWindow, NEClipboard.Current.Select((clipboard, index) => (clipboard, $"Clipboard {index + 1}", ParserType.None)).ToList());
		}

		void Execute_File_New_FromClipboard_Selections()
		{
			AddFilesFromStrings(state.NEWindow, NEClipboard.Current.Strings.Select((str, index) => (new List<string> { str } as IReadOnlyList<string>, $"Clipboard {index + 1}", ParserType.None)).ToList());
		}

		void Execute_File_New_WordList()
		{
			byte[] bytes;
			using (var stream = typeof(NEWindow).Assembly.GetManifestResourceStream(typeof(NEWindow).Assembly.GetManifestResourceNames().Where(name => name.EndsWith(".Words.txt.gz")).Single()))
			using (var ms = new MemoryStream())
			{
				stream.CopyTo(ms);
				bytes = ms.ToArray();
			}

			bytes = Compressor.Decompress(bytes, Compressor.Type.GZip);
			state.NEWindow.AddNewNEFile(new NEFile(displayName: "Word List", bytes: bytes, modified: false));
		}
	}
}
