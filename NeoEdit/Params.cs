using System.Collections.Generic;
using System.Linq;
using NeoEdit.GUI;

namespace NeoEdit
{
	abstract class Param
	{
		public abstract void Execute();
	}

	class AboutParam : Param
	{
		public override void Execute() => Launcher.Static.LaunchAbout();
	}

	class DiffParam : Param
	{
		readonly List<TextEditParam.TextEditFile> Files;
		public DiffParam(List<TextEditParam.TextEditFile> files) { Files = files; }
		public override void Execute() => Launcher.Static.LaunchTextEditorDiff(fileName1: Files[0]?.FileName, displayName1: Files[0]?.DisplayName, line1: Files[0]?.Line, column1: Files[0]?.Column, fileName2: Files[1]?.FileName, displayName2: Files[1]?.DisplayName, line2: Files[1]?.Line, column2: Files[1]?.Column);
	}

	class DiskParam : Param
	{
		readonly string Location;
		public DiskParam(string location) { Location = location; }
		public override void Execute() => Launcher.Static.LaunchDisk(Location);
	}

	class HandlesParam : Param
	{
		readonly int? PID;
		public HandlesParam(int? pid) { PID = pid; }
		public override void Execute() => Launcher.Static.LaunchHandles(PID);
	}

	class HexDumpParam : Param
	{
		readonly List<string> Files;
		public HexDumpParam(List<string> files) { Files = files; }
		public override void Execute()
		{
			foreach (var file in Files)
				Launcher.Static.LaunchHexEditorDump(file);
		}
	}

	class HexEditParam : Param
	{
		readonly List<string> Files;
		public HexEditParam(List<string> files) { Files = files; }
		public override void Execute()
		{
			if (!Files.Any())
				Launcher.Static.LaunchHexEditorFile();
			foreach (var file in Files)
				Launcher.Static.LaunchHexEditorFile(file);
		}
	}

	class HexPidParam : Param
	{
		readonly List<int> PIDs;
		public HexPidParam(List<int> pids) { PIDs = pids; }
		public override void Execute()
		{
			foreach (var pid in PIDs)
				Launcher.Static.LaunchHexEditorProcess(pid);
		}
	}

	class ImageEditParam : Param
	{
		readonly List<string> Files;
		public ImageEditParam(List<string> files) { Files = files; }
		public override void Execute()
		{
			if (!Files.Any())
				Launcher.Static.LaunchImageEditor();
			foreach (var file in Files)
				Launcher.Static.LaunchImageEditor(file);
		}
	}

	class LicenseParam : Param
	{
		public override void Execute() => Launcher.Static.LaunchLicense();
	}

	class NetworkParam : Param
	{
		public override void Execute() => Launcher.Static.LaunchNetwork();
	}

	class ProcessesParam : Param
	{
		readonly int? PID;
		public ProcessesParam(int? pid) { PID = pid; }
		public override void Execute() => Launcher.Static.LaunchProcesses(PID);
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
				Launcher.Static.LaunchTextEditorFile();
			foreach (var file in Files)
				Launcher.Static.LaunchTextEditorFile(file.FileName, file.DisplayName, line: file.Line, column: file.Column);
		}
	}

	class TextViewParam : Param
	{
		readonly List<string> Files;
		public TextViewParam(List<string> files) { Files = files; }
		public override void Execute()
		{
			if (!Files.Any())
				Launcher.Static.LaunchTextViewer();
			foreach (var file in Files)
				Launcher.Static.LaunchTextViewer(file);
		}
	}
}
