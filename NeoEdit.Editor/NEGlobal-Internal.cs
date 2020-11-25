using System;
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

			var files = commandLineParams.Files.ToList();
			var createWindow = commandLineParams.Diff;
			if ((!files.Any()) && (!commandLineParams.Background))
			{
				createWindow = true;
				files.Add(null);
			}

			var shutdownData = string.IsNullOrWhiteSpace(commandLineParams.Wait) ? null : new ShutdownData(commandLineParams.Wait, files.Count);

			if (commandLineParams.Existing)
			{
				var find = files.NonNull().ToDictionary(x => x.FileName, x => x, StringComparer.OrdinalIgnoreCase);
				var found = NEWindows.SelectMany(x => x.NEFiles).OrderByDescending(x => x.LastActive).NonNullOrWhiteSpace(x => x.FileName).Distinct(x => x.FileName.ToLower()).Where(x => find.ContainsKey(x.FileName)).ToList();
				found.GroupBy(x => x.NEWindow).ForEach(g =>
				{
					foreach (var neFile in g)
					{
						var file = find[neFile.FileName];
						files.Remove(file);
						neFile.Goto(file.Line, file.Column, file.Index);
						neFile.AddShutdownData(shutdownData);
					}
					g.Key.SetActiveFiles(g);
					g.Key.SetForeground();
				});
			}

			var neFiles = new List<NEFile>();
			foreach (var file in files)
			{
				if (file == null)
					neFiles.Add(new NEFile());
				else
					neFiles.Add(new NEFile(file.FileName, file.DisplayName, line: file.Line, column: file.Column, index: file.Index));
			}

			if (!neFiles.Any())
				return;

			neFiles.ForEach(neFile => neFile.AddShutdownData(shutdownData));

			NEWindow neWindow = null;
			if (!createWindow)
			{
				neWindow = NEWindows.OrderByDescending(x => x.LastActive).FirstOrDefault();
				if (neWindow != null)
					neWindow.SetForeground();
			}
			if (neWindow == null)
				neWindow = new NEWindow();
			neFiles.ForEach(neWindow.AddNewNEFile);
			if (commandLineParams.Diff)
				neWindow.SetupDiff(neFiles);
		}
	}
}
