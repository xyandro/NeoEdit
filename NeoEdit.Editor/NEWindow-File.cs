using System.Collections.Generic;
using System.Linq;
using NeoEdit.Common;

namespace NeoEdit.Editor
{
	partial class NEWindow
	{
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
	}
}
