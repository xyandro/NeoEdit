using System.Collections.Generic;
using NeoEdit.GUI.Misc;

namespace NeoEdit.Disk.VCS
{
	public enum VersionControlStatus
	{
		Unknown = 0,
		None = 1,
		Regular = 2,
		Modified = 4,
		Ignored = 8,

		Standard = Regular | Modified,
		All = Regular | Modified | Ignored | None,
	}

	public interface IVCSCache
	{
		VersionControlStatus GetStatus(string FullName, string Path);
		void Clear();
	}

	public class VCSCache : IVCSCache
	{
		readonly List<IVCSCache> caches = new List<IVCSCache> { new GitCache(), new SvnCache() };
		readonly RunOnceTimer cacheClearTimer;

		static VCSCache vcsCache = new VCSCache();
		public static VCSCache Single => vcsCache;

		VCSCache() { cacheClearTimer = new RunOnceTimer(Clear); }

		public VersionControlStatus GetStatus(string FullName, string Path)
		{
			cacheClearTimer.Start();
			foreach (var cache in caches)
			{
				var result = cache.GetStatus(FullName, Path);
				if (result != VersionControlStatus.Unknown)
					return result;
			}
			return VersionControlStatus.Unknown;
		}

		public void Clear()
		{
			foreach (var cache in caches)
				cache.Clear();
			cacheClearTimer.Stop();
		}
	}
}
