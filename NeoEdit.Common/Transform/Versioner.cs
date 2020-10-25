using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LibGit2Sharp;
using NeoEdit.Common;
using SharpSvn;

namespace NeoEdit.Common.Transform
{
	public class Versioner
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
			public Status Status { get; private set; }
			Dictionary<string, Node> children = new Dictionary<string, Node>(StringComparer.OrdinalIgnoreCase);

			public Node(Status status) => Status = status;

			public Node GetChild(string name, bool create = false)
			{
				if (children.ContainsKey(name))
					return children[name];

				if (!create)
					return null;

				return children[name] = new Node(Status.None);
			}

			public void Reset(Status status)
			{
				Status = status;
				children.Clear();
			}
		}

		readonly Node root = new Node(Status.None);

		static List<string> SplitPath(string path) => path.Split('\\').NonNullOrEmpty().ToList();

		void AddPathStatus(string path, Status status)
		{
			var node = root;
			foreach (var item in SplitPath(path))
				node = node.GetChild(item, true);
			node.Reset(status);
		}

		public Status GetStatus(string path)
		{
			if ((!File.Exists(path)) && (!Directory.Exists(path)))
				return Status.Unknown;

			for (var pass = 0; pass < 2; ++pass)
			{
				var node = root;
				var status = Status.None;
				foreach (var item in SplitPath(path))
				{
					node = node.GetChild(item);
					if (node == null)
						break;
					if (node.Status != Status.None)
						status = node.Status;
				}

				if (status != Status.None)
					return status;

				if (pass == 0)
					SetupPath(path);
			}
			throw new Exception("GetStatus logic failure");
		}

		void SetupPath(string path)
		{
			if (GetGitFiles(path))
				return;

			if (GetSvnFiles(path))
				return;

			if (File.Exists(path))
				path = Path.GetDirectoryName(path);
			AddPathStatus(path, Status.Unknown);
		}

		bool GetSvnFiles(string path)
		{
			using (var client = new SvnClient())
			{
				var root = client.GetWorkingCopyRoot(path);
				if (root == null)
					return false;

				client.Status(root, new SvnStatusArgs { RetrieveAllEntries = true, RetrieveIgnoredEntries = true, Depth = SvnDepth.Infinity }, (sender, status) => AddPathStatus(status.FullPath, GetSvnStatus(status)));
				AddPathStatus($@"{root}\.svn", Status.VersionControl);
				return true;
			}
		}

		static Status GetSvnStatus(SvnStatusEventArgs status)
		{
			switch (status.LocalContentStatus)
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

		bool GetGitFiles(string path)
		{
			var repoPath = Repository.Discover(path);
			if (repoPath == null)
				return false;

			using (var repository = new Repository(repoPath))
			{
				AddPathStatus(repository.Info.WorkingDirectory, Status.Normal);

				var status = repository.RetrieveStatus();
				new Dictionary<Status, IEnumerable<StatusEntry>>
				{
					[Status.Ignored] = status.Ignored,
					[Status.Modified] = status.Modified.Concat(status.Added).Concat(status.Staged).Concat(status.Removed),
					[Status.Unknown] = status.Untracked,
				}.ForEach(pair => pair.Value.ForEach(entry => AddPathStatus(Path.Combine(repository.Info.WorkingDirectory, entry.FilePath), pair.Key)));

				AddPathStatus(repository.Info.Path, Status.VersionControl);
			}
			return true;
		}

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
