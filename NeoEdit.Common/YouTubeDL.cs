using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Newtonsoft.Json.Linq;

namespace NeoEdit.Common
{
	public static class YouTubeDL
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

		public static List<string> GetPlayListItems(string url)
		{
			var startInfo = new ProcessStartInfo
			{
				FileName = Settings.YouTubeDLPath,
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
				result = process.StandardOutput.ReadToEnd();
				process.WaitForExit();
			}

			return JToken.Parse(result)["entries"].Children().Select(token => GetPlayListItem(token)).ToList();
		}

		public static void Update() => Process.Start(Settings.YouTubeDLPath, "-U");

		public static void DownloadStream(string directory, string url, DateTime fileTime, Action<long> progress)
		{
			using (var process = new Process
			{
				StartInfo = new ProcessStartInfo
				{
					FileName = Settings.YouTubeDLPath,
					Arguments = $@"-iwc --ffmpeg-location ""{Settings.FFmpegPath}"" --no-playlist ""{url}""",
					WorkingDirectory = directory,
					UseShellExecute = false,
					StandardOutputEncoding = Encoding.Default,
					StandardErrorEncoding = Encoding.Default,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					CreateNoWindow = true,
				}
			})
			{
				var fileName = default(string);
				process.OutputDataReceived += (s, e) =>
				{
					if (e.Data == null)
						return;

					var match = Regex.Match(e.Data, @"^\[download\]\s*([0-9.]+)%(?:\s|$)");
					if (match.Success)
						progress((int)(double.Parse(match.Groups[1].Value) + 0.5));

					match = Regex.Match(e.Data, @"^\[download\]\s*(?:Destination:\s*(.*?)|(.*?) has already been downloaded)(?:$)");
					if (match.Success)
						fileName = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;
				};
				process.ErrorDataReceived += (s, e) => { };
				process.Start();
				process.BeginOutputReadLine();
				process.BeginErrorReadLine();

				var done = new AutoResetEvent(false);
				process.EnableRaisingEvents = true;
				process.Exited += (s, e) => { process.WaitForExit(); done.Set(); };
				try
				{
					done.WaitOne();
					if (fileName != null)
					{
						var fileInfo = new FileInfo(Path.Combine(directory, fileName));
						if (fileInfo.Exists)
							fileInfo.LastWriteTime = fileTime;
					}
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
