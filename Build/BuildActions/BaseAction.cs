using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Build.BuildActions
{
	abstract class BaseAction
	{
		abstract public string Name { get; }

		public override string ToString() => Name;

		virtual public bool Prepare() => true;

		abstract public void Run(WriteTextDelegate writeText, string configuration, List<string> platforms);

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
