using System.Collections.Generic;
using System.Linq;
using NeoEdit.GUI;

namespace NeoEdit
{
	abstract class Param
	{
		public abstract void Execute(string shutdownEvent);
	}

	class AboutParam : Param
	{
		public override void Execute(string shutdownEvent) => Launcher.Static.LaunchAbout();
	}

	class DiffParam : Param
	{
		readonly List<TextEditParam.TextEditFile> Files;
		public DiffParam(List<TextEditParam.TextEditFile> files) { Files = files; }
		public override void Execute(string shutdownEvent) => Launcher.Static.LaunchTextEditorDiff(fileName1: Files[0]?.FileName, displayName1: Files[0]?.DisplayName, line1: Files[0]?.Line, column1: Files[0]?.Column, fileName2: Files[1]?.FileName, displayName2: Files[1]?.DisplayName, line2: Files[1]?.Line, column2: Files[1]?.Column, shutdownEvent: shutdownEvent);
	}

	class DiskParam : Param
	{
		readonly string Location;
		public DiskParam(string location) { Location = location; }
		public override void Execute(string shutdownEvent) => Launcher.Static.LaunchDisk(Location);
	}

	class HexEditParam : Param
	{
		readonly List<string> Files;
		public HexEditParam(List<string> files) { Files = files; }
		public override void Execute(string shutdownEvent)
		{
			if (!Files.Any())
				Launcher.Static.LaunchHexEditor();
			foreach (var file in Files)
				Launcher.Static.LaunchHexEditor(file);
		}
	}

	class LicenseParam : Param
	{
		public override void Execute(string shutdownEvent) => Launcher.Static.LaunchLicense();
	}

	class StreamSaveParam : Param
	{
		readonly List<string> Urls;
		readonly bool IsPlaylist;
		public StreamSaveParam(List<string> urls, bool isPlaylist) { Urls = urls; IsPlaylist = isPlaylist; }
		public override void Execute(string shutdownEvent) => Launcher.Static.LaunchStreamSaver(Urls, IsPlaylist);
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
		public override void Execute(string shutdownEvent)
		{
			if (!Files.Any())
				Launcher.Static.LaunchTextEditorFile(shutdownEvent: shutdownEvent);
			foreach (var file in Files)
				Launcher.Static.LaunchTextEditorFile(file.FileName, file.DisplayName, line: file.Line, column: file.Column, shutdownEvent: shutdownEvent);
		}
	}

	class TextViewParam : Param
	{
		readonly List<string> Files;
		public TextViewParam(List<string> files) { Files = files; }
		public override void Execute(string shutdownEvent)
		{
			if (!Files.Any())
				Launcher.Static.LaunchTextViewer();
			foreach (var file in Files)
				Launcher.Static.LaunchTextViewer(file);
		}
	}

	class WaitParam : Param
	{
		public readonly string ShutdownEvent;
		public WaitParam(string guid) { ShutdownEvent = guid; }
		public override void Execute(string shutdownEvent) { }
	}
}
