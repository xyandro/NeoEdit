using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using NeoEdit.Common;
using NeoEdit.Common.Dialogs;
using NeoEdit.MenuHelp.Dialogs;

namespace NeoEdit.MenuHelp
{
	public static class HelpFunctions
	{
		static public void Load() { } // Doesn't do anything except load the assembly

		static public void Command_Help_About() => HelpAboutDialog.Run();

		static public void Command_Help_Update()
		{
			const string location = "https://github.com/xyandro/NeoEdit/releases";
			const string url = location + "/latest";
			const string check = location + "/tag/";
			const string exe = location + "/download/{0}/NeoEdit.exe";

			var entryAssembly = Assembly.GetEntryAssembly();
			var oldVersion = ((AssemblyFileVersionAttribute)entryAssembly.GetCustomAttribute(typeof(AssemblyFileVersionAttribute))).Version;
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
			if (new Message
			{
				Title = "Download new version?",
				Text = newer ? $"A newer version ({newVersion}) is available. Download it?" : $"Already up to date ({newVersion}). Update anyway?",
				Options = MessageOptions.YesNo,
				DefaultAccept = newer ? MessageOptions.Yes : MessageOptions.No,
				DefaultCancel = MessageOptions.No,
			}.Show() != MessageOptions.Yes)
				return;

			var oldLocation = entryAssembly.Location;
			var newLocation = Path.Combine(Path.GetDirectoryName(oldLocation), $"{Path.GetFileNameWithoutExtension(oldLocation)}-Update{Path.GetExtension(oldLocation)}");

			byte[] result = null;
			ProgressDialog.Run(null, "Downloading new version...", (cancelled, progress) =>
			{
				var finished = new ManualResetEvent(false);
				using (var client = new WebClient())
				{
					client.DownloadProgressChanged += (s, e) => progress(e.ProgressPercentage);
					client.DownloadDataCompleted += (s, e) =>
					{
						if (!e.Cancelled)
							result = e.Result;
						finished.Set();
					};
					client.DownloadDataAsync(new Uri(string.Format(exe, newVersion)));
					while (!finished.WaitOne(500))
						if (cancelled())
							client.CancelAsync();
				}
			});

			if (result == null)
				return;

			File.WriteAllBytes(newLocation, result);

			Message.Show("The program will be updated after exiting.");
			Process.Start(newLocation, $@"-update ""{oldLocation}"" {Process.GetCurrentProcess().Id}");
		}

		static public void Command_Help_RunGC() => GC.Collect();
	}
}
