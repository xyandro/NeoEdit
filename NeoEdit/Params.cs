using System.Collections.Generic;
using System.IO;
using System.Linq;
using NeoEdit.Common.Transform;
#if BuildConsole
using NeoEdit.Console;
#endif
#if BuildDisk
using NeoEdit.Disk;
#endif
using NeoEdit.GUI.About;
#if BuildHandles
using NeoEdit.Handles;
#endif
#if BuildHexEdit
using NeoEdit.HexEdit;
#endif
#if BuildProcesses
using NeoEdit.Processes;
#endif
#if BuildRegistry
using NeoEdit.Registry;
#endif
#if BuildSystemInfo
using NeoEdit.SystemInfo;
#endif
#if BuildTextEdit
using NeoEdit.TextEdit;
#endif
#if BuildTextView
using NeoEdit.TextView;
#endif

namespace NeoEdit
{
	abstract class Param
	{
		public abstract void Execute();
	}

	class AboutParam : Param
	{
		public override void Execute() { AboutWindow.Run(); }
	}

	class ConsoleParam : Param
	{
		public override void Execute()
		{
#if BuildConsole
			new ConsoleTabs();
#endif
		}
	}

	class ConsoleRunnerParam : Param
	{
		readonly string[] ParamList;
		public ConsoleRunnerParam(string[] paramList) { ParamList = paramList; }
		public override void Execute()
		{
#if BuildConsole
			new Console.ConsoleRunner(ParamList);
#endif
		}
	}

	class DiffParam : Param
	{
		readonly string File1, File2;
		public DiffParam(string file1, string file2) { File1 = file1; File2 = file2; }
		public override void Execute()
		{
#if BuildTextEdit
			TextEditTabs.CreateDiff(File1, File2);
#endif
		}
	}

	class DiskParam : Param
	{
		readonly string Location;
		public DiskParam(string location) { Location = location; }
		public override void Execute()
		{
#if BuildDisk
			new DiskTabs(Location);
#endif
		}
	}

	class GUnZipParam : Param
	{
		readonly string Input, Output;
		public GUnZipParam(string input, string output)
		{
			Input = input;
			Output = output;
		}
		public override void Execute()
		{
			var data = File.ReadAllBytes(Input);
			data = Compression.Decompress(Compression.Type.GZip, data);
			File.WriteAllBytes(Output, data);
		}
	}

	class GZipParam : Param
	{
		readonly string Input, Output;
		public GZipParam(string input, string output)
		{
			Input = input;
			Output = output;
		}
		public override void Execute()
		{
			var data = File.ReadAllBytes(Input);
			data = Compression.Compress(Compression.Type.GZip, data);
			File.WriteAllBytes(Output, data);
		}
	}

	class HandlesParam : Param
	{
		readonly int? PID;
		public HandlesParam(int? pid) { PID = pid; }
		public override void Execute()
		{
#if BuildHandles
			new HandlesWindow(PID);
#endif
		}
	}

	class HexDumpParam : Param
	{
		readonly List<string> Files;
		public HexDumpParam(List<string> files) { Files = files; }
		public override void Execute()
		{
#if BuildHexEdit
			foreach (var file in Files)
				HexEditTabs.CreateFromDump(file);
#endif
		}
	}

	class HexEditParam : Param
	{
		readonly List<string> Files;
		public HexEditParam(List<string> files) { Files = files; }
		public override void Execute()
		{
#if BuildHexEdit
			if (!Files.Any())
				HexEditTabs.CreateFromFile();
			foreach (var file in Files)
				HexEditTabs.CreateFromFile(file);
#endif
		}
	}

	class HexPidParam : Param
	{
		readonly List<int> PIDs;
		public HexPidParam(List<int> pids) { PIDs = pids; }
		public override void Execute()
		{
#if BuildHexEdit
			foreach (var pid in PIDs)
				HexEditTabs.CreateFromProcess(pid);
#endif
		}
	}

	class ProcessesParam : Param
	{
		readonly int? PID;
		public ProcessesParam(int? pid) { PID = pid; }
		public override void Execute()
		{
#if BuildProcesses
			new ProcessesWindow(PID);
#endif
		}
	}

	class RegistryParam : Param
	{
		readonly string Key;
		public RegistryParam(string key) { Key = key; }
		public override void Execute()
		{
#if BuildRegistry
			new RegistryWindow(Key);
#endif
		}
	}

	class SystemInfoParam : Param
	{
		public override void Execute()
		{
#if BuildSystemInfo
			new SystemInfoWindow();
#endif
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
#if BuildTextEdit
			if (!Files.Any())
				TextEditTabs.Create();
			foreach (var file in Files)
				TextEditTabs.Create(file.FileName, line: file.Line, column: file.Column);
#endif
		}
	}

	class TextViewParam : Param
	{
		readonly List<string> Files;
		public TextViewParam(List<string> files) { Files = files; }
		public override void Execute()
		{
#if BuildTextView
			if (!Files.Any())
				TextViewerTabs.Create();
			foreach (var file in Files)
				TextViewerTabs.Create(file);
#endif
		}
	}
}
