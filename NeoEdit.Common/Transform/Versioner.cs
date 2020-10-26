using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LibGit2Sharp;
using SharpSvn;

namespace NeoEdit.Common.Transform
{
	public static class Versioner
	{
		public enum Status
		{
			None = 1,
			Normal = 2,
			Modified = 4,
			Ignored = 8,
			Unknown = 16,
			VersionControl = 32,

			All = Normal | Modified | Ignored | Unknown | VersionControl,
		}

		class Node
		{
			public static Comparer<string> NameComparer = Comparer<string>.Create(CompareTo);

			public string Name { get; }
			public Status Status { get; set; } = Status.None;

			public Node(string name) => Name = (name.Trim('\\') + @"\").ToLowerInvariant();

			public override string ToString() => Name;

			static int CompareTo(string file1, string file2)
			{
				var node1Start = 0;
				var node2Start = 0;
				while (true)
				{
					if (node1Start >= file1.Length)
					{
						if (node2Start >= file2.Length)
							return 0;
						return 1;
					}
					if (node2Start >= file2.Length)
						return -1;

					var node1End = file1.IndexOf('\\', node1Start);
					if (node1End == -1)
						node1End = file1.Length;
					var node1Partial = file1.Substring(node1Start, node1End - node1Start);

					var node2End = file2.IndexOf('\\', node2Start);
					if (node2End == -1)
						node2End = file2.Length;
					var node2Partial = file2.Substring(node2Start, node2End - node2Start);

					var compare = node1Partial.CompareTo(node2Partial);
					if (compare != 0)
						return compare;

					node1Start = node1End + 1;
					node2Start = node2End + 1;
				}
			}
		}

		public static IReadOnlyList<Status> GetStatuses(IReadOnlyList<string> files)
		{
			var data = files.Select(file => new Node(file)).ToList();
			var working = new Queue<Node>(data.OrderBy(x => x.Name, Node.NameComparer));

			using (var svnClient = new SvnClient())
			{
				while (working.Any())
				{
					var fileName = working.Peek().Name;

					var statuses = GetStatusesGit(fileName) ?? GetStatusesSvn(svnClient, fileName);
					if (statuses == null)
						working.Dequeue().Status = Status.Unknown;
					else
						FillStatuses(statuses.Item1, statuses.Item2, statuses.Item3, statuses.Item4, working);
				}
			}

			return data.Select(x => x.Status).ToList();
		}

		static Tuple<string, string, Dictionary<string, Status>, List<string>> GetStatusesGit(string fileName)
		{
			var repoData = Repository.Discover(fileName);
			if (repoData == null)
				return null;

			using (var repository = new Repository(repoData))
			{
				var repoRoot = repository.Info.WorkingDirectory.ToLowerInvariant();
				var repoStatus = repository.RetrieveStatus();

				var ignored = repoStatus.Ignored.Select(entry => Path.Combine(repoRoot, entry.FilePath.ToLowerInvariant().Replace('/', '\\')).TrimEnd('\\') + '\\').OrderBy(Node.NameComparer).ToList();

				var statusMap = new Dictionary<string, Status>();
				repoStatus.Modified.Concat(repoStatus.Added).Concat(repoStatus.Staged).Concat(repoStatus.Removed).ForEach(entry => statusMap[Path.Combine(repoRoot, entry.FilePath.Replace('/', '\\').ToLowerInvariant().TrimEnd('\\') + '\\')] = Status.Modified);
				repoStatus.Untracked.ForEach(entry => statusMap[Path.Combine(repoRoot, entry.FilePath.Replace('/', '\\').ToLowerInvariant().TrimEnd('\\') + '\\')] = Status.Unknown);

				return Tuple.Create(repoRoot, repoData, statusMap, ignored);
			}
		}

