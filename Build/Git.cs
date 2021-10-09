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

		public static string GetDirtyPaths() => string.Join("\n", repo.RetrieveStatus().Where(entry => entry.State != FileStatus.NewInWorkdir).Select(entry => $"{entry.State}: {entry.FilePath}"));

		public static IEnumerable<string> GetIgoredPaths() => repo.RetrieveStatus().Ignored.Select(entry => Path.Combine(repo.Info.WorkingDirectory, entry.FilePath));

		public static void Fetch(string branch)
		{
			var remote = repo.Network.Remotes["origin"];
			Commands.Fetch(repo, remote.Name, remote.FetchRefSpecs.Select(x => x.Specification), null, "");
		}

		public static void SwitchBranch(string branch) => Commands.Checkout(repo, repo.Branches[branch]);

		public static void Reset(string branch) => repo.Reset(ResetMode.Hard, repo.Branches[branch].Tip);

		public static int CommitCount() => repo.Head.Commits.Count();

		public static void Revert(IEnumerable<string> paths) => repo.CheckoutPaths(repo.Head.FriendlyName, paths, new CheckoutOptions { CheckoutModifiers = CheckoutModifiers.Force });
	}
}
