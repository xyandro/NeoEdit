namespace Build.BuildActions
{
	class DeleteReleasesAction : BaseAction
	{
		public override string Name => "Delete Releases";

		public override bool Prepare() => App.EnsureGitHubTokenExists();

		public override void Run(WriteTextDelegate writeText)
		{
			using var git = new GitHub();

			writeText("Looking for releases...");
			var releaseIds = git.GetReleaseIDs().Result;
			writeText($"{releaseIds.Count} releases found.");
			foreach (var releaseId in releaseIds)
			{
				// Keep oldest NeoEdit 3 around just in case
				if (releaseId == 25182147)
					continue;

				writeText($"Delete release {releaseId}...");
				git.DeleteRelease(releaseId).Wait();
			}

			writeText("Looking for tags...");
			var tagIds = git.GetTagIDs().Result;
			writeText($"{tagIds.Count} tags found.");
			foreach (var tagId in tagIds)
			{
				// Keep oldest NeoEdit 3 around just in case
				if (tagId == "3.0.1.2761")
					continue;

				writeText($"Delete tag {tagId}...");
				git.DeleteTag(tagId).Wait();
			}
		}
	}
}
