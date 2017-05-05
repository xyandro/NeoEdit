using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using NeoEdit.Common;
using NeoEdit.GUI.Controls;
using Newtonsoft.Json.Linq;

namespace NeoEdit.StreamSave
{
	static class YouTubeDL
	{
		static string GetPlayListItem(JToken json)
		{
			var str = ((json["ie_key"] as JValue)?.Value as string)?.ToLowerInvariant();
			switch (str)
			{
				case "youtube":
					{
						var id = (json["id"] as JValue)?.Value as string;
						if (id == null)
							return null;
						return $"http://www.youtube.com/watch?v={id}";
					}
				default: return null;
			}
		}

		public static async Task<List<string>> GetPlayListItems(string url, IProgress<ProgressReport> progress = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			var startInfo = new ProcessStartInfo
			{
				FileName = NEWindow.YouTubeDLPath,
				Arguments = $@"-J --flat-playlist ""{url}""",
				UseShellExecute = false,
				StandardOutputEncoding = Encoding.UTF8,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				CreateNoWindow = true,
			};
			string result;
			using (var process = Process.Start(startInfo))
			{
				result = await process.StandardOutput.ReadToEndAsync();
				process.WaitForExit();
			}

			return JToken.Parse(result)["entries"].Children().Select(token => GetPlayListItem(token)).ToList();
		}

		public static void Update() => Process.Start(NEWindow.YouTubeDLPath, "-U");

		public static async Task DownloadStream(string directory, string url, IProgress<ProgressReport> progress = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			using (var process = new Process
			{
				StartInfo = new ProcessStartInfo
				{
					FileName = NEWindow.YouTubeDLPath,
					Arguments = $@"-iwc --no-playlist ""{url}""",
					WorkingDirectory = directory,
					UseShellExecute = false,
					StandardOutputEncoding = Encoding.UTF8,
					StandardErrorEncoding = Encoding.UTF8,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					CreateNoWindow = true,
				}
			})
			{
				process.OutputDataReceived += (s, e) =>
				{
					if (e.Data == null)
						return;

					var match = Regex.Match(e.Data, @"^\[download\]\s*([0-9.]+)%(?:\s|$)");
					if (!match.Success)
						return;

					var percent = double.Parse(match.Groups[1].Value);
					progress?.Report(new ProgressReport(percent, 100));
				};
				process.ErrorDataReceived += (s, e) => { };
				process.Start();
				process.BeginOutputReadLine();
				process.BeginErrorReadLine();

				var tcs = new TaskCompletionSource<object>();
				process.EnableRaisingEvents = true;
				process.Exited += (s, e) => tcs.TrySetResult(null);
				if (cancellationToken != default(CancellationToken))
					cancellationToken.Register(tcs.SetCanceled);
				try
				{
					await tcs.Task;
				}
				catch when (!process.HasExited)
				{
					process.Kill();
					throw;
				}
			}
		}
	}
}
