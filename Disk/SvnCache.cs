using System;
using System.Collections.Generic;
using NeoEdit.GUI.Common;
using SharpSvn;

namespace NeoEdit.Disk
{
	[Flags]
	public enum VersionControlStatus
	{
		Unknown = 0,
		None = 1,
		NotVersioned = 2,
		Normal = 4,
		Added = 8,
		Missing = 16,
		Deleted = 32,
		Replaced = 64,
		Modified = 128,
		Merged = 256,
		Conflicted = 512,
		Ignored = 1024,
		Obstructed = 2048,
		External = 4096,
		Incomplete = 8192,
	}

	public class SvnCache
	{
		SvnClient svnClient = null;
		readonly Dictionary<string, Dictionary<string, VersionControlStatus>> svnStatusCache = new Dictionary<string, Dictionary<string, VersionControlStatus>>();
		readonly Dictionary<string, string> svnPathRepoCache = new Dictionary<string, string>();
		readonly RunOnceTimer svnCacheClearTimer;

		public SvnCache()
		{
			svnCacheClearTimer = new RunOnceTimer(() => Clear());
		}

		public void Clear()
		{
			svnStatusCache.Clear();
			svnPathRepoCache.Clear();
			svnClient.Dispose();
			svnClient = null;
			svnCacheClearTimer.Stop();
		}

		public VersionControlStatus GetStatus(string FullName, string Path)
		{
			if (svnClient == null)
			{
				svnClient = new SvnClient();
				svnCacheClearTimer.Start();
			}

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
				svnStatusCache[repository] = new Dictionary<string, VersionControlStatus>();
				svnClient.Status(Path, new SvnStatusArgs { RetrieveAllEntries = true, RetrieveIgnoredEntries = true, Depth = SvnDepth.Infinity }, (sender, status) =>
				{
					switch (status.LocalContentStatus)
					{
						case SharpSvn.SvnStatus.None: svnStatusCache[repository][status.FullPath] = VersionControlStatus.None; break;
						case SharpSvn.SvnStatus.NotVersioned: svnStatusCache[repository][status.FullPath] = VersionControlStatus.NotVersioned; break;
						case SharpSvn.SvnStatus.Normal: svnStatusCache[repository][status.FullPath] = VersionControlStatus.Normal; break;
						case SharpSvn.SvnStatus.Added: svnStatusCache[repository][status.FullPath] = VersionControlStatus.Added; break;
						case SharpSvn.SvnStatus.Missing: svnStatusCache[repository][status.FullPath] = VersionControlStatus.Missing; break;
						case SharpSvn.SvnStatus.Deleted: svnStatusCache[repository][status.FullPath] = VersionControlStatus.Deleted; break;
						case SharpSvn.SvnStatus.Replaced: svnStatusCache[repository][status.FullPath] = VersionControlStatus.Replaced; break;
						case SharpSvn.SvnStatus.Modified: svnStatusCache[repository][status.FullPath] = VersionControlStatus.Modified; break;
						case SharpSvn.SvnStatus.Merged: svnStatusCache[repository][status.FullPath] = VersionControlStatus.Merged; break;
						case SharpSvn.SvnStatus.Conflicted: svnStatusCache[repository][status.FullPath] = VersionControlStatus.Conflicted; break;
						case SharpSvn.SvnStatus.Ignored: svnStatusCache[repository][status.FullPath] = VersionControlStatus.Ignored; break;
						case SharpSvn.SvnStatus.Obstructed: svnStatusCache[repository][status.FullPath] = VersionControlStatus.Obstructed; break;
						case SharpSvn.SvnStatus.External: svnStatusCache[repository][status.FullPath] = VersionControlStatus.External; break;
						case SharpSvn.SvnStatus.Incomplete: svnStatusCache[repository][status.FullPath] = VersionControlStatus.Incomplete; break;
					}
				});
			}

			if (svnStatusCache[repository].ContainsKey(FullName))
				return svnStatusCache[repository][FullName];

			return VersionControlStatus.None;
		}
	}
}
