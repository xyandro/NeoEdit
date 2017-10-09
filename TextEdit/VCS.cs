using System.Collections.Generic;
using System.IO;
using System.Linq;
using LibGit2Sharp;
using NeoEdit.Common;
using SharpSvn;

namespace NeoEdit.TextEdit
{
	class VCS
	{
		Dictionary<string, VCSStatus> statusCache = new Dictionary<string, VCSStatus>();

		public enum VCSStatus
		{
			Unknown,
			Ignored,
			Modified,
			Normal,
		}

		readonly HashSet<string> gitRepoPaths = new HashSet<string>();
		VCSStatus? GetGitStatus(string file)
		{
			var repoPath = Repository.Discover(file);
			if (repoPath == null)
				return null;

			if (!gitRepoPaths.Contains(repoPath))
			{
				gitRepoPaths.Add(repoPath);

				var repository = new Repository(repoPath);
				var repoInfo = repository.Info;
				var status = repository.RetrieveStatus();

				new Dictionary<VCSStatus, IEnumerable<StatusEntry>>
				{
					[VCSStatus.Ignored] = status.Ignored,
					[VCSStatus.Modified] = status.Modified.Concat(status.Added).Concat(status.Staged).Concat(status.Removed),
					[VCSStatus.Unknown] = status.Untracked,
				}.ForEach(pair => pair.Value.ForEach(entry => statusCache[Path.Combine(repoInfo.WorkingDirectory, entry.FilePath).TrimEnd('\\')] = pair.Key));
				statusCache[repoInfo.Path.TrimEnd('\\')] = VCSStatus.Ignored;
			}

			return GetStatus(file) ?? VCSStatus.Normal;
		}

		SvnClient svnClient = null;
		readonly HashSet<string> svnRepoPaths = new HashSet<string>();
		VCSStatus? GetSvnStatus(string file)
		{
			if (svnClient == null)
				svnClient = new SvnClient();

			var root = svnClient.GetWorkingCopyRoot(file);
			if (root == null)
				return null;

			if (!svnRepoPaths.Contains(root))
			{
				svnRepoPaths.Add(root);

				var map = new Dictionary<SvnStatus, VCSStatus>
				{
					[SvnStatus.Zero] = VCSStatus.Unknown,
					[SvnStatus.None] = VCSStatus.Unknown,
					[SvnStatus.NotVersioned] = VCSStatus.Unknown,
					[SvnStatus.Normal] = VCSStatus.Normal,
					[SvnStatus.Added] = VCSStatus.Modified,
					[SvnStatus.Missing] = VCSStatus.Modified,
					[SvnStatus.Deleted] = VCSStatus.Modified,
					[SvnStatus.Replaced] = VCSStatus.Modified,
					[SvnStatus.Modified] = VCSStatus.Modified,
					[SvnStatus.Merged] = VCSStatus.Modified,
					[SvnStatus.Conflicted] = VCSStatus.Modified,
					[SvnStatus.Ignored] = VCSStatus.Ignored,
					[SvnStatus.Obstructed] = VCSStatus.Modified,
					[SvnStatus.External] = VCSStatus.Modified,
					[SvnStatus.Incomplete] = VCSStatus.Modified,
				};

				svnClient.Status(root, new SvnStatusArgs { RetrieveAllEntries = true, RetrieveIgnoredEntries = true, Depth = SvnDepth.Infinity }, (sender, status) => statusCache[status.FullPath] = map[status.LocalContentStatus]);
				statusCache[$@"{root}\.svn"] = VCSStatus.Ignored;
			}

			return GetStatus(file) ?? VCSStatus.Unknown;
		}

		VCSStatus? GetStatus(string file)
		{
			if (statusCache.ContainsKey(file))
				return statusCache[file];

			for (; !string.IsNullOrEmpty(file); file = Path.GetDirectoryName(file))
				if (statusCache.ContainsKey(file))
					if (statusCache[file] == VCSStatus.Ignored)
						return VCSStatus.Ignored;
					else
						break;

			return null;
		}

		public IEnumerable<VCSStatus> GetStatus(IEnumerable<string> files) => files.Select(file => GetGitStatus(file) ?? GetSvnStatus(file) ?? VCSStatus.Unknown);
	}
}
