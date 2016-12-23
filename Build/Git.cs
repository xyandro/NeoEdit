using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LibGit2Sharp;

namespace Build
{
	static class Git
	{
		static readonly Repository repo;

		static Git()
		{
			var path = Repository.Discover(App.Location);
			if (path == null)
				throw new Exception("Unable to find git repository.");
			repo = new Repository(path);
		}

		public static string GetDirtyPaths() => string.Join("\n", repo.RetrieveStatus().Select(entry => $"{entry.State}: {entry.FilePath}"));

		public static IEnumerable<string> GetIgoredPaths() => repo.RetrieveStatus().Ignored.Select(entry => Path.Combine(repo.Info.WorkingDirectory, entry.FilePath));

		public static void Fetch(string branch) => repo.Fetch(branch);

		public static void SwitchBranch(string branch) => repo.Checkout(branch);

		public static void Reset(string branch) => repo.Reset(ResetMode.Hard, repo.Branches[branch].Tip);
	}
}
