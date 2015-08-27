using System;
using System.Collections.Generic;
using SharpSvn;

namespace NeoEdit.Disk.VCS
{
	public class SvnCache : IVCSCache
	{
		SvnClient svnClient = null;
		readonly Dictionary<string, Dictionary<string, VersionControlStatus>> svnStatusCache = new Dictionary<string, Dictionary<string, VersionControlStatus>>();
		readonly Dictionary<string, string> svnPathRepoCache = new Dictionary<string, string>();

		public void Clear()
		{
			svnStatusCache.Clear();
			svnPathRepoCache.Clear();
			if (svnClient != null)
				svnClient.Dispose();
			svnClient = null;
		}

		public VersionControlStatus GetStatus(string FullName, string Path)
		{
			if (svnClient == null)
				svnClient = new SvnClient();

			if (!svnPathRepoCache.ContainsKey(Path))
			{
				Uri repositoryUrl;
				Guid id;
				if (svnClient.TryGetRepository(Path, out repositoryUrl, out id))
					svnPathRepoCache[Path] = repositoryUrl.ToString();
				else
					svnPathRepoCache[Path] = null;
			}
			var repository = svnPathRepoCache[Path];
			if (repository == null)
				return VersionControlStatus.Unknown;

			if (!svnStatusCache.ContainsKey(repository))
			{
				var root = svnClient.GetWorkingCopyRoot(Path);
				svnStatusCache[repository] = new Dictionary<string, VersionControlStatus>();
				svnClient.Status(root, new SvnStatusArgs { RetrieveAllEntries = true, RetrieveIgnoredEntries = true, Depth = SvnDepth.Infinity }, (sender, status) =>
				{
					switch (status.LocalContentStatus)
					{
						case SvnStatus.Modified: svnStatusCache[repository][status.FullPath] = VersionControlStatus.Modified; break;
						case SvnStatus.Ignored: svnStatusCache[repository][status.FullPath] = VersionControlStatus.Ignored; break;
						case SvnStatus.None:
						case SvnStatus.NotVersioned: svnStatusCache[repository][status.FullPath] = VersionControlStatus.Unknown; break;
						default: svnStatusCache[repository][status.FullPath] = VersionControlStatus.Regular; break;
					}
				});
			}

			if (svnStatusCache[repository].ContainsKey(FullName))
				return svnStatusCache[repository][FullName];

			return VersionControlStatus.None;
		}
	}
}
