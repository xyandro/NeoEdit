using System;
using System.Linq;
using NeoEdit.Program.CommandLine;

namespace NeoEdit.Editor
{
	static public class CommandLineParser
	{
		static public void CreateTabs(string commandLine)
		{
			try
			{
				var clParams = CommandLineVisitor.GetCommandLineParams(commandLine);
				if (clParams.Background)
					return;

				if (!clParams.Files.Any())
				{
					new Tabs(true);
					return;
				}

				var shutdownData = string.IsNullOrWhiteSpace(clParams.Wait) ? null : new ShutdownData(clParams.Wait, clParams.Files.Count);
				var tabs = default(Tabs);
				if (!clParams.Diff)
					tabs = Tabs.Instances.OrderByDescending(x => x.LastActivated).FirstOrDefault();
				if (tabs == null)
					tabs = new Tabs();
				foreach (var file in clParams.Files)
				{
					if ((file.Existing) && (Tabs.Instances.OrderByDescending(x => x.LastActivated).Select(x => x.GotoTab(file.FileName, file.Line, file.Column, file.Index)).FirstOrDefault(x => x)))
						continue;

					tabs.HandleCommand(new ExecuteState(NECommand.Internal_AddTab) { Configuration = new Tab(file.FileName, file.DisplayName, line: file.Line, column: file.Column, index: file.Index, shutdownData: shutdownData) });
				}

				//if (clParams.Diff)
				//{
				//	for (var ctr = 0; ctr + 1 < tabsWindow.Tabs.Count; ctr += 2)
				//	{
				//		tabsWindow.Tabs[ctr].DiffTarget = tabsWindow.Tabs[ctr + 1];
				//		if (tabsWindow.Tabs[ctr].ContentType == ParserType.None)
				//			tabsWindow.Tabs[ctr].ContentType = tabsWindow.Tabs[ctr + 1].ContentType;
				//		if (tabsWindow.Tabs[ctr + 1].ContentType == ParserType.None)
				//			tabsWindow.Tabs[ctr + 1].ContentType = tabsWindow.Tabs[ctr].ContentType;
				//	}
				//	tabsWindow.SetLayout(maxColumns: 2);
				//}
			}
			catch (Exception ex) { ShowExceptionMessage(ex); }
		}

		static void ShowExceptionMessage(Exception ex)
		{
			throw new NotImplementedException();
		}
	}
}
