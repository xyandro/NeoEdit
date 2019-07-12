using System.Collections.Generic;

namespace NeoEdit.Program.CommandLine
{
	public class CommandLineParams
	{
		public class File
		{
			public string FileName { get; set; }
			public string DisplayName { get; set; }
			public int Line { get; set; } = 1;
			public int Column { get; set; } = 1;
		}

		public bool Diff { get; set; }
		public List<File> Files { get; } = new List<File>();
		public string Wait { get; set; }
	}
}
