using System;
using System.Linq;
using NeoEdit.Common;
using NeoEdit.Common.Configuration;

namespace NeoEdit.Editor
{
	partial class NEWindow
	{
		void Execute_Internal_Activate()
		{
			LastActive = DateTime.Now;
			NEFiles.ForEach(neFile => neFile.CheckForRefresh());
		}

		void Execute_Internal_MouseActivate()
		{
			var neFile = (state.Configuration as Configuration_Internal_MouseActivate).NEFile as NEFile;
			SetActiveFiles(NEFiles.Where(file => (file == neFile) || ((state.ShiftDown) && (ActiveFiles.Contains(file)))));
			Focused = neFile;
		}

		void Execute_Internal_CloseFile()
		{
			var neFile = (state.Configuration as Configuration_Internal_CloseFile).NEFile as NEFile;
			neFile.VerifyCanClose();
			neFile.Close();
		}
	}
}
