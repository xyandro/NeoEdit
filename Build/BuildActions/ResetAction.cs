using System.Collections.Generic;
using System.IO;

namespace Build.BuildActions
{
	class ResetAction : BaseAction
	{
		public override string Name => "Reset";

		public override void Run(WriteTextDelegate writeText)
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

			writeText("Resetting versioned files...");
			Git.Revert(new List<string>
			{
				Path.Combine(App.Location, "Version.csproj"),
				Path.Combine(App.Location, "GlobalAssemblyInfo.cs"),
				Path.Combine(App.Location, "NeoEdit.Setup", "NeoEdit.Setup.vdproj"),
			});
		}
	}
}
