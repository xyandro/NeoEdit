using System.Collections.Generic;
using System.IO;

namespace Build.BuildActions
{
	class DeleteIgnoredAction : BaseAction
	{
		public override string Name => "Delete Ignored";

		public override void Run(WriteTextDelegate writeText, string configuration, List<string> platforms)
		{
			writeText("Deleting ignored files...");
			foreach (var path in Git.GetIgoredPaths())
			{
				writeText($"Deleting {path}...");
				if (File.Exists(path))
					File.Delete(path);
				else if (Directory.Exists(path))
					Directory.Delete(path, true);
			}
		}
	}
}
