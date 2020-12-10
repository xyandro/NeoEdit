using System.Collections.Generic;

namespace Build.BuildActions
{
	class UpdateAction : BaseAction
	{
		public override string Name => "Update";

		public override void Run(WriteTextDelegate writeText, string configuration)
		{
			const string remoteName = "origin";
			const string localBranch = "master";
			const string remoteBranch = remoteName + "/" + localBranch;

			writeText($"Fetching {remoteName}...");
			Git.Fetch(remoteName);

			writeText($"Switching to {localBranch}...");
			Git.SwitchBranch(localBranch);

			writeText($"Reset to {remoteBranch}...");
			Git.Reset(remoteBranch);
		}
	}
}
