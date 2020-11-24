using System.Linq;

namespace NeoEdit.Editor
{
	partial class NEWindow
	{
		void Execute_File_Select_All()
		{
			ActiveFiles = NEFiles;
			Focused = ActiveFiles.FirstOrDefault();
		}

		void Execute_File_Select_None() => state.NEWindow.ClearActiveFiles();

		void Execute_File_Select_WithWithoutSelections(bool hasSelections) => state.NEWindow.SetActiveFiles(state.NEWindow.ActiveFiles.Where(neFile => neFile.Selections.Any() == hasSelections));

		void Execute_File_Select_ModifiedUnmodified(bool modified) => state.NEWindow.SetActiveFiles(state.NEWindow.ActiveFiles.Where(neFile => neFile.IsModified == modified));

		void Execute_File_Select_Inactive() => state.NEWindow.SetActiveFiles(state.NEWindow.NEFiles.Except(state.NEWindow.ActiveFiles));
	}
}
