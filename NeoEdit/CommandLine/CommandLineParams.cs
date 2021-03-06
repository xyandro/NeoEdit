﻿using System.Collections.Generic;

namespace NeoEdit.Program.CommandLine
{
	public class CommandLineParams
	{
		public class File
		{
			public bool Existing { get; set; }
			public string FileName { get; set; }
			public string DisplayName { get; set; }
			public int? Line { get; set; }
			public int? Column { get; set; }
			public int? Index { get; set; }
		}

		public bool Background { get; set; }
		public bool Diff { get; set; }
		public List<File> Files { get; } = new List<File>();
		public string Wait { get; set; }
	}
}
