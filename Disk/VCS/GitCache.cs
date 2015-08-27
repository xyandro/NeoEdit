using System.Collections.Generic;
using System.Linq;
using LibGit2Sharp;

namespace NeoEdit.Disk.VCS
{
	public class GitCache : IVCSCache
	{
		readonly Dictionary<string, Repository> repoCache = new Dictionary<string, Repository>();
		readonly Dictionary<string, VersionControlStatus> statusCache = new Dictionary<string, VersionControlStatus>();

		public void Clear()
		{
			foreach (var repo in repoCache.Values)
				repo.Dispose();
			repoCache.Clear();
			statusCache.Clear();
		}

		public VersionControlStatus GetStatus(string FullName, string Path)
		{
			var repoPath = Repository.Discover(Path);
			if (repoPath == null)
				return VersionControlStatus.Unknown;

			if (!repoCache.ContainsKey(repoPath))
			{
				repoCache[repoPath] = new Repository(repoPath);
				var repoInfo = repoCache[repoPath].Info;
				var status = repoCache[repoPath].RetrieveStatus();
				var statusList = new Dictionary<VersionControlStatus, IEnumerable<StatusEntry>>();
				statusList[VersionControlStatus.Ignored] = status.Ignored;
				statusList[VersionControlStatus.Modified] = status.Modified.Concat(status.Added).Concat(status.Staged);
				statusList[VersionControlStatus.Unknown] = status.Untracked;
				statusList.ToList().ForEach(pair => pair.Value.ToList().ForEach(entry => statusCache[System.IO.Path.Combine(repoInfo.WorkingDirectory, entry.FilePath).TrimEnd('\\')] = pair.Key));
				statusCache[repoInfo.Path.TrimEnd('\\')] = VersionControlStatus.None;
			}
			if (statusCache.ContainsKey(FullName))
				return statusCache[FullName];
			return VersionControlStatus.Regular;
		}
	}
}
