using System;
using System.Diagnostics;
using System.IO;

namespace Build.BuildActions
{
	abstract class BaseAction
	{
		public abstract string Name { get; }

		public override string ToString() => Name;

		public virtual bool Prepare() => true;

		public abstract void Run(WriteTextDelegate writeText);

		protected void RunCommand(WriteTextDelegate writeText, string fileName, string arguments)
		{
			writeText($"Execute: {fileName} {arguments}...");

			var stopWatch = Stopwatch.StartNew();
			var process = new Process
			{
				StartInfo = new ProcessStartInfo
				{
					FileName = fileName,
					Arguments = arguments,
					UseShellExecute = false,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					CreateNoWindow = true,
				}
			};
			process.OutputDataReceived += (s, e) => { if (e.Data != null) writeText(e.Data); };
			process.ErrorDataReceived += (s, e) => { if (e.Data != null) writeText($"[stderr] {e.Data}"); };
			process.Start();
			process.BeginOutputReadLine();
			process.BeginErrorReadLine();
			process.WaitForExit();
			stopWatch.Stop();
			writeText($"Finished {Path.GetFileName(fileName)} ({stopWatch.Elapsed.TotalSeconds} seconds).");

			if (process.ExitCode != 0)
				throw new Exception($"Command failed: exit code {process.ExitCode}.");
		}
	}
}
