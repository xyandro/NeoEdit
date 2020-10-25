using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using NeoEdit.Common;
using NeoEdit.Common.Enums;
using NeoEdit.Editor.PreExecution;
using NeoEdit.TaskRunning;

namespace NeoEdit.Editor
{
	partial class Tab
	{
		static PreExecutionStop PreExecute_Help_About(EditorExecuteState state)
		{
			state.Tabs.TabsWindow.RunHelpAboutDialog();
			return PreExecutionStop.Stop;
		}

		static PreExecutionStop PreExecute_Help_Tutorial(EditorExecuteState state)
		{
			//TODO => new TutorialWindow(this);
			return PreExecutionStop.Stop;
		}

		static PreExecutionStop PreExecute_Help_Update(EditorExecuteState state)
		{
			const string location = "https://github.com/xyandro/NeoEdit/releases";
			const string url = location + "/latest";
			const string check = location + "/tag/";
			const string exe = location + "/download/{0}/NeoEdit.exe";

			var oldVersion = ((AssemblyFileVersionAttribute)typeof(Tabs).Assembly.GetCustomAttribute(typeof(AssemblyFileVersionAttribute))).Version;
			string newVersion;

			var request = WebRequest.Create(url) as HttpWebRequest;
			request.AllowAutoRedirect = false;
			using (var response = request.GetResponse() as HttpWebResponse)
			{
				var redirUrl = response.Headers["Location"];
				if (!redirUrl.StartsWith(check))
					throw new Exception("Version check failed to find latest version");

				newVersion = redirUrl.Substring(check.Length);
			}

			var oldNums = oldVersion.Split('.').Select(str => int.Parse(str)).ToList();
			var newNums = newVersion.Split('.').Select(str => int.Parse(str)).ToList();
			if (oldNums.Count != newNums.Count)
				throw new Exception("Version length mismatch");

			var newer = oldNums.Zip(newNums, (oldNum, newNum) => newNum.IsGreater(oldNum)).NonNull().FirstOrDefault();
			if (!state.Tabs.TabsWindow.RunMessageDialog("Download new version?", newer ? $"A newer version ({newVersion}) is available. Download it?" : $"Already up to date ({newVersion}). Update anyway?", MessageOptions.YesNo, newer ? MessageOptions.Yes : MessageOptions.No, MessageOptions.No).HasFlag(MessageOptions.Yes))
				return PreExecutionStop.Stop;

			var oldLocation = Assembly.GetEntryAssembly().Location;
			var newLocation = Path.Combine(Path.GetDirectoryName(oldLocation), $"{Path.GetFileNameWithoutExtension(oldLocation)}-Update{Path.GetExtension(oldLocation)}");

			TaskRunner.Run(progress =>
			{
				byte[] result = null;

				var finished = new ManualResetEvent(false);
				using (var client = new WebClient())
				{
					client.DownloadProgressChanged += (s, e) =>
					{
						try { progress(e.ProgressPercentage); }
						catch { client.CancelAsync(); }
					};
					client.DownloadDataCompleted += (s, e) =>
					{
						if (!e.Cancelled)
							result = e.Result;
						finished.Set();
					};
					client.DownloadDataAsync(new Uri(string.Format(exe, newVersion)));
					finished.WaitOne();
				}

				if (result == null)
					return;

				File.WriteAllBytes(newLocation, result);

				Process.Start(newLocation, $@"-update ""{oldLocation}"" {Process.GetCurrentProcess().Id}");
				state.Tabs.TabsWindow.RunMessageDialog("Info", "The program will be updated after exiting.");
			});

			return PreExecutionStop.Stop;
		}

		static PreExecutionStop PreExecute_Help_Extract(EditorExecuteState state)
		{
			var location = Assembly.GetEntryAssembly().Location;

			if (!state.Tabs.TabsWindow.RunMessageDialog("Extract files", $"Files will be extracted from {location} after program exits.", MessageOptions.OkCancel, MessageOptions.Ok, MessageOptions.Cancel).HasFlag(MessageOptions.Ok))
				return PreExecutionStop.Stop;

			Process.Start(location, $@"-extract {Process.GetCurrentProcess().Id}");

			return PreExecutionStop.Stop;
		}

		static PreExecutionStop PreExecute_Help_RunGC(EditorExecuteState state)
		{
			GC.Collect();
			return PreExecutionStop.Stop;
		}

		static PreExecutionStop PreExecute_Help_CopyCommandLine(EditorExecuteState state)
		{
			var clipboard = new NEClipboard();
			clipboard.Add(new List<string> { Environment.CommandLine });
			NEClipboard.Current = clipboard;

			return PreExecutionStop.Stop;
		}
	}
}
