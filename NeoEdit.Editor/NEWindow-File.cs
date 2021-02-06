using System.Collections.Generic;
using System.IO;
using System.Linq;
using NeoEdit.Common;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Enums;
using NeoEdit.Common.Expressions;
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

		void Execute_File_Select_ExternalModifiedUnmodified(bool modified) => SetActiveFiles(ActiveFiles.Where(neFile => (neFile.LastWriteTime != neFile.LastExternalWriteTime) == modified));

		void Execute_File_Select_Inactive() => SetActiveFiles(NEFiles.Except(ActiveFiles));

		void Execute_File_Select_SelectByExpression()
		{
			var expression = state.GetExpression((state.Configuration as Configuration_FileTable_Various_Various).Expression);
			SetActiveFiles(ActiveFiles.Where(neFile => expression.Evaluate<bool>(neFile.GetVariables())));
		}

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

		void Execute_File_New_FromSelections_AllFilesSelections()
		{
			state.PreExecution = new PreExecution_File_New_FromSelections_AllFilesSelections
			{
				Selections = ActiveFiles.ToDictionary(x => x, x => default((IReadOnlyList<string>, string, Common.Enums.ParserType))),
			};
		}

		void PostExecute_File_New_FromSelections_All()
		{
			var preExecution = state.PreExecution as PreExecution_File_New_FromSelections_AllFilesSelections;
			var contentType = ActiveFiles.GroupBy(neFile => neFile.ContentType).OrderByDescending(group => group.Count()).Select(group => group.Key).FirstOrDefault();
			AddFilesFromStrings(this, new List<(IReadOnlyList<string> strs, string name, ParserType contentType)> { (preExecution.Selections.SelectMany(d => d.Value.selections).ToList(), "Selections", contentType) });
		}

		void PostExecute_File_New_FromSelections_Files()
		{
			var preExecution = state.PreExecution as PreExecution_File_New_FromSelections_AllFilesSelections;
			AddFilesFromStrings(this, preExecution.Selections.Values.Select((tuple, index) => (tuple.selections, tuple.name ?? $"Selections {index + 1}", tuple.contentType)).ToList());
		}

		void PostExecute_File_New_FromSelections_Selections()
		{
			var preExecution = state.PreExecution as PreExecution_File_New_FromSelections_AllFilesSelections;
			var index = 0;
			AddFilesFromStrings(this, preExecution.Selections.Values.SelectMany(tuple => tuple.selections.Select(str => (new List<string> { str } as IReadOnlyList<string>, $"Selection {++index}", tuple.contentType))).ToList());
		}

		void Execute_File_New_FromClipboard_All()
		{
			AddFilesFromStrings(this, new List<(IReadOnlyList<string> strs, string name, ParserType contentType)> { (NEClipboard.Current.Strings, "Clipboards", ParserType.None) });
		}

		void Execute_File_New_FromClipboard_Files()
		{
			AddFilesFromStrings(this, NEClipboard.Current.Select((clipboard, index) => (clipboard, $"Clipboard {index + 1}", ParserType.None)).ToList());
		}

		void Execute_File_New_FromClipboard_Selections()
		{
			AddFilesFromStrings(this, NEClipboard.Current.Strings.Select((str, index) => (new List<string> { str } as IReadOnlyList<string>, $"Clipboard {index + 1}", ParserType.None)).ToList());
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
			AddNewNEFile(new NEFile(displayName: "Word List", bytes: bytes, modified: false));
		}

		void Configure_FileMacro_Open_Open(string initialDirectory = null)
		{
			if ((initialDirectory == null) && (Focused != null))
				initialDirectory = Path.GetDirectoryName(Focused.FileName);
			state.Configuration = neWindowUI.RunDialog_Configure_FileMacro_Open_Open("txt", initialDirectory, "Text files|*.txt|All files|*.*", 2, true);
		}

		void Execute_FileMacro_Open_Open()
		{
			var configuration = state.Configuration as Configuration_FileMacro_Open_Open;
			configuration.FileNames.ForEach(fileName => AddNewNEFile(new NEFile(fileName)));
		}

		void Execute_File_Open_CopiedCut() => NEClipboard.Current.Strings.AsTaskRunner().Select(file => new NEFile(file)).ForEach(neFile => AddNewNEFile(neFile));

		void Configure_File_Open_ReopenWithEncoding()
		{
			(var codePage, var hasBOM) = ActiveFiles.GroupBy(neFile => (neFile.CodePage, neFile.HasBOM)).OrderByDescending(group => group.Count()).Select(group => group.Key).DefaultIfEmpty((Coder.CodePage.UTF8, true)).First();
			state.Configuration = neWindowUI.RunDialog_Configure_File_OpenEncoding_ReopenWithEncoding(codePage, hasBOM);
		}

		void Configure_File_SaveCopyAdvanced_SaveAsCopyByExpressionSetDisplayName() => state.Configuration = neWindowUI.RunDialog_Configure_FileTable_Various_Various(Focused?.GetVariables() ?? new NEVariables(), Focused?.Selections.Count ?? 0);

		void Configure_File_Move_MoveByExpression() => state.Configuration = neWindowUI.RunDialog_Configure_FileTable_Various_Various(Focused?.GetVariables() ?? new NEVariables(), Focused?.Selections.Count ?? 0);

		void Configure_File_Encoding() => state.Configuration = neWindowUI.RunDialog_Configure_File_OpenEncoding_ReopenWithEncoding(Focused?.CodePage ?? Coder.CodePage.UTF8, Focused?.HasBOM ?? true);

		void Configure_File_LineEndings()
		{
			var endings = ActiveFiles.Select(neFile => neFile.Text.OnlyEnding).Distinct().Take(2).ToList();
			var ending = endings.Count == 1 ? endings[0] : "";
			state.Configuration = neWindowUI.RunDialog_Configure_File_LineEndings(ending);
		}

		void Configure_File_Advanced_Encrypt()
		{
			if (state.MultiStatus != true)
				state.Configuration = neWindowUI.RunDialog_Configure_File_Advanced_Encrypt(Cryptor.Type.AES, true);
			else
				state.Configuration = new Configuration_File_Advanced_Encrypt();
		}

		void Configure_File_Exit() => state.Configuration = new Configuration_File_Exit { ShouldExit = true };

		void Execute_File_Exit()
		{
			foreach (var neFile in NEFiles)
			{
				neFile.VerifyCanClose();
				neFile.Close();
			}
			ClearNEWindows();
		}
	}
}
