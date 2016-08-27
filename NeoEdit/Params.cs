using System.Collections.Generic;
using System.Linq;
using NeoEdit.Console;
using NeoEdit.Disk;
using NeoEdit.GUI.About;
using NeoEdit.Handles;
using NeoEdit.HexEdit;
using NeoEdit.Network;
using NeoEdit.Processes;
using NeoEdit.SystemInfo;
using NeoEdit.TextEdit;
using NeoEdit.TextView;
using NeoEdit.Tools;

namespace NeoEdit
{
	abstract class Param
	{
		public abstract void Execute();
	}

	class AboutParam : Param
	{
		public override void Execute() => AboutWindow.Run();
	}

	class ConsoleParam : Param
	{
		public override void Execute() => ConsoleTabs.Create();
	}

	class ConsoleRunnerParam : Param
	{
		readonly string[] ParamList;
		public ConsoleRunnerParam(string[] paramList) { ParamList = paramList; }
		public override void Execute() => new Console.ConsoleRunner(ParamList);
	}

	class DiffParam : Param
	{
		readonly List<TextEditParam.TextEditFile> Files;
		public DiffParam(List<TextEditParam.TextEditFile> files) { Files = files; }
		public override void Execute() => TextEditTabs.CreateDiff().AddDiff(fileName1: Files[0]?.FileName, displayName1: Files[0]?.DisplayName, line1: Files[0]?.Line, column1: Files[0]?.Column, fileName2: Files[1]?.FileName, displayName2: Files[1]?.DisplayName, line2: Files[1]?.Line, column2: Files[1]?.Column);
	}

	class DiskParam : Param
	{
		readonly string Location;
		public DiskParam(string location) { Location = location; }
		public override void Execute() => DiskTabs.Create(Location);
	}

	class HandlesParam : Param
	{
		readonly int? PID;
		public HandlesParam(int? pid) { PID = pid; }
		public override void Execute() => new HandlesWindow(PID);
	}

	class HexDumpParam : Param
	{
		readonly List<string> Files;
		public HexDumpParam(List<string> files) { Files = files; }
		public override void Execute()
		{
			foreach (var file in Files)
				HexEditTabs.CreateFromDump(file);
		}
	}

	class HexEditParam : Param
	{
		readonly List<string> Files;
		public HexEditParam(List<string> files) { Files = files; }
		public override void Execute()
		{
			if (!Files.Any())
				HexEditTabs.CreateFromFile();
			foreach (var file in Files)
				HexEditTabs.CreateFromFile(file);
		}
	}

	class HexPidParam : Param
	{
		readonly List<int> PIDs;
		public HexPidParam(List<int> pids) { PIDs = pids; }
		public override void Execute()
		{
			foreach (var pid in PIDs)
				HexEditTabs.CreateFromProcess(pid);
		}
	}

	class NetworkParam : Param
	{
		public override void Execute() => new NetworkWindow();
	}

	class ProcessesParam : Param
	{
		readonly int? PID;
		public ProcessesParam(int? pid) { PID = pid; }
		public override void Execute() => new ProcessesWindow(PID);
	}

	class SystemInfoParam : Param
	{
		public override void Execute() => new SystemInfoWindow();
	}

	class TextEditParam : Param
	{
		public class TextEditFile
		{
			public readonly string FileName;
			public readonly string DisplayName;
			public readonly int Line = 1;
			public readonly int Column = 1;

			public TextEditFile(string fileName, string displayName, int? line, int? column)
			{
				FileName = fileName;
				DisplayName = displayName;
				Line = line ?? Line;
				Column = column ?? Column;
			}
		}

		readonly List<TextEditFile> Files;
		public TextEditParam(List<TextEditFile> files) { Files = files; }
		public override void Execute()
		{
			if (!Files.Any())
				TextEditTabs.Create();
			foreach (var file in Files)
				TextEditTabs.Create(file.FileName, file.DisplayName, line: file.Line, column: file.Column);
		}
	}

	class TextViewParam : Param
	{
		readonly List<string> Files;
		public TextViewParam(List<string> files) { Files = files; }
		public override void Execute()
		{
			if (!Files.Any())
				TextViewTabs.Create();
			foreach (var file in Files)
				TextViewTabs.Create(file);
		}
	}

	class ToolsParam : Param
	{
		public override void Execute() => new ToolsMain();
	}
}
