using System.Collections.Generic;
using System.Linq;
using NeoEdit.Common;
using NeoEdit.Common.Configuration;
namespace NeoEdit.Editor
{
	partial class NEGlobal
	{
		void Execute_Internal_CommandLine()
		{
			var commandLineParams = (state.Configuration as Configuration_Internal_CommandLine).CommandLineParams;
			if (commandLineParams.Background)
				return;

			if (!commandLineParams.Files.Any())
			{
				new NEWindow(true);
				return;
			}

			var shutdownData = string.IsNullOrWhiteSpace(commandLineParams.Wait) ? null : new ShutdownData(commandLineParams.Wait, commandLineParams.Files.Count);

			NEWindow neWindow = null;
			if (!commandLineParams.Diff)
				neWindow = state.NEGlobal.NEWindows.OrderByDescending(x => x.LastActive).FirstOrDefault();
			if (neWindow == null)
				neWindow = new NEWindow();
			var neFiles = new List<NEFile>();
			foreach (var file in commandLineParams.Files)
			{
				if (commandLineParams.Existing)
				{
					var neFile = state.NEGlobal.NEWindows.OrderByDescending(x => x.LastActive).SelectMany(x => x.ActiveFiles).OrderByDescending(x => x.LastActive).FirstOrDefault(x => x.FileName == file.FileName);
					if (neFile == null)
						neFile = state.NEGlobal.NEWindows.OrderByDescending(x => x.LastActive).SelectMany(x => x.NEFiles).FirstOrDefault(x => x.FileName == file.FileName);
					if (neFile != null)
					{
						neFile.NEWindow.SetActiveFile(neFile);
						neFile.Goto(file.Line, file.Column, file.Index);
						neFile.NEWindow.SetForeground();
						continue;
					}
				}

				neFiles.Add(new NEFile(file.FileName, file.DisplayName, line: file.Line, column: file.Column, index: file.Index, shutdownData: shutdownData));
			}

			neFiles.ForEach(neWindow.AddNewNEFile);
			if (commandLineParams.Diff)
				neWindow.SetupDiff(neFiles);

			return;
		}
	}
}
