using System.Collections.Generic;
using System.Linq;
using NeoEdit.Console;
using NeoEdit.Disk;
using NeoEdit.GUI.About;
using NeoEdit.Handles;
using NeoEdit.HexEdit;
using NeoEdit.Network;
using NeoEdit.Processes;
using NeoEdit.Registry;
using NeoEdit.SystemInfo;
using NeoEdit.TableEdit;
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
		readonly string File1, File2;
		public DiffParam(string file1, string file2) { File1 = file1; File2 = file2; }
		public override void Execute() => TextEditTabs.CreateDiff(File1, File2);
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

	class RegistryParam : Param
	{
		readonly string Key;
		public RegistryParam(string key) { Key = key; }
		public override void Execute() => new RegistryWindow(Key);
	}

	class SystemInfoParam : Param
	{
		public override void Execute() => new SystemInfoWindow();
	}

	class TableEditParam : Param
	{
		readonly List<string> FileNames;
		public TableEditParam(List<string> fileNames) { FileNames = fileNames; }
		public override void Execute()
		{
			if (!FileNames.Any())
				TableEditTabs.Create();
			foreach (var fileName in FileNames)
				TableEditTabs.Create(fileName);
		}
	}

	class TextEditParam : Param
	{
		public class TextEditFile
		{
			public readonly string FileName;
			public readonly int Line = 1;
			public readonly int Column = 1;

			public TextEditFile(string fileName, int? line, int? column)
			{
				FileName = fileName;
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
				TextEditTabs.Create(file.FileName, line: file.Line, column: file.Column);
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
