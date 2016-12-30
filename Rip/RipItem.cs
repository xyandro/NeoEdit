using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NeoEdit.Common;

namespace NeoEdit.Rip
{
	abstract class RipItem
	{
		public abstract string FileName { get; }
		public string GetFileName(string directory) => Path.Combine(directory, FileName);
		public abstract Task Run(IProgress<ProgressReport> progress, CancellationToken token, string directory);
	}
}
