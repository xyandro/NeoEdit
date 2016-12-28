using System;
using System.IO;

namespace NeoEdit.Rip
{
	abstract class RipItem
	{
		public abstract string FileName { get; }
		public string GetFileName(string directory) => Path.Combine(directory, FileName);
		public abstract void Run(Func<bool> cancelled, Action<int> progress, string directory);
	}
}
