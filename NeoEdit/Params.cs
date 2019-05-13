using System.Collections.Generic;
using System.Linq;
using System.Windows;
using NeoEdit;

namespace NeoEdit
{
	abstract class Param
	{
		public abstract Window Execute(string shutdownEvent);
	}

	class AboutParam : Param
	{
		public override Window Execute(string shutdownEvent) => Launcher.Static.LaunchAbout();
	}

	class DiffParam : Param
	{
		readonly List<TextEditParam.TextEditFile> Files;
		public DiffParam(List<TextEditParam.TextEditFile> files) { Files = files; }
		public override Window Execute(string shutdownEvent) => Launcher.Static.LaunchTextEditorDiff(fileName1: Files[0]?.FileName, displayName1: Files[0]?.DisplayName, line1: Files[0]?.Line, column1: Files[0]?.Column, fileName2: Files[1]?.FileName, displayName2: Files[1]?.DisplayName, line2: Files[1]?.Line, column2: Files[1]?.Column, shutdownEvent: shutdownEvent);
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
		public override Window Execute(string shutdownEvent)
		{
			if (!Files.Any())
				return Launcher.Static.LaunchTextEditorFile(shutdownEvent: shutdownEvent);
			Window window = null;
			foreach (var file in Files)
				window = Launcher.Static.LaunchTextEditorFile(file.FileName, file.DisplayName, line: file.Line, column: file.Column, shutdownEvent: shutdownEvent);
			return window;
		}
	}

	class WaitParam : Param
	{
		public readonly string ShutdownEvent;
		public WaitParam(string guid) { ShutdownEvent = guid; }
		public override Window Execute(string shutdownEvent) => null;
	}
}
