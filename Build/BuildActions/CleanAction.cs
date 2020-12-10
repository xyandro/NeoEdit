using System.Collections.Generic;
using System.IO;

namespace Build.BuildActions
{
	class CleanAction : BaseAction
	{
		public override string Name => "Clean";

		public override void Run(WriteTextDelegate writeText, string configuration)
		{
			var releases = $@"{App.Location}\Release";
			foreach (var path in Git.GetIgoredPaths())
			{
				if (path.StartsWith(releases))
					continue;
				if (File.Exists(path))
					File.Delete(path);
				else if (Directory.Exists(path))
					Directory.Delete(path, true);
			}
		}
	}
}
