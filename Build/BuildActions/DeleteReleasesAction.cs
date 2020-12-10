using System.Collections.Generic;

namespace Build.BuildActions
{
	class DeleteReleasesAction : BaseAction
	{
		public override string Name => "Delete Releases";

		public override bool Prepare() => App.EnsureGitHubTokenExists();

		public override void Run(WriteTextDelegate writeText, string configuration)
		{
			using (var git = new GitHub())
			{
				writeText("Looking for releases...");
				var releaseIds = git.GetReleaseIDs().Result;
				writeText($"{releaseIds.Count} releases found.");
				foreach (var releaseId in releaseIds)
				{
					writeText($"Delete release {releaseId}...");
					git.DeleteRelease(releaseId).Wait();
				}

				writeText("Looking for tags...");
				var tagIds = git.GetTagIDs().Result;
				writeText($"{tagIds.Count} tags found.");
				foreach (var tagId in tagIds)
				{
					writeText($"Delete tag {tagId}...");
					git.DeleteTag(tagId).Wait();
				}
			}
		}
	}
}