		static Status GetSvnStatus(SvnStatus status)
		{
			switch (status)
			{
				case SvnStatus.Zero: return Status.Unknown;
				case SvnStatus.None: return Status.Unknown;
				case SvnStatus.NotVersioned: return Status.Unknown;
				case SvnStatus.Normal: return Status.Normal;
				case SvnStatus.Added: return Status.Modified;
				case SvnStatus.Missing: return Status.Modified;
				case SvnStatus.Deleted: return Status.Modified;
				case SvnStatus.Replaced: return Status.Modified;
				case SvnStatus.Modified: return Status.Modified;
				case SvnStatus.Merged: return Status.Modified;
				case SvnStatus.Conflicted: return Status.Modified;
				case SvnStatus.Ignored: return Status.Ignored;
				case SvnStatus.Obstructed: return Status.Modified;
				case SvnStatus.External: return Status.Modified;
				case SvnStatus.Incomplete: return Status.Modified;
				default: throw new Exception("Invalid status");
			}
		}

		static Tuple<string, string, Dictionary<string, Status>, List<string>> GetStatusesSvn(SvnClient svnClient, string fileName)
		{
			var repoRoot = svnClient.GetWorkingCopyRoot(fileName).ToLowerInvariant() + '\\';
			if (repoRoot == null)
				return null;

			var statusMap = new Dictionary<string, Status>();
			var ignored = new List<string>();
			svnClient.Status(repoRoot, new SvnStatusArgs { RetrieveIgnoredEntries = true, Depth = SvnDepth.Infinity }, (sender, eventArgs) =>
			{
				var path = eventArgs.FullPath.ToLowerInvariant() + '\\';
				var status = GetSvnStatus(eventArgs.LocalContentStatus);
				if (status == Status.Ignored)
					ignored.Add(path);
				else
					statusMap[path] = status;
			});

			ignored = ignored.OrderBy(Node.NameComparer).ToList();

			return Tuple.Create(repoRoot, $@"{repoRoot}.svn\", statusMap, ignored);
		}

		static void FillStatuses(string repoRoot, string repoData, Dictionary<string, Status> statusMap, List<string> ignored, Queue<Node> working)
		{
			var ignoredIndex = 0;
			while (working.Any())
			{
				var node = working.Peek();
				if (!node.Name.StartsWith(repoRoot))
					break;
				working.Dequeue();

				if (node.Name.StartsWith(repoData))
				{
					node.Status = Status.VersionControl;
					continue;
				}

				while (ignoredIndex < ignored.Count)
				{
					var ignorePath = ignored[ignoredIndex];
					if (node.Name.StartsWith(ignorePath))
					{
						node.Status = Status.Ignored;
						break;
					}

					var ignoreCompare = Node.NameComparer.Compare(node.Name, ignorePath);
					if (ignoreCompare == -1)
						break;

					++ignoredIndex;
				}
				if (node.Status != Status.None)
					continue;

				if (statusMap.TryGetValue(node.Name, out var status))
					node.Status = status;
				else
					node.Status = Status.Normal;
			}
		}

		static List<string> SplitPath(string path) => path.Split('\\').NonNullOrEmpty().ToList();

		static byte[] GetUnmodifiedGitFile(string file)
		{
			try
			{
				var repoPath = Repository.Discover(file);
				if (repoPath == null)
					return null;

				using (var repository = new Repository(repoPath))
				{
					var obj = repository.Head.Tip.Tree as GitObject;
					foreach (var path in SplitPath(file.Substring(repository.Info.WorkingDirectory.Length)))
					{
						obj = (obj as Tree)?.SingleOrDefault(x => x.Name.Equals(path, StringComparison.OrdinalIgnoreCase))?.Target;
						if (obj == null)
							return null;
					}

					if (!(obj is Blob blob))
						return null;

					using (var ms = new MemoryStream((int)blob.Size))
					{
						using (var stream = blob.GetContentStream())
							stream.CopyTo(ms);
						return ms.ToArray();
					}
				}
			}
			catch { return null; }
		}

		static byte[] GetUnmodifiedSvnFile(string file)
		{
			try
			{
				file = SvnTools.GetTruePath(file);
				if (file == null)
					return null;

				using (var ms = new MemoryStream())
				{
					using (var client = new SvnClient())
						client.Write(SvnPathTarget.FromString(file), ms, new SvnWriteArgs() { Revision = SvnRevision.Base });
					return ms.ToArray();
				}
			}
			catch { return null; }
		}

		public static byte[] GetUnmodifiedFile(string file) => GetUnmodifiedGitFile(file) ?? GetUnmodifiedSvnFile(file);
	}
}
