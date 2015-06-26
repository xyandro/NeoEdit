using System.Collections.Generic;
using System.IO;
using System.Linq;
using NeoEdit.Common.Transform;
using NeoEdit.Console;
using NeoEdit.DBViewer;
using NeoEdit.Disk;
using NeoEdit.GUI.About;
using NeoEdit.Handles;
using NeoEdit.HexEdit;
using NeoEdit.Processes;
using NeoEdit.Registry;
using NeoEdit.SystemInfo;
using NeoEdit.TextEdit;
using NeoEdit.TextView;

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
		public override void Execute() { new ConsoleTabs(); }
	}

	class ConsoleRunnerParam : Param
	{
		readonly List<string> paramList = new List<string>();
		public void AddParam(string param) { paramList.Add(param); }
		public override void Execute() { new Console.ConsoleRunner(paramList.ToArray()); }
	}

	class DBViewerParam : Param
	{
		public override void Execute() { new DBViewerWindow(); }
	}

	class DiskParam : Param
	{
		public string Location { get; set; }
		public override void Execute() { new DiskTabs(Location); }
	}

	class GUnZipParam : Param
	{
		readonly string Input, Output;
		public GUnZipParam(string input, string output) { Input = input; Output = output; }
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
		public GZipParam(string input, string output) { Input = input; Output = output; }
		public override void Execute()
		{
			var data = File.ReadAllBytes(Input);
			data = Compression.Compress(Compression.Type.GZip, data);
			File.WriteAllBytes(Output, data);
		}
	}

	class HandlesParam : Param
	{
		public int? PID { get; set; }
		public override void Execute() { new HandlesWindow(PID); }
	}

	class HexDumpParam : Param
	{
		readonly List<string> files = new List<string>();

		public void AddFile(string file) { files.Add(file); }

		public override void Execute()
		{
			foreach (var file in files)
				HexEditTabs.CreateFromDump(file);
		}
	}

	class HexEditParam : Param
	{
		readonly List<string> files = new List<string>();

		public void AddFile(string file) { files.Add(file); }

		public override void Execute()
		{
			if (!files.Any())
				HexEditTabs.CreateFromFile();
			foreach (var file in files)
				HexEditTabs.CreateFromFile(file);
		}
	}

	class HexPidParam : Param
	{
		readonly List<int> pids = new List<int>();

		public void AddPID(int pid) { pids.Add(pid); }

		public override void Execute()
		{
			foreach (var pid in pids)
				HexEditTabs.CreateFromProcess(pid);
		}
	}

	class ProcessesParam : Param
	{
		public int? PID { get; set; }
		public override void Execute() { new ProcessesWindow(PID); }
	}

	class RegistryParam : Param
	{
		public string Key { get; set; }
		public override void Execute() { new RegistryWindow(Key); }
	}

	class SystemInfoParam : Param
	{
		public override void Execute() { new SystemInfoWindow(); }
	}

	class TextEditParam : Param
	{
		public class TextEditFile
		{
			public string FileName;
			public int Line = 1;
			public int Column = 1;

			public TextEditFile(string fileName) { FileName = fileName; }
		}

		readonly List<TextEditFile> files = new List<TextEditFile>();

		public void AddFile(TextEditFile file) { files.Add(file); }

		public override void Execute()
		{
			if (!files.Any())
				TextEditTabs.Create();
			foreach (var file in files)
				TextEditTabs.Create(file.FileName, line: file.Line, column: file.Column);
		}
	}

	class TextViewParam : Param
	{
		readonly List<string> files = new List<string>();

		public void AddFile(string file) { files.Add(file); }

		public override void Execute()
		{
			if (!files.Any())
				TextViewerTabs.Create();
			foreach (var file in files)
				TextViewerTabs.Create(file);
		}
	}
}
