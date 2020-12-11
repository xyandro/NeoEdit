﻿using System;
using System.Collections.Generic;
using System.Linq;
using NeoEdit.Common;
using NeoEdit.Common.Configuration;
using NeoEdit.Editor.PreExecution;

namespace NeoEdit.Editor
{
	partial class NEWindow
	{
		void Execute_Internal_Activate()
		{
			LastActive = DateTime.Now;
			var modified = NEFiles.Where(neFile => neFile.watcherFileModified).ToList();
			modified.ForEach(neFile => neFile.watcherFileModified = false);
			modified.ForEach(neFile => neFile.Execute_File_Refresh());
		}

		void Execute_Internal_CloseFile()
		{
			var neFile = (state.Configuration as Configuration_Internal_CloseFile).NEFile as NEFile;
			neFile.VerifyCanClose();
			neFile.Close();
		}

		void Configure_Internal_Key()
		{
			switch (state.Key)
			{
				case "Back":
				case "Delete":
				case "Left":
				case "Right":
				case "Up":
				case "Down":
					state.Configuration = new Configuration_Internal_Key { HasSelections = ActiveFiles.Any(neFile => neFile.Selections.Any(range => range.HasSelection)) };
					break;
			}
		}

		void PreExecute_Internal_Key()
		{
			if ((!state.ControlDown) || (state.AltDown))
				return;

			switch (state.Key)
			{
				case "PageUp": MovePrevNext(-1, state.ShiftDown); state.PreExecution = PreExecution_TaskFinished.Singleton; break;
				case "PageDown": MovePrevNext(1, state.ShiftDown); state.PreExecution = PreExecution_TaskFinished.Singleton; break;
				case "Tab": MovePrevNext(1, state.ShiftDown, true); state.PreExecution = PreExecution_TaskFinished.Singleton; break;
			}
		}

		void Execute_Internal_Mouse()
		{
			var configuration = state.Configuration as Configuration_Internal_Mouse;
			var neFile = configuration.NEFile as NEFile;

			if (state.AltDown)
			{
				if (!configuration.Selecting)
				{
					var nextActiveFiles = new HashSet<NEFile>(ActiveFiles);
					if (nextActiveFiles.Contains(neFile))
						nextActiveFiles.Remove(neFile);
					else
						nextActiveFiles.Add(neFile);

					SetActiveFiles(NEFiles.Where(file => nextActiveFiles.Contains(file))); // Keep order
					if (ActiveFiles.Contains(neFile))
						Focused = neFile;
				}
			}
			else if ((ActiveFiles.Count != 1) || (!ActiveFiles.Contains(neFile)))
				SetActiveFile(neFile);
			else if (!configuration.ActivateOnly)
				neFile.Execute_Internal_Mouse(configuration.Line, configuration.Column, configuration.ClickCount, configuration.Selecting);
		}

		void Execute_Internal_Redraw() => SetNeedsRender();
	}
}
