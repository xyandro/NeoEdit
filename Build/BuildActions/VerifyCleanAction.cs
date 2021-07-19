using System;

namespace Build.BuildActions
{
	class VerifyCleanAction : BaseAction
	{
		public override string Name => "Verify Clean";

		public override void Run(WriteTextDelegate writeText)
		{
			var dirty = Git.GetDirtyPaths();
			if (string.IsNullOrEmpty(dirty))
				writeText("Build directory is clean.");
			else
				throw new Exception($"Build directory is dirty:\n{dirty}");
		}
	}
}
